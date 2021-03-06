﻿using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WpfCommApp.Helpers;

namespace WpfCommApp
{
    public class MainViewModel : ObservableObject
    {
        #region Fields

        private bool _backwardEnabled;
        private bool _forwardEnabled;
        private bool _imported;
        private string _toUploadDir = $@"{ Directory.GetCurrentDirectory() }\ToUpload";

        private IAsyncCommand _backwardPage;
        private ICommand _closeTab;
        private IAsyncCommand _forwardPage;
        private ICommand _importCommand;
        private ICommand _importMeters;
        private ICommand _openTab;
        private ICommand _resizeControl;
        private IAsyncCommand _saveCommand;
        private ICommand _shutdown;
        private ICommand _uploadCommand;

        private ContentTabViewModel _current;
        private Dictionary<string, Meter> _meters;
        private Dictionary<string, SerialComm> _serial;
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

        public Dictionary<string, SerialComm> Serial
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
                    _backwardPage = new AsyncRelayCommand(Backward);

                return _backwardPage;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                if (_closeTab == null)
                    _closeTab = new RelayCommand(p => CloseTab(p as Tuple<string, string>));

                return _closeTab;
            }
        }

        public ICommand ForwardPage
        {
            get
            {
                if (_forwardPage == null)
                    _forwardPage = new AsyncRelayCommand(Forward);

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

        public ICommand ResizeControl
        {
            get
            {
                if (_resizeControl == null)
                    _resizeControl = new RelayCommand(e => PossiblyResizeControl(e as SizeChangedEventArgs));

                return _resizeControl;
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

        public ICommand Shutdown
        {
            get
            {
                if (_shutdown == null)
                    _shutdown = new RelayCommand(p => Cleanup());

                return _shutdown;
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
            Meters = Globals.Meters;
            Serial = Globals.Serials;
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
        ///
        /// Closes the tab that is associated with the serial port that was recently closed
        /// and removes the serial and meter object from the collections that are hosting them
        /// simultaneously saves the meter object. Modifies collections used to control visible
        /// menu tabs
        /// </summary>
        /// <param name="args"></param>
        public async Task CloseTab(Tuple<string, string> args, bool complete = false)
        {
            // Close the comm port if the complete arg is true
            var comms = Globals.Serials;
            if (complete)
                comms[args.Item1].Close();

            // Retrieves the index for the tab in the underlying collection
            var idx = _tabs.FindIndex(t => t.MeterSerialNo == args.Item2);
            var tab = _tabs[idx];

            // Do not remove the first tab if there are other tabs open
            if (_tabs.Where(x => x.Visible == true).Count() > 1 && idx == 0)
                return;

            // "Close" (Hide) the desired tab
            tab.Visible = false;

            // If there are no more visible tabs, close the application and save meter data
            if (_tabs.Where(x => x.Visible == true).Count() == 0)
            {
                ShutdownProcedure();
            }
            // If the current tab is a meter tab and user is viewing this tab
            // Shift to the next open tab and save the meter data for this tab
            else if (CurrentTab == tab)
            {
                string id = CurrentTab.MeterSerialNo;
                await CurrentTab.StopAsync();
                CurrentTab = _tabs[idx - 1];
                CurrentTab.StartAsync();
                ModifyNavigation();
                var meters = Globals.Meters;
                if (complete)
                    meters[id].Commissioned = true;
                meters[id].Save(_toUploadDir);
            }

            ModifyTabs();
        }

        /// <summary>
        /// Creates a new meter tab only if the comport that the new meter tab was requested for 
        /// is not currently in use by another meter being commissioned.
        /// </summary>
        public async Task CreateNewMeter()
        {
            // If the Serial Connection tab is the only tab, create the new meter
            if (_tabs.Count == 1)
                await (CurrentTab.CurrentPage as ConnectViewModel).CreateNewMeter();

            // If Serial Connection is not the only tab iterate over all tabs and 
            // verify that the com port is not currently in use, if it is 
            // return immediately from function otherwise, create a new meter
            else
            {
                string com = (CurrentTab.CurrentPage as ConnectViewModel).COMPORT;
                foreach(ContentTabViewModel ct in _tabs)
                {
                    if (!string.IsNullOrEmpty(ct.MeterSerialNo))
                        if (com == ct.SerialIdx)
                            return;
                }

                await (CurrentTab.CurrentPage as ConnectViewModel).CreateNewMeter();
            }
        }

        /// <summary>
        /// Creates a new meter tab, enables backward button to allow user to get back to Serial Page
        /// Enables forward button because first page is Configuration page and can have no modifications
        /// </summary>
        /// <param name="objects">Tuple containing the serial port index and meter serial number</param>
        public void CreateTab(Tuple<string, string> objects)
        {
            // If the tab does not exist, create one otherwise re-open the tab
            var tab = _tabs.Where(x => x.SerialIdx == objects.Item1 && x.MeterSerialNo == objects.Item2).FirstOrDefault();
            if (tab == null)
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    _tabs.Add(new ContentTabViewModel(objects.Item1, objects.Item2));
                    await CurrentTab.StopAsync();
                    CurrentTab = _tabs.Last();
                    CurrentTab.StartAsync();
                    ModifyNavigation();
                    ModifyTabs();
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    tab.Visible = true;
                    await CurrentTab.StopAsync();
                    CurrentTab = tab;
                    CurrentTab.StartAsync();
                    ModifyNavigation();
                    ModifyTabs();
                });
            }
        }

        /// <summary>
        /// Switches the active meter tab after a user has moved the optical port to a different meter
        /// </summary>
        /// <returns></returns>
        public async Task SwitchMeters()
        {
            // Retrieves the serial number of the meter currently attached to the serial port
            string serialIdx = CurrentTab.SerialIdx;
            string currSerialNo = Serial[serialIdx].SerialNo;
            var tabs = _tabs.Where(x => x.MeterSerialNo == currSerialNo);
            CurrentTab.Visible = false;

            // If the meter exists and there is a tab already created for it then switch to that tab
            // and change to Commissioning page
            if (Meters.ContainsKey(currSerialNo) && tabs.Count() == 1)
            {
                await CurrentTab.StopAsync();
                var tab = tabs.ElementAt(0);
                CurrentTab = tab;
                tab.Visible = true;
                tab.CurrentPage = tab.Pages[1];
            }
            // If the meter exists (imported) but there is not a tab created for it then create the tab
            // and change to the Commissioning page
            else if (Meters.ContainsKey(currSerialNo))
            {
                await CurrentTab.StopAsync();
                CreateTab(new Tuple<string, string>(serialIdx, currSerialNo));
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
                string version = await Serial[serialIdx].GetVersion();
                string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines[2].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)[1].StartsWith("593"))
                {
                    string[] serials = (await Serial[serialIdx].GetChildSerial()).Split(',');
                    for (int i = 0; i < 12; i++)
                    {
                        m.Channels.Add(new Channel(i + 1));
                        m.Channels[i].Serial = serials[i];
                    }
                }

                await CurrentTab.StopAsync();
                CreateTab(new Tuple<string, string>(serialIdx, currSerialNo));
                CurrentTab.CurrentPage = CurrentTab.Pages[1];
            }

            // Initiate the async processes for the current page and modify navigation buttons
            CurrentTab.StartAsync();
            ModifyNavigation();

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
        private async Task Backward()
        {
            var pages = CurrentTab.Pages;
            var current = CurrentTab.CurrentPage;
            var idx = pages.IndexOf(current);

            // If not currently on first page for tab then go back one page
            if (idx > 0)
            {
                await CurrentTab.StopAsync();
                CurrentTab.CurrentPage = pages[idx - 1];
                CurrentTab.StartAsync();
                ModifyNavigation();
            }
            // If currently on first page of tab, then go back to the first tab
            // only if user is currently not on first tab
            else if (CurrentTab.Name != "Serial Connection")
            {
                await CurrentTab.StopAsync();
                CurrentTab = _tabs[0];
                CurrentTab.StartAsync();
                ModifyNavigation();
            }
        }

        /// <summary>
        /// Progresses the window forward one page or progresses user to different tab in tab control
        /// </summary>
        private async Task Forward()
        {
            // The first tab is a control tab thus it's forward operations are different
            if (_tabs.IndexOf(CurrentTab) == 0)
            {
                // if there are more tabs visible than just the first tab
                // go to the next visible tab
                if (_tabs.Where(x => x.Visible == true).Count() > 1)
                {
                    await CurrentTab.StopAsync();
                    CurrentTab = _tabs[1];
                    CurrentTab.StartAsync();
                    ModifyNavigation();
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
                        await CurrentTab.StopAsync();
                        CurrentTab.CurrentPage = pages[idx + 1];
                        CurrentTab.StartAsync();
                        ModifyNavigation();
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
                    await CurrentTab.StopAsync();
                    CurrentTab.CurrentPage = pages[idx + 1];
                    CurrentTab.StartAsync();
                    ModifyNavigation();
                }
                else
                {
                    // Close this tab if we are finished commissioning this meter.
                    await CloseTab(new Tuple<string, string>(CurrentTab.SerialIdx, CurrentTab.MeterSerialNo), true);
                }
            }
        }

        /// <summary>
        /// Runs upon program invocation and imports all meter data from the ToUpload folder
        /// </summary>
        private void Import()
        {
            var importedFileCount = 0;
            foreach (string fileName in Directory.GetFiles(_toUploadDir))
            {
                if (fileName.Contains("txt"))
                    OldImportMeter(fileName);
                else
                    ImportMeter(fileName);

                importedFileCount++;
            }

            _imported = true;
            Globals.Logger.LogInformation($"Imported {importedFileCount} files from {_toUploadDir}");
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
            string name;
            int start;
            if (result == true)
                foreach (string fileName in ofd.FileNames)
                {
                    ImportMeter(fileName);
                    start = fileName.IndexOf("\\") + 1;
                    name = fileName.Substring(start);
                    File.Move(fileName, $@"{_toUploadDir}\{name}");
                }
        }

        /// <summary>
        /// Performs the actual import of meter data by iterating over the lines and setting
        /// appropriate variables
        /// </summary>
        /// <param name="fileName">File location of meter data</param>
        private void ImportMeter(string fileName)
        {
            var meters = Globals.Meters;
            Meter m = new Meter();
            using (var sr = new StreamReader(fileName))
                m = JsonConvert.DeserializeObject<Meter>(sr.ReadToEnd());
            meters.Add(m.ID, m);
        }

        /// <summary>
        /// Performs the actual import of meter data by iterating over the lines and setting 
        /// appropriate variables
        /// </summary>
        /// <param name="fileName">File location of meter data</param>
        private void OldImportMeter(string fileName)
        {
            StreamReader sr = new StreamReader(fileName);
            var meters = Globals.Meters;
            Meter m = new Meter();
            Channel c = null;
            string line;
            string[] split;
            int subtract = 0;
            var header = sr.ReadLine();          // skip meter header row
            line = sr.ReadLine();   // meter properties

            // Set Meter Properties
            split = line.Split(new char[1] { ',' });
            m.ID = split[0];

            // if meter already exist, overwrite data in meter
            if (meters.ContainsKey(m.ID))
                meters.Remove(m.ID);

            m.Floor = split[1];
            m.Location = split[2];
            m.PLCVerified = split[4] == "Yes" ? true : false;
            m.Disposition = int.Parse(split[5]);
            m.FSReturn = split[6] == "1" ? true : false;
            m.OprComplete = split[7] == "1" ? true : false;
            m.Commissioned = split[8] == "1" ? true : false;
            m.OperationID = split[9];

            // Backwards compatible modification
            if (split.Length == 11 && header.Contains("Firmware"))
                m.Firmware = split[10];

            if (split.Length > 11 && header.Contains("Notes"))
                m.Notes = String.Join(",", split.Skip(11));

            line = sr.ReadLine();
            while (true)
            {
                if (line.Contains("CT,Serial,Apartment,C/B#,CT Type,Primary,Secondary,Multiplier,Commissioned,Forced,Reason,Notes"))
                    break;
                m.Notes += $"\r\n{line}";
            }

            // Remove quotations from notes, if any
            m.Notes = m.Notes.Trim(new char[1] { '"' });

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
            meters.Add(m.ID, m);

            // Get the path to save the new meter format
            var path = Path.GetDirectoryName(fileName);
            m.Save(path);

            // Delete the old file
            File.Delete(fileName);
        }

        /// <summary>
        /// Runs filtering operation on tabs that are visible to user in tab control
        /// and tabs that are present within the menu
        /// </summary>
        private void ModifyTabs()
        {
            // Refreshes the filter for the two tab "types"
            Application.Current.Dispatcher.Invoke(() =>
            {
                var curr = CurrentTab;
                ListCollectionView lcv = (ListCollectionView)ViewVisibleTabs;
                lcv.Filter = x => (x as ContentTabViewModel).Visible;
                ListCollectionView mcv = (ListCollectionView)MenuVisibleTabs;
                mcv.Filter = x => !(x as ContentTabViewModel).Visible;
                if (CurrentTab != curr)
                    CurrentTab = curr;
            });
        }

        /// <summary>
        /// Opens a tab once the user clicks on the tab from the menu 
        /// </summary>
        /// <param name="p"></param>
        private async Task OpenTab(string p)
        {
            // Iterates over each tab and once the tab is located sets that tab
            // as the CurrentTab and changes it's visibility value as well as 
            // modifying the collections that control which tabs are viewable
            // in the menu and to the user for interaction
            foreach(var tab in _tabs)
            {
                if (tab.MeterSerialNo == p)
                {
                    // Re-establish serial connection, if port is closed
                    var comms = Globals.Serials;
                    foreach (var comm in comms)
                    {
                        if (comm.Value.SerialNo == tab.MeterSerialNo)
                        {
                            if (!comm.Value.IsOpen)
                                await comm.Value.SetupSerial(comm.Key);
                            break;
                        }
                    }


                    // Work on visual display for user
                    tab.Visible = true;
                    await CurrentTab.StopAsync();
                    CurrentTab = tab;
                    CurrentTab.StartAsync();
                    ModifyNavigation();
                    ModifyTabs();
                    break;
                }
            }
        }

        /// <summary>
        /// Sets the font size for each meter tab depending on the
        /// size of the window
        /// </summary>
        /// <param name="e"></param>
        private void PossiblyResizeControl(SizeChangedEventArgs e)
        {
            if (_tabs.Count > 0 /* && CurrentTab.Pages.IndexOf(CurrentTab.CurrentPage) == 1 */)
            {
                if (e.PreviousSize.Width != 0 && e.PreviousSize.Height != 0)
                {
                    for (int i = 1; i < _tabs.Count; i++)
                    {
                        if (e.NewSize.Width > e.PreviousSize.Width && e.NewSize.Height > e.PreviousSize.Height)
                        {
                            (_tabs[i].Pages[0] as ConfigurationViewModel).FontSize = 30;
                            (_tabs[i].Pages[1] as CommissioningViewModel).FontSize = 28;
                            (_tabs[i].Pages[1] as CommissioningViewModel).LedControlHeight = 32;
                            (_tabs[i].Pages[2] as ReviewViewModel).FontSize = 28;
                            (_tabs[i].Pages[2] as ReviewViewModel).LedControlHeight = 32;
                        }
                        else
                        {
                            (_tabs[i].Pages[0] as ConfigurationViewModel).FontSize = 19;
                            (_tabs[i].Pages[1] as CommissioningViewModel).FontSize = 17;
                            (_tabs[i].Pages[1] as CommissioningViewModel).LedControlHeight = 22;
                            (_tabs[i].Pages[2] as ReviewViewModel).FontSize = 16;
                            (_tabs[i].Pages[2] as ReviewViewModel).LedControlHeight = 22;
                        }
                    }
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
            foreach (KeyValuePair<string, Meter> kvp in Globals.Meters)
                kvp.Value.Save(_toUploadDir);

            Globals.Logger.LogInformation("Saved all meters in memory to disk.");
        }

        /// <summary>
        /// Saves the current state of all meters and then closes the application
        /// </summary>
        private async void ShutdownProcedure()
        {
            await SaveMeters();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Globals.Logger.LogInformation("Closing the application.");
                Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// Methods to run to clean up the App before shutting down
        /// </summary>
        private void Cleanup()
        {
            foreach (var tab in _tabs)
            {
                foreach(var page in tab.Pages)
                {
                    page.Dispose();
                }
            }

            foreach (var meter in Globals.Meters)
            {
                // meter.Value.Close();
            }
            Globals.Meters = null;

            foreach (var serial in Globals.Serials)
            {
                serial.Value.Close();
            }
            Globals.Serials = null;
        }

        /// <summary>
        /// Used to start the upload to crm process if the meters have already been commissioned but an
        /// internet connection was not available on site
        /// </summary>
        private async Task StartCRM()
        {
            if (CurrentTab.Name == "Serial Connection")
            {
                var pages = CurrentTab.Pages;
                var current = CurrentTab.CurrentPage;
                var idx = pages.IndexOf(current);

                await CurrentTab.StopAsync();
                CurrentTab.CurrentPage = pages[idx + 1];
                CurrentTab.StartAsync();
                ModifyNavigation();
            }
        }

        /// <summary>
        /// Enables/Disables the Forward and Backward button when switching tabs
        /// </summary>
        private void ModifyNavigation()
        {
            ForwardEnabled = false;
            BackwardEnabled = false;
            Task.Run(AsyncModifyNavigation);
        }

        /// <summary>
        /// If on a meter tab, wait for the async processes to stop
        /// before enabling the navigation buttons
        /// </summary>
        /// <returns></returns>
        private async Task AsyncModifyNavigation()
        {
            // Wait to set navigation buttons if these pages have async tasks
            if (_current.CurrentPage is ConfigurationViewModel ||
                _current.CurrentPage is CommissioningViewModel ||
                _current.CurrentPage is ReviewViewModel)
            {
                if (_current.CurrentPage is ConfigurationViewModel)
                    while ((_current.CurrentPage as ConfigurationViewModel).AllFunctionsStopped)
                        await Task.Delay(500);
                else if (_current.CurrentPage is CommissioningViewModel)
                    while ((_current.CurrentPage as CommissioningViewModel).AllFunctionsStopped)
                        await Task.Delay(500);
                if (_current.CurrentPage is ReviewViewModel)
                    while ((_current.CurrentPage as ReviewViewModel).AllFunctionsStopped)
                        await Task.Delay(500);
            }

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
