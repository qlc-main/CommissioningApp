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

        private ICommand _backwardPage;
        private ICommand _closeTab;
        private ICommand _forwardPage;
        private ICommand _importCommand;
        private ICommand _importMeters;
        private ICommand _openTab;
        private IAsyncCommand _saveCommand;
        private ICommand _uploadCommand;

        private ContentTabViewModel _current;
        private Dictionary<string, Meter> _meters;
        private ObservableCollection<SerialComm> _serial;
        private List<ContentTabViewModel> _tabs;

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

        public ContentTabViewModel CurrentTab
        {
            get { return _current; }
            set
            {
                if (_current != value)
                {
                    _current = value;
                    OnPropertyChanged(nameof(CurrentTab));
                    TabHandling();
                }
            }
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
            _tabs = new List<ContentTabViewModel>();
            _tabs.Add(new ContentTabViewModel());
            _current = _tabs[0];
            BackwardEnabled = false;

            ViewVisibleTabs = new CollectionViewSource { Source = _tabs }.View;
            MenuVisibleTabs = new CollectionViewSource { Source = _tabs }.View;

            ModifyTabs();
        }

        #endregion

        #region Methods

        #region Public 

        /// <summary>
        /// Closes the tab that is associated with the serial port that was recently closed
        /// and removes the serial and meter object from the collections that are hosting them
        /// simultaneously saves the meter object. Modifies collections used to control visible
        /// menu tabs
        /// </summary>
        /// <param name="args"></param>
        public void CloseTab(Tuple<int, string> args)
        {
            var comms = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
            comms.RemoveAt(args.Item1);
            var meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
            meters[args.Item2].Save(string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" }));
            meters.Remove(args.Item2);
            _tabs.Remove(_tabs.Where(x => x.MeterSerialNo == args.Item2).First());
            ModifyTabs();
        }

        /// <summary>
        /// Creates a new meter tab, enables backward button to allow user to get back to Serial Page
        /// Enables forward button because first page is Configuration page and can have no modifications
        /// </summary>
        /// <param name="objects">Tuple containing the serial port index and meter serial number</param>
        public void CreateTab(Tuple<int, string> objects)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _tabs.Add(new ContentTabViewModel(objects.Item1, objects.Item2));
                CurrentTab = _tabs.Last();
                ModifyTabs();
            });

            BackwardEnabled = true;
            ForwardEnabled = false;
        }

        /// <summary>
        /// Switches the active meter tab after a user has moved the optical port to a different meter
        /// </summary>
        /// <returns></returns>
        public async Task SwitchMeters()
        {
            // Retrieves the serial number of the meter currently attached to the serial port
            int serialIdx = (CurrentTab.CurrentPage as CommissioningViewModel).IDX;
            string currSerialNo = Serial[serialIdx].SerialNo;
            var tabs = _tabs.Where(x => x.MeterSerialNo == currSerialNo);
            CurrentTab.Visible = false;

            // If the meter exists and there is a tab already created for it then switch to that tab
            // and change to Commissioning page
            if (Meters.ContainsKey(currSerialNo) && tabs.Count() == 1)
            {
                var tab = tabs.ElementAt(0);
                CurrentTab = _tabs[_tabs.IndexOf(tab)];
                tab.Visible = true;
                tab.CurrentPage = tab.Pages[1];
            }
            // If the meter exists (imported) but there is not a tab created for it then create the tab
            // and change to the Commissioning page
            else if (Meters.ContainsKey(currSerialNo))
            {
                CreateTab(new Tuple<int, string>(serialIdx, currSerialNo));
                CurrentTab.CurrentPage = CurrentTab.Pages[1];
            }
            // If the meter does not exist, create a new meter and a new tab
            // then change to the Commissioning page
            else
            {
                Meter m = new Meter();
                m.ID = currSerialNo;
                Meters.Add(currSerialNo, m);

                // Sets the size of the meter based on the type of meter connected
                string version = Serial[serialIdx].GetVersion();
                string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines[2].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)[1].StartsWith("593"))
                {
                    m.Size = 12;
                    string[] serials = Serial[serialIdx].GetChildSerial().Split(',');
                    for (int i = 0; i < m.Size; i++)
                    {
                        m.Channels.Add(new Channel(i + 1));
                        m.Channels[i].Serial = serials[i];
                    }
                }

                CreateTab(new Tuple<int, string>(serialIdx, currSerialNo));
                CurrentTab.CurrentPage = CurrentTab.Pages[1];
            }

            // Starts the async call for retrieving the phase diagnostic from the meter
            (CurrentTab.CurrentPage as CommissioningViewModel).StartAsync.Execute(null);

            // Modifies the collections that represent which tabs should be visible in the menu and which tabs
            // are present in the tab control for user interaction
            Application.Current.Dispatcher.Invoke(() =>
            {
                ModifyTabs();
            });
        }

        #endregion

        #region Private

        /// <summary>
        /// Allows user to progress to a previous page or if at the first page, a previous tab
        /// </summary>
        private void Backward()
        {
            var pages = CurrentTab.Pages;
            var current = CurrentTab.CurrentPage;
            var idx = pages.IndexOf(current);

            // If not currently on first page for tab then go back one page
            if (idx > 0)
            {
                CurrentTab.CurrentPage = pages[idx - 1];
                ForwardEnabled = true;
                BackwardEnabled = true;
            }
            // If currently on first page of tab, then go back to the first tab
            // only if user is currently not on first tab
            else
            {
                if (CurrentTab.Name != "Serial Connection")
                {
                    CurrentTab = _tabs[0];
                    ForwardEnabled = true;
                    BackwardEnabled = false;
                }
            }
        }

        /// <summary>
        /// Closes a tab given the title string for that tab
        /// </summary>
        /// <param name="p"></param>
        private void CloseTab(string p, bool complete = false)
        {
            // Retrieves the index for the tab in the underlying collection
            int idx = _tabs.Select((value, index) => new { value, index })
                        .Where(x => x.value.Name == p && x.value.Visible == true)
                        .Select(x => x.index)
                        .Take(1)
                        .ElementAt(0);

            // Do not remove the first tab if there are other tabs open
            if (_tabs.Where(x => x.Visible == true).Count() > 1 && idx == 0)
                return;

            // "Close" (Hide) the desired tab
            _tabs[idx].Visible = false;

            // If there are no more visible tabs, close the application and save meter data
            if (_tabs.Where(x => x.Visible == true).Count() == 0)
            {
                ShutdownProcedure();
            }
            // If the current tab is a meter tab and user is viewing this tab
            // Shift to the next open tab and then 
            else if (CurrentTab == _tabs[idx])
            {
                string id = CurrentTab.MeterSerialNo;
                CurrentTab = _tabs[idx - 1];
                var meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
                if (complete)
                    meters[id].Commissioned = true;
                meters[id].Save(string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" }));
            }

            ModifyTabs();
        }

        /// <summary>
        /// Progresses the window forward one page or progresses user to different tab in tab control
        /// </summary>
        private void Forward()
        {
            // The first tab is a control tab thus it's forward operations are different
            if (_tabs.IndexOf(CurrentTab) == 0)
            {
                // if there are more tabs visible than just the first tab
                // go to the next visible tab
                if (_tabs.Where(x => x.Visible == true).Count() > 1)
                {
                    CurrentTab = _tabs[1];

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
                    CloseTab(CurrentTab.Name, true);

                    if (_tabs.IndexOf(CurrentTab) == 0)
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

        /// <summary>
        /// Runs upon program invocation and imports all meter data from the ToUpload folder
        /// </summary>
        private void Import()
        {
            string dir = string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" });
            foreach(string file in Directory.GetFiles(dir))
                ImportMeter(file);

            _imported = true;
        }

        /// <summary>
        /// Allows user to select specific files to import as meter objects
        /// </summary>
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

        /// <summary>
        /// Performs the actual import of meter data by iterating over the lines and setting 
        /// appropriate variables
        /// </summary>
        /// <param name="fileName">File location of meter data</param>
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
            m.Size = int.Parse(split[3]);
            m.PLCVerified = split[4] == "Yes" ? true : false;
            m.Disposition = int.Parse(split[5]);
            m.FSReturn = split[6] == "1" ? true : false;
            m.OprComplete = split[7] == "1" ? true : false;
            m.Commissioned = split[8] == "1" ? true : false;

            while ((line = sr.ReadLine()) != null)
            {
                split = line.Split(new char[] { ',' });
                if (int.Parse(split[0]) % 2 == 1)
                {
                    c = new Channel(int.Parse(split[0]) - subtract);
                    m.Channels.Add(c);
                    c.Serial = split[1];
                    c.Primary = split[5];
                    c.Secondary = split[6];
                    c.ApartmentNumber = split[2];
                    c.BreakerNumber = split[3];
                    c.CTType = split[4];
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

        /// <summary>
        /// Runs filtering operation on tabs that are visible to user in tab control
        /// and tabs that are present within the menu
        /// </summary>
        private void ModifyTabs()
        {
            // Refreshes the filter for the two tab "types"
            ListCollectionView lcv = (ListCollectionView)ViewVisibleTabs;
            lcv.Filter = x => (x as ContentTabViewModel).Visible;
            ListCollectionView mcv = (ListCollectionView)MenuVisibleTabs;
            mcv.Filter = x => !(x as ContentTabViewModel).Visible;
        }

        /// <summary>
        /// Opens a tab once the user clicks on the tab from the menu 
        /// </summary>
        /// <param name="p"></param>
        private void OpenTab(string p)
        {
            // Iterates over each tab and once the tab is located sets that tab
            // as the CurrentTab and changes it's visibility value as well as 
            // modifying the collections that control which tabs are viewable
            // in the menu and to the user for interaction
            foreach(var tab in _tabs)
            {
                if (tab.MeterSerialNo == p)
                {
                    tab.Visible = true;
                    CurrentTab = tab;
                    ModifyTabs();
                    return;
                }
            }
        }

        /// <summary>
        /// Iterates over each meter in the Meters collection and writes the data to a file
        /// that can be imported later or uploaded to CRM as needed
        /// </summary>
        /// <returns></returns>
        private async Task SaveMeters()
        {
            // Creates the string for the directory that files will be written to, checks if the directory exists
            // if not the directory is created and then each meter is written to file
            string dir = string.Join("\\", new string[] { Directory.GetCurrentDirectory(), "ToUpload" });
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            foreach (KeyValuePair<string, Meter> kvp in (Application.Current.Properties["meters"] as Dictionary<string, Meter>))
                kvp.Value.Save(dir);
        }

        /// <summary>
        /// Saves the current state of all meters and then closes the application
        /// </summary>
        private async void ShutdownProcedure()
        {
            await Task.Run(SaveMeters);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// Used to start the upload to crm process if the meters have already been commissioned but an
        /// internet connection was not available on site
        /// </summary>
        private void StartCRM()
        {
            if (CurrentTab.Name == "Serial Connection")
            {
                var pages = CurrentTab.Pages;
                var current = CurrentTab.CurrentPage;
                var idx = pages.IndexOf(current);

                CurrentTab.CurrentPage = pages[idx + 1];
                BackwardEnabled = true;
            }
        }

        /// <summary>
        /// Enables/Disables the Forward and Backward button when switching tabs
        /// </summary>
        private void TabHandling()
        {
            // Send message to enable forward button, if current page for current tab marked as complete
            if (_current.CurrentPage.Completed)
                ForwardEnabled = true;
            else
                ForwardEnabled = false;

            // Disable backwards button if on first tab and first page otherwise enable
            if (_current.MeterSerialNo == "" && _current.CurrentPage.Name == "Serial Connection")
                BackwardEnabled = false;
            else
                BackwardEnabled = true;
        }

        #endregion

        #endregion
    }
}
