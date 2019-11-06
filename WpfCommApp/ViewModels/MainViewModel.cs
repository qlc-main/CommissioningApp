using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace WpfCommApp
{
    public class MainViewModel : ObservableObject
    {
        #region Fields

        private bool _backwardEnabled;
        private bool _forwardEnabled;
        private bool _imported;
        private Dictionary<string, Meter> _meters;
        private int _tabIndex;

        private ICommand _backwardPage;
        private ICommand _closeTab;
        private ICommand _forwardPage;
        private ICommand _importCommand;
        private ICommand _importMeters;
        private ICommand _openTab;
        private IAsyncCommand _saveCommand;
        private ICommand _uploadCommand;

        private ObservableCollection<SerialComm> _serial;
        private ObservableCollection<ContentTab> _tabs;

        #endregion

        #region Properties

        public bool BackwardEnabled
        {
            get { return _backwardEnabled; }
            set
            {
                if (_backwardEnabled != value)
                {
                    _backwardEnabled = value;
                    OnPropertyChanged(nameof(BackwardEnabled));
                }
            }
        }

        public ContentTab CurrentTab
        {
            get { return TabIndex < 0 ? null : Tabs[TabIndex]; }
        }

        public bool ForwardEnabled
        {
            get { return _forwardEnabled; }
            set
            {
                if (_forwardEnabled != value)
                {
                    _forwardEnabled = value;
                    OnPropertyChanged(nameof(ForwardEnabled));
                }
            }
        }

        public int TabIndex
        {
            get { return _tabIndex; }
            set
            {
                if (_tabIndex != value)
                {
                    _tabIndex = value;
                    OnPropertyChanged(nameof(TabIndex));
                }
            }
        }

        public Dictionary<string, Meter> Meters
        {
            get { return _meters; }
            set { _meters = value; OnPropertyChanged(nameof(Meters)); }
        }

        public ObservableCollection<SerialComm> Serial
        {
            get { return _serial; }
            set { _serial = value; OnPropertyChanged(nameof(Serial)); }
        }

        public ObservableCollection<ContentTab> Tabs
        {
            get { return _tabs; }
            set { _tabs = value; }
        }

        public ICollectionView MenuVisibleTabs { get; set; }

        public ICollectionView ViewVisibleTabs { get; set; }

        #endregion

        #region Commands

        public ICommand BackwardPage
        {
            get
            {
                if (_backwardPage == null)
                    _backwardPage = new RelayCommand(p => Backward());

                return _backwardPage;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                if (_closeTab == null)
                    _closeTab = new RelayCommand(p => CloseTab(p as string));

                return _closeTab;
            }
        }

        public ICommand ForwardPage
        {
            get
            {
                if (_forwardPage == null)
                    _forwardPage = new RelayCommand(p => Forward());

                return _forwardPage;
            }
        }

        public ICommand ImportCommand
        {
            get
            {
                if (_importCommand == null)
                    _importCommand = new RelayCommand(p => ImportLocation());

                return _importCommand;
            }
        }

        public ICommand ImportMeters
        {
            get
            {
                if (_importMeters == null)
                    _importMeters = new RelayCommand(p => Import(), p => !_imported);

                return _importMeters;
            }
        }

        public ICommand OpenCommand
        {
            get
            {
                if (_openTab == null)
                    _openTab = new RelayCommand(p => OpenTab(p as string));

                return _openTab;
            }
        }

        public IAsyncCommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                    _saveCommand = new AsyncRelayCommand(SaveMeters, () => { return true; });

                return _saveCommand;
            }
        }

        public ICommand UploadCommand
        {
            get
            {
                if (_uploadCommand == null)
                    _uploadCommand = new RelayCommand(p => StartCRM());

                return _uploadCommand;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainViewModel()
        {
            Meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
            Serial = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
            Tabs = new ObservableCollection<ContentTab>();
            Tabs.Add(new ContentTab(-1));
            TabIndex = 0;
            BackwardEnabled = false;

            ViewVisibleTabs = new CollectionViewSource { Source = Tabs }.View;
            MenuVisibleTabs = new CollectionViewSource { Source = Tabs }.View;

            ModifyTabs();
        }

        #endregion

        #region Methods

        #region Public 

        // Functions necessary to create a new Meter Tab
        // Enables backward button to allow user to get back to Serial Page
        // Enables forward button because default page is the Configuration Page
        public void CreateTab(Tuple<int, string> objects)
        {
            Tabs.Add(new ContentTab(Tabs.Count - 1, objects.Item1, objects.Item2));
            TabIndex = Tabs.Count - 1;
            BackwardEnabled = true;
            ForwardEnabled = true;
        }

        public void SwitchMeters()
        {
            // Retrieves the serial number of the meter currently attached to the serial port
            int serialIdx = (CurrentTab.CurrentPage as CommissioningViewModel).IDX;
            string currSerialNo = Serial[serialIdx].SerialNo;
            var tabs = _tabs.Where(x => x.MeterSerialNo == currSerialNo);
            CurrentTab.Visible = false;

            // If the meter exists and there is a tab already created for it then switch to that tab
            // and change the page to the Commissioning screen
            if (Meters.ContainsKey(currSerialNo) && tabs.Count() == 1)
            {
                tabs.ElementAt(0).CurrentPage = tabs.ElementAt(0).Pages[1];
                TabIndex = _tabs.IndexOf(tabs.ElementAt(0));
            }
            // If the meter exists but there is not a tab created for it then create the tab
            // and change the page to the Commissioning screen
            else if (Meters.ContainsKey(currSerialNo))
            {
                CreateTab(new Tuple<int, string>(serialIdx, currSerialNo));
                CurrentTab.CurrentPage = CurrentTab.Pages[1];
            }
            // If the meter does not exist, create a new meter and a new tab
            // then change the page to the Commissioning screen
            else
            {
                Meter m = new Meter();
                Meters.Add(currSerialNo, m);
                CreateTab(new Tuple<int, string>(serialIdx, currSerialNo));
                CurrentTab.CurrentPage = CurrentTab.Pages[1];
            }

            // Starts the async call for retrieving the phase diagnostic from the meter
            (CurrentTab.CurrentPage as CommissioningViewModel).StartAsync.Execute(null);

            ModifyTabs();
        }

        #endregion

        #region Private

        private void Backward()
        {
            var pages = CurrentTab.Pages;
            var current = CurrentTab.CurrentPage;
            var idx = pages.IndexOf(current);

            if (idx > 0)
            {
                CurrentTab.CurrentPage = pages[idx - 1];
                ForwardEnabled = true;
                BackwardEnabled = true;
            }
            else
            {
                if (CurrentTab.Name != "Serial Connection")
                {
                    TabIndex = 0;
                    ForwardEnabled = true;
                    BackwardEnabled = false;
                }
            }
        }

        private void CloseTab(string p)
        {
            int idx = Tabs.Select((value, index) => new { value, index })
                        .Where(x => x.value.Name == p && x.value.Visible == true)
                        .Select(x => x.index)
                        .Take(1)
                        .ElementAt(0);

            // Do not remove the first tab if there are other tabs open
            if (Tabs.Where(x => x.Visible == true).Count() > 1 && idx == 0)
                return;

            // Remove the tab and then adjust the TabIndex to shift to other open tabs
            Tabs[idx].Visible = false;
            if (Tabs.Where(x => x.Visible == true).Count() == 0)
                Application.Current.Shutdown();
            else if (TabIndex == idx)
            {
                TabIndex -= 1;
                if (p.Contains("(") && p.Contains(")"))
                {
                    int start = p.IndexOf("(") + 1;
                    string id = p.Substring(start, p.IndexOf(")") - start);
                    var meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
                    meters[id].Commissioned = true;
                    meters[id].Save(string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" }));
                }
            }
            else
            {
                var save = TabIndex;
                TabIndex = 0;
                TabIndex = save;
            }

            ModifyTabs();
        }

        /// <summary>
        /// Runs filtering operation on tabs that are visible to user in tab control
        /// and tabs that are present within the menu
        /// </summary>
        private void ModifyTabs()
        {
            // Refreshes the filter for the two tab "types"
            ListCollectionView lcv = (ListCollectionView)ViewVisibleTabs;
            lcv.Filter = x => (x as ContentTab).Visible;
            ListCollectionView mcv = (ListCollectionView)MenuVisibleTabs;
            mcv.Filter = x => !(x as ContentTab).Visible;
        }

        /// <summary>
        /// Progresses the window forward one page or progresses user to different tab in tab control
        /// </summary>
        private void Forward()
        {
            // The first tab is a control tab thus it's forward operations are different
            if (TabIndex == 0)
            {
                // if there are more tabs visible than just the first tab
                // go to the next visible tab
                if (Tabs.Where(x => x.Visible == true).Count() > 1)
                {
                    TabIndex = 1;

                    var current = CurrentTab.CurrentPage;
                    if (current.Completed)
                        ForwardEnabled = true;
                    else
                        ForwardEnabled = false;
                }

                // If there are no other visible tabs then assuming that the user now wants to
                // input their credentials into the login screen so they can upload into CRM
                else
                {
                    var pages = CurrentTab.Pages;
                    var page = CurrentTab.CurrentPage;
                    var idx = pages.IndexOf(page);

                    if (idx < pages.Count - 1)
                    {
                        CurrentTab.CurrentPage = pages[idx + 1];
                        if (CurrentTab.CurrentPage.Completed)
                            ForwardEnabled = true;
                        else
                            ForwardEnabled = false;
                    }
                }
            }
            // if user is on any other tab but the first tab, go to the next page if there are additional pages
            // if user is at last page for this tab, close the tab and progress to login screen if there are no
            // more tabs besides the first tab otherwise do nothing
            else
            {
                var pages = CurrentTab.Pages;
                var current = CurrentTab.CurrentPage;
                var idx = pages.IndexOf(current);

                if (idx < pages.Count - 1)
                {
                    CurrentTab.CurrentPage = pages[idx + 1];
                    if (CurrentTab.CurrentPage.Completed)
                        ForwardEnabled = true;
                    else
                        ForwardEnabled = false;
                }
                else
                {
                    // Close this tab if we are finished commissioning this meter.
                    CloseTab(CurrentTab.Name);

                    if (TabIndex == 0)
                    {
                        pages = CurrentTab.Pages;
                        current = CurrentTab.CurrentPage;
                        idx = pages.IndexOf(current);

                        if (idx < pages.Count - 1)
                        {
                            CurrentTab.CurrentPage = pages[idx + 1];
                            if (CurrentTab.CurrentPage.Completed)
                                ForwardEnabled = true;
                            else
                                ForwardEnabled = false;
                        }
                    }
                }
            }

            BackwardEnabled = true;
        }

        private void Import()
        {
            string dir = string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" });
            foreach(string file in Directory.GetFiles(dir))
                ImportMeter(file);

            _imported = true;
        }

        private void ImportLocation()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            var result = ofd.ShowDialog();
            if (result == true)
                foreach (string fileName in ofd.FileNames)
                    ImportMeter(fileName);
        }

        private void ImportMeter(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            var meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
            Meter m = new Meter();
            Channel c = null;
            string line;
            string[] split;
            int subtract = 0;
            sr.ReadLine();          // skip meter header row
            line = sr.ReadLine();   // meter properties
            sr.ReadLine();          // skip the header line for the Channels

            // Set Meter Properties
            split = line.Split(new char[1] { ',' });
            m.ID = split[0];

            // if meter already exist, overwrite data in meter
            if (meters.ContainsKey(m.ID))
                meters.Remove(m.ID);

            m.Floor = split[1];
            m.Location = split[2];
            m.PLCVerified = split[3] == "Yes" ? true : false;
            m.Disposition = int.Parse(split[4]);
            m.FSReturn = split[5] == "1" ? true : false;
            m.OprComplete = split[6] == "1" ? true : false;
            m.Commissioned = split[7] == "1" ? true : false;

            while ((line = sr.ReadLine()) != null)
            {
                split = line.Split(new char[] { ',' });
                if (int.Parse(split[0]) % 2 == 1)
                {
                    c = new Channel(int.Parse(split[0]) - subtract);
                    m.Channels.Add(c);
                    c.Serial = split[1];
                    c.ApartmentNumber = split[2];
                    c.BreakerNumber = split[3];
                    c.CTType = split[4];
                    c.Primary = split[5];
                    c.Secondary = split[6];
                    if (string.IsNullOrEmpty(split[8])) { c.Phase1 = null; }
                    else { c.Phase1 = bool.Parse(split[8]); }
                    c.Forced[0] = bool.Parse(split[9]);
                    c.Reason[0] = split[10];
                    c.Notes = split[11];
                }
                else
                {
                    if (string.IsNullOrEmpty(split[8])) { c.Phase2 = null; }
                    else { c.Phase2 = bool.Parse(split[8]); }
                    c.Forced[1] = bool.Parse(split[9]);
                    c.Reason[1] = split[10];
                    subtract++;
                }
            }

            sr.Close();
            m.Size = m.Channels.Count;
            meters.Add(m.ID, m);
        }

        private void OpenTab(string p)
        {
            foreach(var tab in Tabs)
            {
                if (tab.MeterSerialNo == p)
                {
                    tab.Visible = true;
                    TabIndex = Tabs.IndexOf(tab);
                    ModifyTabs();
                    return;
                }
            }

            ModifyTabs();
        }

        private async Task SaveMeters()
        {
            string dir = string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" });
            foreach (KeyValuePair<string, Meter> kvp in (Application.Current.Properties["meters"] as Dictionary<string, Meter>))
                kvp.Value.Save(dir);
        }

        private void StartCRM()
        {
            if (TabIndex == 0 && CurrentTab.Name == "Serial Connection")
            {
                var pages = CurrentTab.Pages;
                var current = CurrentTab.CurrentPage;
                var idx = pages.IndexOf(current);

                CurrentTab.CurrentPage = pages[idx + 1];
                BackwardEnabled = true;
            }
        }

        #endregion

        #endregion
    }
}
