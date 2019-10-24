
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

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

        private IAsyncCommand _login;

        #endregion

        #region Constructors

        public LoginViewModel()
        {

        }

        #endregion

        #region Properties

        public string EmailAddress { get; set; }
        public string Password { get; set; }

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

        public IAsyncCommand Login
        {
            get
            {
                if (_login == null)
                    _login = new AsyncRelayCommand(CreateCRM, () => { return true; });

                return _login;
            }
        }

        #endregion

        #region Methods

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
                rid = response.Substring(start, response.IndexOf("</rid>") - start + 1);
                foreach(Channel c in m.Channels)
                {
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
            foreach(string line in response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains("errcode") && !line.Contains(">0<"))
                    return false;

                if (line.Contains("ticket"))
                {
                    int first = line.IndexOf(">");
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
                int stop = response.LastIndexOf("Matches") - 4;
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
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_GetRecordInfo&ticket={0}&rid={1}", db, _ticket, rid));
        }

        private async Task<string> AddRecord(HttpClient client, string db, Dictionary<string, string> info)
        {
            string append = string.Join("&", info.Select(x => string.Join("=", x.Key, x.Value)));
            return await client.GetStringAsync(string.Format("https://quadlogic.quickbase.com/db/{0}?a=API_AddRecord&{1}", db, append));
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
            Dictionary<string, string> results = new Dictionary<string, string>();
            string[] lines = response.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                // Case # is 17
                // Device ID is 18
                // Device Issue Record is 21
                if (lines[i].Contains("<fid>8</fid>") && !opt)
                {
                    line = lines[i += 3];
                    results["_fid_17"] = line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
                else if (lines[i].Contains("<fid>3</fid>") && !opt)
                {
                    line = lines[i += 3];
                    results["_fid_21"] = line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
                else if (lines[i].Contains("<fid>15</fid>") && !opt)
                {
                    line = lines[i += 3];
                    results["_fid_18"] = line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
                else if (lines[i].Contains("<fid>317</fid>") && opt)
                {
                    line = lines[i += 3];
                    results["_fid_17"] = line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
                else if (lines[i].Contains("<fid>104</fid>") && opt)
                {
                    line = lines[i += 3];
                    results["_fid_18"] = line.Substring(line.IndexOf(">"), line.LastIndexOf("<") - line.IndexOf(">") + 1);
                }
                else if ((results.Count == 3 && !opt) || results.Count == 2 && opt)
                    break;
            }

            results["_fid_9"] = "17";                                       // Activity value to indicate commissioning
            results["_fid_12"] = DateTime.Today.ToString("MM-dd-yyyy");     // Date that work performed
            results["_fid_13"] = "2";                                       // Duration of work
            results["_fid_85"] = "10";                                      // Disposition (No Problem Found)
            results["_fid_23"] = "1";                                       // No FS return required
            results["_fid_121"] = "1";                                      // Opr Complete
            results["_fid_72"] = meter.Floor;                               // Device Location

            return results;
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
            data["_fid_29"] = m.PLCVerified ? "1" : "0";                                    // PLC / RS485 Verified
            data["_fid_40"] = string.Format("{0};{1};{2};{3}", channel.Serial, channel.Reason[0], channel.Reason[1], channel.Notes);

            return data;
        }

        #endregion
    }
}
