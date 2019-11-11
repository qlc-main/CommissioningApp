using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfCommApp
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private string _email;
        private string _id;
        private string _password;
        private string _ticket;

        private ICommand _login;

        #endregion

        #region Properties

        public bool Completed { get; }

        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged(nameof(Email));
                }
            }
        }

        public string OperationID
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(OperationID));
                }
            }
        }

        public string Name { get { return "Login"; } }

        #endregion

        #region Commands

        public ICommand Login
        {
            get
            {
                if (_login == null)
                    _login = new RelayCommand(p => CRMWrapper(p as PasswordBox));

                return _login;
            }
        }

        #endregion

        #region Constructors

        public LoginViewModel()
        {
        }

        #endregion

        #region Methods

        #region Public 

        #endregion

        #region Private

        /// <summary>
        /// Logs into CRM using provided credentials and then uploads each meter in memory that
        /// has been commissioned
        /// </summary>
        /// <returns></returns>
        private async Task CreateCRM()
        {
            HttpClient client = new HttpClient();
            string logDir = String.Format("{0}\\Logs", Directory.GetCurrentDirectory());
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string uploadedDir = String.Format("{0}\\Uploaded", Directory.GetCurrentDirectory());
            if (!Directory.Exists(uploadedDir))
                Directory.CreateDirectory(uploadedDir);

            // Logs into CRM using provided credentials and saves login token
            if (!await Authenticate(client))
                return; // TODO: show error to user that provided credentials are incorrect if login attempt fails

            // Iterate over each meter in memory and attempt to upload contents to CRM
            foreach (KeyValuePair<string, Meter> kvp in (Application.Current.Properties["meters"] as Dictionary<string, Meter>))
            {
                // Only upload meters that have gone through each step and are commissioned
                if (!kvp.Value.Commissioned)
                    continue;

                bool opt = !string.IsNullOrEmpty(_id), edit = false;
                string query, response, rid, fileName;
                Dictionary<string, string> data;

                // Check if a record already exists with this meter ID if so the current user is editting a record
                // if not the current user is creating or adding a record, empty response indicates a new record
                // will be generated
                response = await DoQuery(client, "bhfwfquxf", String.Format("{{'19'.TV.'{0}'}}", kvp.Value.ID));
                if (string.IsNullOrEmpty(response))
                {
                    // If user provides an operation ID then use that in conjunction with the meter ID 
                    // to find the appropriate information and upload to CRM
                    if (opt) /* operation id provided */
                    {
                        rid = "";
                        response = await GetRecord(client, "bghhvi54m", _id);
                        string sID = GetSiteID(response);
                        query = string.Format("{{'391'.TV.'{0}'}}AND{{'186'.TV.'{1}'}}", sID, kvp.Key);
                        rid = await DoQuery(client, "bghhviw72", query);

                        response = await GetRecord(client, "bghhviw72", rid);
                        data = CreateFSData(response, kvp.Value, opt);
                    }
                    // If this is meter does not fall under S.O. device commissioning use this method
                    // to find the appropriate information and upload to CRM
                    else
                    {
                        query = string.Format("{{'16'.TV.'{0}'}}", kvp.Key);
                        rid = await DoQuery(client, "bgs8pzryj", query);

                        response = await GetRecord(client, "bgs8pzryj", rid);
                        data = CreateFSData(response, kvp.Value, opt);
                    }

                    // After data has been retrieved, upload meter data to CRM
                    // If Successful, continue with uploading the meter point data
                    // If Unsuccessful, save response to log file and try next meter
                    response = await AddRecord(client, "bhfwfquxf", data);
                    if (!response.Contains("<errcode>0</errcode>"))
                    {
                        fileName = String.Format("{0}\\{1}-FSReportFailure.txt", logDir, kvp.Key);
                        DumpToLog(fileName, "Failed to create FS Report, dumping response to log file", response);
                        rid = "";
                    }
                    else
                    {
                        // If program successfully created the record, upload the file that the program creates with the meter data
                        // to that record and then attempt to upload meter point data for each fully commissioned meter point
                        int start = response.IndexOf("<rid>") + 5;
                        rid = response.Substring(start, response.IndexOf("</rid>") - start);
                    }
                }
                else
                {
                    // Sets the record ID variable that will be editted by this user
                    // Retrieves the entire record from the database in order to get the 
                    // update ID from the record. Creates the data object to modify the record
                    // Issues the EditRecord command, if it fails dump response to log and then
                    // set the rid to an empty string
                    rid = response;
                    edit = true;
                    response = await GetRecord(client, "bhfwfquxf", rid);
                    int start = response.IndexOf("update_id>") + 10;
                    string update = response.Substring(start, response.IndexOf("<", start) - start);
                    data = EditFSData(kvp.Value, rid, update);
                    response = await EditRecord(client, "bhfwfquxf", data);
                    if (!response.Contains("<errcode>0</errcode>"))
                    {
                        fileName = String.Format("{0}\\{1}-FSReportFailure.txt", logDir, kvp.Key);
                        DumpToLog(fileName, "Failed to edit FS Report, dumping response to log file", response);
                        rid = "";
                    }
                }

                // If the edit or create record was successful then upload the meter file to CRM
                if (!string.IsNullOrEmpty(rid))
                {
                    await UploadFile(client, "bhfwfquxf", new Tuple<string, string, string, string>("24", kvp.Key + ".txt", rid, String.Format("{0}\\ToUpload\\{1}.txt", Directory.GetCurrentDirectory(), kvp.Key)));
                    foreach (Channel c in kvp.Value.Channels)
                    {
                        // Check filters out meter points that have not been commissioned, if this 
                        // meter point existed originally but the current user removed it then delete
                        // record from CRM
                        if (c.Notes == "Not Commissioned" || (c.Phase1 == null && c.Phase2 == null))
                        {
                            // If user is editting a previous FS Report and an entry exists for a Meter Point but 
                            // current user decommissioned that Meter Point, delete the record from CRM
                            if (edit)
                            {
                                query = String.Format("{{'6'.TV.'{0}'}}AND{{'24'.TV.'{1}'}}", c.ID, rid);
                                response = await DoQuery(client, "bicmyvvte", query);
                                if (!string.IsNullOrEmpty(response))
                                {
                                    response = await DeleteRecord(client, "bicmyvvte", response);
                                    // If Delete operation fails then dump report to log 
                                    if (!response.Contains("<errcode>0</errcode>"))
                                    {
                                        fileName = String.Format("{0}\\{1}-{2}-MPDeleteFailure.txt", logDir, kvp.Key, c.ID);
                                        DumpToLog(fileName, "Failed to delete Meter Point Report, dumping response to log file", response);
                                    }
                                }
                            }
                            continue;
                        }

                        // Creates the meter point data object and checks if it currently exists in CRM
                        // If it exists, edit the record instead of creating a new record
                        data = CreateMPData(c, rid);
                        query = String.Format("{{'6'.TV.'{0}'}}AND{{'24'.TV.'{1}'}}", c.ID, rid);
                        response = await DoQuery(client, "bicmyvvte", query);
                        if (!string.IsNullOrEmpty(response))
                        {
                            data["rid"] = response;
                            response = await GetRecord(client, "bicmyvvte", response);
                            int start = response.IndexOf("update_id>") + 10;
                            string update = response.Substring(start, response.IndexOf("<", start) - start);
                            data["update_id"] = update;
                            response = await EditRecord(client, "bicmyvvte", data);
                        }
                        else
                            response = await AddRecord(client, "bicmyvvte", data);

                        // If Unsuccessful at creating/editting meter point data, save response to log file and 
                        // attempt to work with new meter point or progress to next meter
                        if (!response.Contains("<errcode>0</errcode>"))
                        {
                            fileName = String.Format("{0}\\{1}-{2}-MPReportFailure.txt", logDir, kvp.Key, c.ID);
                            DumpToLog(fileName, "Failed to create/edit Meter Point Report, dumping response to log file", response);
                        }
                    }

                    // If successfully created FS Report, move meter save file but keep it locally just in case it needs to be referenced in the future
                    File.Move(String.Format("{0}\\ToUpload\\{1}.txt", Directory.GetCurrentDirectory(), kvp.Key), String.Format("{0}\\{1}.txt", uploadedDir, kvp.Key));
                }
            }
        }

        /// <summary>
        /// Uses data from the response string and meter object to create a dictionary that will be used
        /// to upload data as an FS Report to the CRM application
        /// </summary>
        /// <param name="response">String returned from GetRecord call with necessary data</param>
        /// <param name="meter">Meter object that has data that program will upload</param>
        /// <param name="opt">Boolean indicating whether or not the operational ID was supplied</param>
        /// <returns></returns>
        private Dictionary<string, string> CreateFSData(string response, Meter meter, bool opt)
        {
            string line;
            Dictionary<string, string> data = new Dictionary<string, string>();
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            // Iterates over each line until the necessary data has been filled in or lines have been exhausted, the latter should not occur
            for (int i = 0; i < lines.Length; i++)
            {
                // Case # is 17
                // Device ID is 18
                // Device Issue Record is 21
                if (lines[i].Contains("<fid>8</fid>") && !opt)
                {
                    line = lines[i += 3];
                    data["_fid_17"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if (lines[i].Contains("<fid>3</fid>") && !opt)
                {
                    line = lines[i += 3];
                    data["_fid_21"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if (lines[i].Contains("<fid>15</fid>") && !opt)
                {
                    line = lines[i += 3];
                    data["_fid_18"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if (lines[i].Contains("<fid>317</fid>") && opt)
                {
                    line = lines[i += 3];
                    data["_fid_17"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if (lines[i].Contains("<fid>104</fid>") && opt)
                {
                    line = lines[i += 3];
                    data["_fid_18"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if ((data.Count == 3 && !opt) || data.Count == 2 && opt)
                    break;
            }

            data["_fid_9"] = "17";                                       // Activity value to indicate commissioning
            data["_fid_12"] = DateTime.Today.ToString("MM-dd-yyyy");     // Date that work performed
            data["_fid_13"] = "2";                                       // Duration of work
            data["_fid_16"] = meter.Notes;                               // Comments for meter
            data["_fid_85"] = meter.Disposition.ToString();              // Disposition (No Problem Found)
            data["_fid_23"] = meter.FSReturn ? "1" : "0";                // No FS return required
            data["_fid_121"] = meter.OprComplete ? "1" : "0";            // Opr Complete
            data["_fid_72"] = meter.Location;                            // Device Location
            data["_fid_161"] = meter.Floor;                              // Device Floor
            data["ticket"] = _ticket;                                    // Authentication ticket

            return data;
        }

        /// <summary>
        /// Uses data from the Channel object to create a dictionary that will be used
        /// to upload data as an MP Report to the CRM application using the FS Report ID
        /// </summary>
        /// <param name="channel">Current Channel/Meter Point that is being uploaded to CRM</param>
        /// <param name="rid">Record ID of the FS Report</param>
        /// <returns></returns>
        private Dictionary<string, string> CreateMPData(Channel channel, string rid)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["_fid_6"] = channel.ID.ToString();                                         // Meter Point
            data["_fid_7"] = channel.ApartmentNumber;                                       // Apt/Load
            data["_fid_8"] = channel.BreakerNumber;                                         // Breakers
            data["_fid_9"] = string.Format("{0}:{1}", channel.Primary, channel.Secondary);  // CT Ratio
            data["_fid_10"] = channel.CTType;                                               // CT Type
            data["_fid_21"] = (int.Parse(channel.Primary) / 100).ToString();                // Multiplier
            data["_fid_24"] = rid;                                                          // FS Report
            data["_fid_40"] = string.Format("{0};{1};{2};{3}", channel.Serial, channel.Reason[0], channel.Reason[1], channel.Notes);
            data["ticket"] = _ticket;                                                       // Authentication ticket

            return data;
        }

        /// <summary>
        /// Extracts password out of the password box that is passed as an argument
        /// Initiates asynchronous call to CreateCRM which will use provided credentials
        /// to log the user into CRM and upload the results to CRM
        /// </summary>
        /// <param name="p"></param>
        private void CRMWrapper(PasswordBox p)
        {
            _password = p.Password;
            Task.Run(CreateCRM);
        }

        private void DumpToLog(string fileName, string message, string response)
        {
            Console.WriteLine(message);
            StreamWriter sw = new StreamWriter(fileName);
            sw.Write(response);
            sw.Close();
        }

        /// <summary>
        /// Retrieves data from the meter object in order to edit the RS Report that is 
        /// currently in CRM
        /// </summary>
        /// <param name="meter"></param>
        /// <returns></returns>
        private Dictionary<string, string> EditFSData(Meter meter, string rid, string update)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["_fid_16"] = meter.Notes;                               // Comments for meter
            data["_fid_85"] = meter.Disposition.ToString();              // Disposition (No Problem Found)
            data["_fid_23"] = meter.FSReturn ? "1" : "0";                // No FS return required
            data["_fid_121"] = meter.OprComplete ? "1" : "0";            // Opr Complete
            data["_fid_161"] = meter.Floor;                              // Device Floor
            data["rid"] = rid;                                           // Record ID that is being editted
            data["update_id"] = update;                                  // Update ID for the record
            data["ticket"] = _ticket;                                    // Authentication ticket

            return data;
        }

        /// <summary>
        /// Retrieves Site ID of the location where the meter being commissioned is located
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string GetSiteID(string response)
        {
            // Iterates over each line until field id 93 is encountered 
            // When encountered, return the value of this field
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<fid>93</fid>"))
                {
                    string line = lines[i += 3];
                    return line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
            }

            // If this field is not encountered then return empty string
            return "";
        }

        #region CRM Functions

        /// <summary>
        /// Generic Function that adds a record to the database indicated using the dictionary provided 
        /// to populate the parameters within the url field
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="info">Dictionary that contains URL parameters</param>
        /// <returns>Response string from the AddRecord request</returns>
        private async Task<string> AddRecord(HttpClient client, string db, Dictionary<string, string> info)
        {
            // Creates the parameter url string to append to the base URL string below and then returns the string 
            string append = string.Join("&", info.Select(x => string.Join("=", x.Key, x.Value)));
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_AddRecord&{1}", db, append));
        }

        /// <summary>
        /// Logs into the CRM using the provided credentials
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <returns>Boolean indicating the success/failure of the login attempt</returns>
        private async Task<bool> Authenticate(HttpClient client)
        {
            // Attempts to login to CRM and then iterates over each line until errcode line encountered
            // if there isn't an error then continue until the ticket line is encountered 
            // and save the ticket otherwise return false 
            string response = await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/main?a=API_Authenticate&username={0}&password={1}", _email, _password));
            foreach (string line in response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("errcode") && !line.Contains(">0<"))
                    return false;

                if (line.Contains("ticket"))
                {
                    int first = line.IndexOf(">") + 1;
                    int last = line.LastIndexOf("<");
                    _ticket = line.Substring(first, last - first);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="db"></param>
        /// <param name="rid"></param>
        /// <returns></returns>
        private async Task<string> DeleteRecord(HttpClient client, string db, string rid)
        {
            string paramString = String.Format("ticket={0}&rid={1}", _ticket, rid);
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DeleteRecord&{1}", db, paramString));
        }

        /// <summary>
        /// Performs Query operation on specified database using the supplied query string
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="query">String query parameters</param>
        /// <returns>Returns Record ID of query operation</returns>
        private async Task<string> DoQuery(HttpClient client, string db, string query)
        {
            // Counts the number of results returned from query string, if there 
            // is only one response then continue by performing the query and then
            // retrieve the record id of the one result
            string response = await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQueryCount&ticket={1}&fmt=structured&includeRids=1&query={2}", db, _ticket, query));
            if (response.Contains("<errcode>0</errcode>"))
            {
                int start = response.IndexOf("Matches") + 8;
                int stop = response.LastIndexOf("Matches") - 5;
                int count = int.Parse(response.Substring(start, stop - start));

                if (count != 1)
                    return "";      // error only expected single result received multiple

                response = await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQuery&ticket={1}&fmt=structured&includeRids=1&query={2}", db, _ticket, query));
                start = response.IndexOf("record rid=\"") + 12;
                stop = response.IndexOf('"', start);

                return response.Substring(start, stop - start);
            }
            else
            {
                // error
                return "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="db"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<string> EditRecord(HttpClient client, string db, Dictionary<string, string> data)
        {
            // Creates the parameter url string to append to the base URL string below and then returns the string 
            string append = string.Join("&", data.Select(x => string.Join("=", x.Key, x.Value)));
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_EditRecord&{1}", db, append));
        }

        /// <summary>
        /// Retrieves details of record from supplied record id using the supplied database ID
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="rid">String Record ID</param>
        /// <returns>String response of the GetRecord request</returns>
        private async Task<string> GetRecord(HttpClient client, string db, string rid)
        {
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_GetRecordInfo&ticket={1}&rid={2}", db, _ticket, rid));
        }

        /// <summary>
        /// Uploads the meter file to the specified database using the supplied tuple
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="tuple">Item1: Field ID, Item2: FileName, Item3: Record ID, Item4: File Path</param>
        /// <returns>Returns if operation was un/successful</returns>
        private async Task<bool> UploadFile(HttpClient client, string db, Tuple<string, string, string, string> tuple)
        {
            // Reads the file in byte format and converts it to a base 64 string
            // Creates an xml string that will be used to upload the data 
            // Creates an HttpRequestMessage and then prints the response string
            // to the user
            var bytes = File.ReadAllBytes(tuple.Item4);
            var base64 = Convert.ToBase64String(bytes);
            var xmlContent = String.Format("<qdbapi>\n\t<ticket>{0}</ticket>\n\t<udata>mydata</udata><field fid=\"{1}\" filename=\"{2}\">{3}</field>\n\t<rid>{4}</rid>\n</qdbapi>", _ticket, tuple.Item1, tuple.Item2, base64, tuple.Item3);
            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format("https://quadlogic.quickbase.com/db/{0}?", db)),
                Headers = { { "QUICKBASE-ACTION", "API_UploadFile" } },
                Content = content
            };
            var response = await client.SendAsync(httpRequestMessage);
            var stringResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(stringResponse);

            return true;
        }

        #endregion

        #endregion

        #endregion
    }
}
