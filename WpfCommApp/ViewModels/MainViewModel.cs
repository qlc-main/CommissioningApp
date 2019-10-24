using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace WpfCommApp
{
    
    public class MainViewModel : ObservableObject
    {
        #region Fields

        private ICommand _forwardPage;
        private ICommand _backwardPage;
        private ICommand _closeTab;
        private ICommand _openTab;
        private IAsyncCommand _saveCommand;

        private ObservableCollection<Meter> _meters;
        private ObservableCollection<SerialComm> _serial;
        private ObservableCollection<ContentTab> _tabs;
        private ObservableCollection<ContentTab> _menuTabs;
        private ObservableCollection<ContentTab> _viewTabs;

        private int _tabIndex;

        private bool _forwardEnabled;
        private bool _backwardEnabled;

        #endregion

        #region Properties

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

        public int TabIndex
        {
            get { return _tabIndex; }
            set
            {
                if (_tabIndex != value)
                {
                    _tabIndex = value;
                    foreach(var tab in _tabs)
                    {
                        if (tab.Visible)
                        {
                            if (_menuTabs.Contains(tab))
                                _menuTabs.Remove(tab);
                            if (!_viewTabs.Contains(tab))
                                _viewTabs.Add(tab);
                        }
                        else
                        {
                            if (!_menuTabs.Contains(tab))
                                _menuTabs.Add(tab);
                            if (_viewTabs.Contains(tab))
                                _viewTabs.Remove(tab);
                        }
                    }

                    OnPropertyChanged(nameof(TabIndex));
                    OnPropertyChanged(nameof(ViewTabs));
                    OnPropertyChanged(nameof(MenuTabs));
                }
            }
        }

        public ContentTab CurrentTab
        {
            get { return TabIndex < 0 ? null : Tabs[TabIndex]; }
        }

        public ObservableCollection<Meter> Meters
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

        public ObservableCollection<ContentTab> MenuTabs
        {
            get { return _menuTabs; }
        }

        public ObservableCollection<ContentTab> ViewTabs
        {
            get { return _viewTabs; }
        }

        #endregion

        #region Commands

        public ICommand ForwardPage
        {
            get
            {
                if (_forwardPage == null)
                    _forwardPage = new RelayCommand(p => Forward());

                return _forwardPage;
            }
        }

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

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainViewModel()
        {
            Meters = (Application.Current.Properties["meters"] as ObservableCollection<Meter>);
            Serial = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
            Tabs = new ObservableCollection<ContentTab>();
            Tabs.Add(new ContentTab(-1));
            _viewTabs = new ObservableCollection<ContentTab>();
            _viewTabs.Add(Tabs[0]);
            _menuTabs = new ObservableCollection<ContentTab>();
            TabIndex = 0;
            BackwardEnabled = false;
            
        }

        #endregion

        #region Methods

        private void Forward()
        {
            if (TabIndex == 0)
            {
                if (Tabs.Where(x => x.Visible == true).Count() > 1)
                {
                    TabIndex = 1;

                    var current = CurrentTab.CurrentPage;
                    if (current.Completed)
                        ForwardEnabled = true;
                    else
                        ForwardEnabled = false;
                }
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
            {
                Application.Current.Shutdown();
            }
            else if (TabIndex == idx)
            {
                TabIndex -= 1;
                if (p.Contains("(") && p.Contains(")"))
                {
                    var meters = (Application.Current.Properties["meters"] as ObservableCollection<Meter>);
                    meters[idx - 1].Save(string.Join("//", new string[] { Directory.GetCurrentDirectory(), "ToUpload" }));
                }
            }
            else
            {
                var save = TabIndex;
                TabIndex = 0;
                TabIndex = save;
            }
        }

        private void OpenTab(string p)
        {
            foreach(var tab in Tabs)
            {
                if (tab.MeterSerialNo == p)
                {
                    tab.Visible = true;
                    TabIndex = Tabs.IndexOf(tab);
                    return;
                }
            }

        }

        private async Task SaveMeters()
        {
            string dir = string.Join("//", new string[] { Directory.GetCurrentDirectory(), "ToUpload" });
            foreach (Meter m in (System.Windows.Application.Current.Properties["meters"] as ObservableCollection<Meter>))
                m.Save(dir);
        }

        #endregion
    }
}
