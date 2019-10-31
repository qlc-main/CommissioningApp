
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private bool _completed;

        private string _email;
        private string _password;
        private string _ticket;

        private int _idx;

        private ICommand _login;

        #endregion

        #region Constructors

        public LoginViewModel()
        {

        }

        #endregion

        #region Properties

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

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        public bool Completed
        {
            get
            {
                return _completed;
            }
        }

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
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

        #region Methods

        private void CRMWrapper(PasswordBox p)
        {
            _password = p.Password;
            Task.Run(CreateCRM);
        }

        private async Task CreateCRM()
        {
            HttpClient client = new HttpClient();

            if (!await Authenticate(client))
                return;

            foreach (Meter m in (Application.Current.Properties["meters"] as ObservableCollection<Meter>))
            {
                bool opt = true;
                string query, response, rid;
                Dictionary<string, string> data;
                if (opt) /* operation id provided */
                {
                    rid = "";
                    response = await GetRecord(client, "bghhvi54m", rid);
                    string sID = GetSiteID(response);
                    query = string.Format("{{'391'.TV.'{0}'}}AND{{'186'.TV.'{1}'}}", sID, m.ID);
                    rid = await DoQuery(client, "bghhviw72", query);

                    response = await GetRecord(client, "bghhviw72", rid);
                    data = CreateFSReportData(response, m, opt);
                }
                else
                {
                    query = string.Format("{{'16'.TV.'{0}'}}", m.ID);
                    rid = await DoQuery(client, "bgs8pzryj", query);

                    response = await GetRecord(client, "bgs8pzryj", rid);
                    data = CreateFSReportData(response, m, opt);
                }

                response = await AddRecord(client, "bhfwfquxf", data);
                if (!response.Contains("<errcode>0</errcode>"))
                {
                    // failed to create record , print out meaningful message
                }


                int start = response.IndexOf("<rid>") + 5;
                rid = response.Substring(start, response.IndexOf("</rid>") - start);
                await UploadFile(client, "bhfwfquxf", new Tuple<string, string, string, string>("24", m.ID + ".txt", rid, String.Format("{0}\\ToUpload\\{1}.txt", Directory.GetCurrentDirectory(), m.ID)));
                foreach (Channel c in m.Channels)
                {
                    if ( c.Notes == "Not Commissioned" || (c.Phase1 == null && c.Phase2 == null) )
                        continue;
                    data = CreateMPData(c, m, data, rid);
                    response = await AddRecord(client, "bicmyvvte", data);

                    if (!response.Contains("<errcode>0</errcode>"))
                    {
                        // failed to create record , print out meaningful message
                    }
                }
            }
        }

        private async Task<bool> Authenticate(HttpClient client)
        {
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

        private async Task<string> DoQuery(HttpClient client, string db, string query)
        {
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

        private async Task<string> GetRecord(HttpClient client, string db, string rid)
        {
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_GetRecordInfo&ticket={1}&rid={2}", db, _ticket, rid));
        }

        private async Task<string> AddRecord(HttpClient client, string db, Dictionary<string, string> info)
        {
            string append = string.Join("&", info.Select(x => string.Join("=", x.Key, x.Value)));
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_AddRecord&{1}", db, append));
        }

        private async Task<bool> UploadFile(HttpClient client, string db, Tuple<string, string, string, string> tuple)
        {
            var bytes = File.ReadAllBytes(tuple.Item4);
            var base64 = Convert.ToBase64String(bytes);
            var xmlContent = String.Format("<qdbapi>\n\t<ticket>{0}</ticket>\n\t<udata>mydata</udata><field fid=\"{1}\" filename=\"{2}\">{3}</field>\n\t<rid>{4}</rid>\n</qdbapi>", _ticket, tuple.Item1, tuple.Item2, base64, tuple.Item3);
            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
            var httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format("https://quadlogic.quickbase.com/db/{0}?", db)),
                Headers = { { "QUICKBASE-ACTION", "API_UploadFile"} },
                Content = content
            };
            var response = await client.SendAsync(httpRequestMessage);
            var stringResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(stringResponse);

            return true;
        }

        private string GetSiteID(string response)
        {
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for(int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<fid>93</fid>"))
                {
                    string line = lines[i += 3];
                    return line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
            }

            return "";
        }

        private Dictionary<string, string> CreateFSReportData(string response, Meter meter, bool opt)
        {
            string line;
            Dictionary<string, string> data = new Dictionary<string, string>();
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
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

        private Dictionary<string, string> CreateMPData(Channel channel, Meter m, Dictionary<string, string> data, string rid)
        {
            data.Clear();
            data["_fid_6"] = channel.ID.ToString();                                         // Meter Point
            data["_fid_7"] = channel.ApartmentNumber;                                       // Apt/Load
            data["_fid_8"] = channel.BreakerNumber;                                         // Breakers
            data["_fid_9"] = string.Format("{0}:{1}", channel.Primary, channel.Secondary);  // CT Ratio
            data["_fid_10"] = channel.CTType;                                               // CT Type
            data["_fid_21"] = ( int.Parse(channel.Primary) / 100 ).ToString();              // Multiplier
            data["_fid_24"] = rid;                                                          // FS Report
            data["_fid_40"] = string.Format("{0};{1};{2};{3}", channel.Serial, channel.Reason[0], channel.Reason[1], channel.Notes);
            data["ticket"] = _ticket;                                                       // Authentication ticket

            return data;
        }
        
        #endregion
    }
}
