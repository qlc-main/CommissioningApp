using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace WpfCommApp
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private string _email;
        private string _password;
        private bool _submitEnabled;
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

        public string FailedMessage { get; private set; }

        public string Name { get { return "Login"; } }

        public bool SubmitEnabled
        {
            get { return _submitEnabled;  }
            set
            {
                if (_submitEnabled != value)
                {
                    _submitEnabled = value;
                    OnPropertyChanged(nameof(SubmitEnabled));
                }
            }
        }

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
            SubmitEnabled = true;
            FailedMessage = "";
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
            SubmitEnabled = false;
            HttpClient client = new HttpClient();
            string fileName, sid = null, goldenCase = "";
            string toUploadDir = String.Format("{0}\\ToUpload", Directory.GetCurrentDirectory());
            string logDir = String.Format("{0}\\ErrorLogs", Directory.GetCurrentDirectory());
            Dictionary<string, string> failedMeters = new Dictionary<string, string>(), infoMeters = new Dictionary<string, string>();
            Dictionary<string, string> operationToSite = new Dictionary<string, string>(), siteToCases = new Dictionary<string, string>();
            List<string> successMeters = new List<string>();
            List<string> recordIDs, caseIDs = null;
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string uploadedDir = String.Format("{0}\\Uploaded", Directory.GetCurrentDirectory());
            if (!Directory.Exists(uploadedDir))
                Directory.CreateDirectory(uploadedDir);

            // Logs into CRM using provided credentials and saves login token
            if (_ticket == null && !await Authenticate(client))
            {
                FailedMessage = "Incorrect credentials provided, please try again!";
                return;
            }

            var service = CreateService();
            Dictionary<string, Meter> metersToCommission = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);

            while (true)
            {
                // bhfwfquxf - FS Reports Table
                // bghhvi54m - Operations Table
                // bghhviw72 - Devices Table
                // bgs8pzryj - Device Issues
                // 
                // Iterate over each meter in memory and attempt to upload contents to CRM
                foreach (KeyValuePair<string, Meter> kvp in metersToCommission)
                {
                    // Only upload meters that have gone through each step and are commissioned
                    if (!kvp.Value.Commissioned)
                        continue;

                    // Upload meters that have not been processed
                    if (!successMeters.Contains(kvp.Key) && !failedMeters.ContainsKey(kvp.Key))
                        UploadToGoogleDrive(service, String.Format("{0}\\{1}.txt", toUploadDir, kvp.Key), "1ITbna7DnCpXUvj2BgE3UtPgiZFIPITBo");

                    string query, response, rid, oldMeters;
                    Dictionary<string, string> data;

                    // Check if a record already exists with this meter ID if so the current user is editting a record
                    // if not the current user is creating or adding a record, empty response indicates a new record
                    // will be generated
                    oldMeters = await DoQueryRecordString(client, "bhfwfquxf", String.Format("{{19.TV.'{0}'}}AND{{37.TV.'{0}'}}", kvp.Value.ID, kvp.Value.OperationID), "37");
                    var originalCount = failedMeters.Count;
                    var results = await GetSite(client, kvp, operationToSite, failedMeters);
                    sid = results.Item1;
                    if (!siteToCases.ContainsKey(sid))
                        siteToCases.Add(sid, results.Item2);
                    operationToSite = results.Item3;
                    failedMeters = results.Item4;
                    if (failedMeters.Count > originalCount)
                        continue;

                    // Check if current serial num has a device issue with a customer problem commissioning request
                    // associated with the current site and a status that is not completed
                    var deviceIssue = await IsDeviceIssue(client, kvp.Key, sid);

                    // Get the Device Record 
                    query = string.Format("{{186.TV.'{0}'}}", kvp.Key);
                    response = await DoQueryRecordString(client, "bghhviw72", query);

                    if (String.IsNullOrEmpty(response))
                    {
                        FailedMessage = "Failure encountered, check log file";
                        failedMeters.Add(kvp.Key, String.Format("Can't find serial number: {0} in Devices, continuing...", kvp.Key));
                        continue;
                    }

                    data = CreateFSData(response, kvp.Value, deviceIssue);
                    if (CheckCase(kvp.Key, sid, siteToCases, ref goldenCase, ref data, ref failedMeters, ref infoMeters))
                        continue;

                    var results2 = await UploadFSReport(client, oldMeters, kvp.Value.OperationID, kvp.Key, logDir, data, failedMeters);
                    rid = results2.Item1;
                    var edit = results2.Item2;
                    failedMeters = results2.Item3;

                    // If the edit or create record was successful then upload the meter file to CRM
                    if (!string.IsNullOrEmpty(rid))
                    {
                        foreach (Channel c in kvp.Value.Channels)
                        {
                            await UploadMeterPoint(client, c, rid, edit, kvp.Key, logDir);
                        }

                        // If successfully created FS Report, move meter save file but keep it locally just in case it needs to be referenced in the future
                        // 1ITbna7DnCpXUvj2BgE3UtPgiZFIPITBo Meters Folder
                        // 1YkVwvSJuQrb4RGsy9BoNTIfAuhqb-YNS Logs Folder

                        System.IO.File.Move(String.Format("{0}\\{1}.txt", toUploadDir, kvp.Key), String.Format("{0}\\{1}.txt", uploadedDir, kvp.Key));
                        successMeters.Add(kvp.Key);
                    }
                }

                // Run through commissioning loop again if there was a case that needed a "golden" reference case 
                // and it's original case is not associated with the current site from the operation ID
                bool completeCommissioning = true;
                metersToCommission = new Dictionary<string, Meter>();
                foreach (KeyValuePair<string, string> kv in failedMeters)
                {
                    if (kv.Value == "Need golden case id because current case id is associated with different site")
                    {
                        completeCommissioning = false;
                        metersToCommission.Add(kv.Value, (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[kv.Value]);
                    }
                }

                if (completeCommissioning)
                    break;
            }

            string windowMessage = LoggingInfo(successMeters, failedMeters, infoMeters, service);

            // Enable submit button after process complete
            SubmitEnabled = true;

            InfoView info = null;
            InfoViewModel ifvm = new InfoViewModel("Upload Process Complete", windowMessage);

            Application.Current.Dispatcher.Invoke(() =>
            {
                info = new InfoView
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    DataContext = ifvm
                };

                info.Show();
            });
        }

        /// <summary>
        /// Uses data from the response string and meter object to create a dictionary that will be used
        /// to upload data as an FS Report to the CRM application
        /// </summary>
        /// <param name="response">String returned from GetRecord call with necessary data</param>
        /// <param name="meter">Meter object that has data that program will upload</param>
        /// <param name="opt">Boolean indicating whether or not the operational ID was supplied</param>
        /// <returns></returns>
        private Dictionary<string, string> CreateFSData(string response, Meter meter, string deviceIssue)
        {
            string line;
            Dictionary<string, string> data = new Dictionary<string, string>();
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            // Iterates over each line until the necessary data has been filled in or lines have been exhausted, the latter should not occur
            for (int i = 0; i < lines.Length; i++)
            {
                // Case # is 17
                // Device ID is 18
                if (lines[i].Contains("<f id=\"137\">") && !String.IsNullOrEmpty(deviceIssue))
                {
                    line = lines[i];
                    data["_fid_17"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                    data["_fid_21"] = deviceIssue;
                }
                else if (lines[i].Contains("<f id=\"317\">") && String.IsNullOrEmpty(deviceIssue))
                {
                    line = lines[i];
                    data["_fid_17"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if (lines[i].Contains("<f id=\"104\">"))
                {
                    line = lines[i];
                    data["_fid_18"] = line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
                else if ((data.Count == 2 && String.IsNullOrEmpty(deviceIssue)) || (data.Count == 3 && !String.IsNullOrEmpty(deviceIssue)))
                    break;
            }

            data["_fid_9"] = "17";                                       // Activity value to indicate commissioning
            data["_fid_12"] = DateTime.Today.ToString("MM-dd-yyyy");     // Date that work performed
            data["_fid_13"] = "2";                                       // Duration of work
            data["_fid_16"] = meter.Notes;                               // Comments for meter
            data["_fid_70"] = meter.Location;                            // Device Location
            data["_fid_85"] = meter.Disposition.ToString();              // Disposition (No Problem Found)
            data["_fid_23"] = meter.FSReturn ? "1" : "0";                // No FS return required
            data["_fid_121"] = meter.OprComplete ? "1" : "0";            // Opr Complete
            data["_fid_161"] = meter.Floor;                              // Device Floor
            data["_fid_37"] = meter.OperationID;                         // Operation Id

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

        /// <summary>
        /// Creates a file and places the contents into that file and prints a message to the command prompt.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="message"></param>
        /// <param name="contents"></param>
        private void DumpToLog(string fileName, string message, string contents)
        {
            Console.WriteLine(message);
            StreamWriter sw = new StreamWriter(fileName);
            sw.Write(contents);
            sw.Close();
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
                    return line.Substring(line.IndexOf(">") + 1, line.LastIndexOf("<") - (line.IndexOf(">") + 1));
                }
            }

            // If this field is not encountered then return empty string
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csvList"></param>
        /// <param name="recordIDs"></param>
        /// <returns></returns>
        private string Reconcile(string csvList, List<string> recordIDs)
        {
            string theCase = "";
            string[] instructions = csvList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string aCase in instructions)
            {
                if (recordIDs.Contains(aCase) && String.IsNullOrEmpty(theCase))
                    theCase = aCase;
                else if (recordIDs.Contains(aCase) && !String.IsNullOrEmpty(theCase))
                    theCase = "Multiple cases matched, should be impossible";
            }

            return theCase;
        }

        /// <summary>
        /// Creates the Drive API Service that is used to upload files to the Google Drive
        /// </summary>
        /// <returns></returns>
        private DriveService CreateService()
        {
            UserCredential credential;
            string[] Scopes = { DriveService.Scope.Drive };

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Upload from Commissioning App",
            });
        }

        /// <summary>
        /// Uploads a file to Google Drive under a specific folder, if parent is specified.
        /// </summary>
        /// <param name="_service"></param>
        /// <param name="_uploadFile"></param>
        /// <param name="_parent"></param>
        /// <returns></returns>
        private Google.Apis.Drive.v3.Data.File UploadToGoogleDrive(DriveService _service, string _uploadFile, string _parent)
        {
            if (System.IO.File.Exists(_uploadFile))
            {
                Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
                body.Name = System.IO.Path.GetFileName(_uploadFile);
                body.MimeType = "text/plain";
                body.Parents = new List<string> { _parent };
                byte[] byteArray = System.IO.File.ReadAllBytes(_uploadFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = _service.Files.Create(body, stream, "text/plain");
                    request.SupportsTeamDrives = true;
                    // You can bind event handler with progress changed event and response recieved(completed event)
                    request.Upload();
                    return request.ResponseBody;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error Occured");
                    return null;
                }
            }
            else
            {
                MessageBox.Show("The file does not exist.", "404");
                return null;
            }
        }

        /// <summary>
        /// Handles the logging messages for this upload attempt and saves log to Google drive and locally
        /// </summary>
        /// <param name="successMeters"></param>
        /// <param name="failedMeters"></param>
        /// <param name="infoMeters"></param>
        /// <param name="service"></param>
        private string LoggingInfo(List<string> successMeters, Dictionary<string, string> failedMeters, Dictionary<string, string> infoMeters, DriveService service)
        {
            string fileName = String.Format("{0}\\Logs\\UploadLog_{1}.txt", Directory.GetCurrentDirectory(), DateTime.Now.ToString("yyyyMMddHHmmss"));
            string logMessage = "";
            string windowMessage = "";
            if (successMeters.Count > 0)
            {
                logMessage = "Successfully uploaded the following meters:\n\t" + String.Join("\n\t", successMeters);
                windowMessage = successMeters.Count + " Successful";
            }

            // Prints failed meters, if any and associated info messages to log file
            if (failedMeters.Count > 0)
            {
                if (logMessage.Length > 0)
                    logMessage += "\n";
                logMessage += "Failed to upload the following meters:\n\t";
                logMessage += String.Join("\n\t", failedMeters.Select(m => m.Key + " " + m.Value));

                if (windowMessage.Length > 0)
                    windowMessage += ", " + failedMeters.Count + " Failed";
                else
                    windowMessage = failedMeters.Count + " Failed";
            }

            // Prints info messages about specific meters to log file
            if (infoMeters.Count > 0)
            {
                if (logMessage.Length > 0)
                    logMessage += "\n";
                logMessage += "Info messages for the following meters:\n\t";
                logMessage += String.Join("\n\t", infoMeters.Select(m => m.Key + " " + m.Value));

                string emailBody = "The following meters were commissioned at a different site:\n\t";
                emailBody += String.Join("\n\t", infoMeters.Select(m => m.Value));

                SendEmail(emailBody);
            }

            DumpToLog(fileName, "Finished uploading", logMessage);
            UploadToGoogleDrive(service, fileName, "1YkVwvSJuQrb4RGsy9BoNTIfAuhqb-YNS");

            return windowMessage;
        }

        /// <summary>
        /// Sends Email to predefined list of recipients concerning meters that were commissioned
        /// for a different site than the sales order
        /// </summary>
        /// <param name="body"></param>
        private void SendEmail(string body)
        {
            using (var mail = new MailMessage())
            using (var client = new SmtpClient("smtp.gmail.com"))
            {
                mail.From = new MailAddress("5nCommissioningApp@quadlogic.com", "Gordon (5N Version)");
                mail.To.Add("ecastillo@quadlogic.com,irosier@quadlogic.com");
                mail.Subject = "Meter(s) Commissioned At Different Site";
                mail.Body = body;

                client.UseDefaultCredentials = false;
                client.Port = 587;
                client.Credentials = new System.Net.NetworkCredential("dataservices@quadlogic.com", "zewbeqfwxqxvbesl");
                client.EnableSsl = true;

                client.Send(mail);
            }
        }

        /// <summary>
        /// Retrieves the device issue record id for this serial number and site id combination
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serial"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        private async Task<string> IsDeviceIssue(HttpClient client, string serial, string sid)
        {
            var query = string.Format("{{16.TV.'{0}'}}AND{{97.TV.'Commissioning Request'}}AND{{214.TV.'{1}'}}AND{{104.XEX.'I-Completed'}}", serial, sid);
            var recordIDs = await DoQueryRecordID(client, "bgs8pzryj", query);

            if (recordIDs.Count > 0)
                return recordIDs[0];

            return "";
        }

        /// <summary>
        /// Scans the response of previously entered FS Reports to see if there is a record
        /// that was entered against this current operation
        /// </summary>
        /// <param name="response"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        private string ScanOldMeters(string response)
        {
            string id;
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("record rid="))
                {
                    int start = lines[i].IndexOf("record rid=\"") + 12;
                    int stop = lines[i].IndexOf('"', start);
                    return lines[i].Substring(start, stop - start);
                }
            }

            return "";
        }

        /// <summary>
        /// Retrieves the site that this operation corresponds to as well as all of the open commissioning
        /// cases that are associated with this site
        /// </summary>
        /// <param name="client"></param>
        /// <param name="kvp"></param>
        /// <param name="operationToSite"></param>
        /// <param name="failedMeters"></param>
        /// <returns></returns>
        private async Task<Tuple<string, string, Dictionary<string, string>, Dictionary<string, string>>> GetSite(HttpClient client, KeyValuePair<string, Meter> kvp, Dictionary<string, string> operationToSite, Dictionary<string, string> failedMeters)
        {
            var operationID = kvp.Value.OperationID;
            string cases = "";
            if (!operationToSite.ContainsKey(operationID))
            {
                var response = await GetRecord(client, "bghhvi54m", operationID);
                var sid = GetSiteID(response);

                // Unable to get the site id for this meter so move to next meter
                if (sid == "")
                {
                    FailedMessage = "Unable to retrieve Site from Operation ID";
                    failedMeters.Add(kvp.Key, String.Format("Unable to retrieve Site from Operation ID: {0}", operationID));
                    return new Tuple<string, string, Dictionary<string, string>, Dictionary<string, string>> ("", "", operationToSite, failedMeters);
                }

                operationToSite.Add(operationID, sid);

                // Retrieve commissioning cases that are not completed and attached to this site
                var query = string.Format("{{146.TV.'{0}'}}AND{{174.TV.'Commissioning Request'}}AND{{140.XEX.'I-Completed'}}", sid);
                var caseIDs = await DoQueryRecordID(client, "bghhvievy", query);
                cases = String.Join(",", caseIDs);
            }

            return new Tuple<string, string, Dictionary<string, string>, Dictionary<string, string>>(operationToSite[operationID], cases, operationToSite, failedMeters);
        }

        /// <summary>
        /// Checks if the case that is about to be uploaded is among the list of cases for this site
        /// Sets a goldenCase variable used in the scenario where this device is associated with a
        /// different site, logs data in the infoMeter variable to alert FieldService
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="sid"></param>
        /// <param name="siteToCases"></param>
        /// <param name="goldenCase"></param>
        /// <param name="data"></param>
        /// <param name="failedMeters"></param>
        /// <param name="infoMeters"></param>
        /// <returns></returns>
        private bool CheckCase(string serial, string sid, Dictionary<string, string> siteToCases, ref string goldenCase, ref Dictionary<string, string> data, ref Dictionary<string, string> failedMeters, ref Dictionary<string, string> infoMeters)
        {
            // Check if the case for this proposed FS Report is associated with current site
            // If not, attempt to assign the correct case
            var exit = false;
            var cases = siteToCases[sid].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (!cases.Contains(data["_fid_17"]))
            {
                string originalCase = data["_fid_17"];
                if (!siteToCases[sid].Contains(","))
                    data["_fid_17"] = siteToCases[sid];
                else if (!String.IsNullOrEmpty(goldenCase))
                {
                    data["_fid_17"] = goldenCase;
                    if (failedMeters.ContainsKey(serial))
                        failedMeters.Remove(serial);
                }
                else
                {
                    exit = true;
                    if (failedMeters.ContainsKey(serial))
                        failedMeters[serial] = "Unable to identify usable case ID for this serial number";
                    else
                        failedMeters.Add(serial, "Need golden case id because current case id is associated with different site");
                }

                if (!infoMeters.ContainsKey(serial))
                {
                    if (originalCase != data["_fid_17"])
                        infoMeters.Add(serial, String.Format("Serial number: {0}, originally had case: {1} and now has case: {2}.", serial, originalCase, data["_fid_17"]));
                    else if (failedMeters[serial] == "Unable to identify usable case ID for this serial number")
                        infoMeters.Add(serial, String.Format("Serial number: {0}, originally had case: {1} and app unable to determine correct case.", serial, originalCase));
                }
            }
            else
                goldenCase = data["_fid_17"];

            return exit;
        }

        /// <summary>
        /// Attempts to create a new or edit an FS Report
        /// </summary>
        /// <param name="client"></param>
        /// <param name="oldMeters"></param>
        /// <param name="operationID"></param>
        /// <param name="serial"></param>
        /// <param name="logDir"></param>
        /// <param name="data"></param>
        /// <param name="failedMeters"></param>
        /// <returns></returns>
        private async Task<Tuple<string, bool, Dictionary<string, string>>> UploadFSReport(HttpClient client, string oldMeters, string operationID, string serial, string logDir, Dictionary<string, string> data, Dictionary<string, string> failedMeters)
        {
            // After data has been retrieved, upload meter data to CRM
            // If Successful, continue with uploading the meter point data
            // If Unsuccessful, save response to log file and try next meter
            string oldCase = ScanOldMeters(oldMeters);
            var rid = "";
            var edit = false;
            if (String.IsNullOrEmpty(oldCase))
            {
                var response = await AddRecord(client, "bhfwfquxf", data);
                if (!response.Contains("<errcode>0</errcode>"))
                {
                    var fileName = String.Format("{0}\\{1}-FSReportFailure.txt", logDir, serial);
                    failedMeters.Add(serial, "Failure uploading FS Report, check log file for additional details");
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
                rid = oldCase;
                edit = true;

                // Remove these keys
                data.Remove("_fid_9");
                data.Remove("_fid_13");
                data.Remove("_fid_37");
                if (data.ContainsKey("_fid_21"))
                    data.Remove("_fid_21");

                var response = await GetRecord(client, "bhfwfquxf", rid);
                int start = response.IndexOf("update_id>") + 10;
                var update = response.Substring(start, response.IndexOf("<", start) - start);
                data["rid"] = rid;
                data["update_id"] = update;
                response = await EditRecord(client, "bhfwfquxf", data);
                if (!response.Contains("<errcode>0</errcode>"))
                {
                    var fileName = String.Format("{0}\\{1}-FSReportFailure.txt", logDir, serial);
                    failedMeters.Add(serial, "Failure editing record, check log file for additional details");
                    DumpToLog(fileName, "Failed to edit FS Report, dumping response to log file", response);
                    rid = "";
                }
            }

            return new Tuple<string, bool, Dictionary<string, string>>(rid, edit, failedMeters);
        }

        /// <summary>
        /// Attempts to create new, edit or delete Meter Point Report depending on pre-existing state of 
        /// meter points in CRM and current state according to the meter object that is about to be uploaded
        /// </summary>
        /// <param name="client"></param>
        /// <param name="c"></param>
        /// <param name="rid"></param>
        /// <param name="edit"></param>
        /// <param name="serial"></param>
        /// <param name="logDir"></param>
        /// <param name="failedMeters"></param>
        /// <returns></returns>
        private async Task UploadMeterPoint(HttpClient client, Channel c, string rid, bool edit, string serial, string logDir)
        {
            // Check filters out meter points that have not been commissioned, if this 
            // meter point existed originally but the current user removed it then delete
            // record from CRM
            string query, response;
            List<string> records;
            if (c.Notes == "Not Commissioned" || (c.Phase1 == null && c.Phase2 == null))
            {
                // If user is editting a previous FS Report and an entry exists for a Meter Point but 
                // current user decommissioned that Meter Point, delete the record from CRM
                if (edit)
                {
                    query = String.Format("{{'6'.TV.'{0}'}}AND{{'24'.TV.'{1}'}}", c.ID, rid);
                    records = await DoQueryRecordID(client, "bicmyvvte", query);
                    if (records.Count == 1)
                    {
                        response = await DeleteRecord(client, "bicmyvvte", records[0]);
                        // If Delete operation fails then dump report to log 
                        if (!response.Contains("<errcode>0</errcode>"))
                        {
                            var fileName = String.Format("{0}\\{1}-{2}-MPDeleteFailure.txt", logDir, serial, c.ID);
                            DumpToLog(fileName, "Failed to delete Meter Point Report, dumping response to log file", response);
                        }
                    }
                }

                return;
            }

            // Creates the meter point data object and checks if it currently exists in CRM
            // If it exists, edit the record instead of creating a new record
            var data = CreateMPData(c, rid);
            query = String.Format("{{'6'.TV.'{0}'}}AND{{'24'.TV.'{1}'}}", c.ID, rid);
            records = await DoQueryRecordID(client, "bicmyvvte", query);
            if (records.Count == 1)
            {
                data["rid"] = records[0];
                response = await GetRecord(client, "bicmyvvte", data["rid"]);
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
                var fileName = String.Format("{0}\\{1}-{2}-MPReportFailure.txt", logDir, serial, c.ID);
                DumpToLog(fileName, "Failed to create/edit Meter Point Report, dumping response to log file", response);
            }
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
        /// Deletes a record from a database given a record ID
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
        private async Task<int> DoQueryCount(HttpClient client, string url)
        {
            // Counts the number of results returned from query string, if there 
            // is only one response then continue by performing the query and then
            // retrieve the record id of the one result
            string response = await client.GetStringAsync(url);
            int count = -1;
            if (response.Contains("<errcode>0</errcode>"))
            {
                int start = response.IndexOf("Matches") + 8;
                int stop = response.LastIndexOf("Matches") - 5;
                count = int.Parse(response.Substring(start, stop - start));
            }

            return count;
        }

        /// <summary>
        /// Performs Query operation on specified database using the supplied query string
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="query">String query parameters</param>
        /// <returns>Returns Record ID of query operation</returns>
        private async Task<List<string>> DoQueryRecordID(HttpClient client, string db, string query, string clist = "")
        {
            // Counts the number of results returned from query string, if there 
            // is only one response then continue by performing the query and then
            // retrieve the record id of the one result
            List<string> results = new List<string>();

            string url, response;
            int start, stop;
            if (String.IsNullOrEmpty(clist))
                url = string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQueryCount&ticket={1}&fmt=structured&includeRids=1&query={2}", db, _ticket, query);
            else
                url = string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQueryCount&ticket={1}&fmt=structured&includeRids=1&query={2}&clist={3}", db, _ticket, query, clist);

            int count = await DoQueryCount(client, url);
            int last = 0;
            if (count > 0)
            {
                url = url.Replace("DoQueryCount", "DoQuery");
                response = await client.GetStringAsync(url);

                while (true)
                {
                    start = response.IndexOf("record rid=\"", last) + 12;

                    // Break if string was not found, 11 because 12 was added above
                    if (start == 11)
                        break;

                    stop = response.IndexOf('"', start);
                    results.Add(response.Substring(start, stop - start));
                    last = stop;
                }
            }

            return results;
        }

        /// <summary>
        /// Performs Query operation on specified database using the supplied query string
        /// </summary>
        /// <param name="client">Client used to create http request</param>
        /// <param name="db">String Database ID</param>
        /// <param name="query">String query parameters</param>
        /// <returns>Returns Record ID of query operation</returns>
        private async Task<string> DoQueryRecordString(HttpClient client, string db, string query, string clist = "a")
        {
            // Counts the number of results returned from query string, if there 
            // is only one response then continue by performing the query and then
            // retrieve the record id of the one result
            string url;
            if (String.IsNullOrEmpty(clist))
                url = string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQueryCount&ticket={1}&fmt=structured&includeRids=1&query={2}", db, _ticket, query);
            else
                url = string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_DoQueryCount&ticket={1}&fmt=structured&includeRids=1&query={2}&clist={3}", db, _ticket, query, clist);

            int count = await DoQueryCount(client, url);
            url = url.Replace("DoQueryCount", "DoQuery");
            if (count > 0)
                return await client.GetStringAsync(url);
            else
                return "";
        }

        /// <summary>
        /// Edits a record in a database using the data variable
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
            var bytes = System.IO.File.ReadAllBytes(tuple.Item4);
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
