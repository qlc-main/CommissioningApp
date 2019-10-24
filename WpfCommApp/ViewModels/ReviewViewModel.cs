using Hellang.MessageBus;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    public class ReviewViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private bool _break;
        private bool _completed;
        private string[] _ctTypes;
        private int _idx;

        private Meter _meter;

        private ICommand _monitor;
        private ICommand _stop;

        #endregion

        #region Properties

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                    _meter = (Application.Current.Properties["meters"] as ObservableCollection<Meter>)[IDX];

                return _meter.Channels;
            }
            set
            {
                _meter.Channels = value;
                OnPropertyChanged(nameof(Channels));
            }
        }

        public bool Completed
        {
            get { return _completed; }

            private set
            {
                if (_completed != value)
                {
                    _completed = value;

                    // Enable the forward button if necessary details completely filled in
                    if (value)
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete());

                    // Disable the forward button if at least one necessary detail is unavailable
                    else
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete("disable"));
                }
            }
        }

        public string[] CTTypes
        {
            get { return _ctTypes; }
        }

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public string Name { get { return "Review"; } }

        public string Notes
        {
            get { return _meter.Notes; }
            set
            {
                _meter.Notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public ICommand Monitor
        {
            get
            {
                if (_monitor == null)
                    _monitor = new RelayCommand(p => LaunchWatcher());

                return _monitor;
            }
        }

        public ICommand Stop
        {
            get
            {
                if (_stop == null)
                    _stop = new RelayCommand(p => Cease());

                return _stop;
            }
        }
        
        #endregion

        #region Constructors

        public ReviewViewModel(int idx)
        {
            _idx = idx;
            _ctTypes = new string[3] { "flex", "solid", "split" };
        }

        #endregion

        #region Methods

        #endregion

        #region Commands

        private void LaunchWatcher()
        {
            Task.Run(Watcher);
        }

        private async Task Watcher()
        {
            // loop here until break value is reset before entering loop
            while (_break) { }

            while (true)
            {
                System.Threading.Thread.Sleep(2500);

                bool stop = true;
                foreach (Channel c in Channels)
                    if (((c.Phase1 == true || c.Phase2 == true) && (
                        string.IsNullOrEmpty(c.ApartmentNumber) ||
                        string.IsNullOrEmpty(c.BreakerNumber))) ||
                        (c.Phase1 == true && c.Forced[0] && string.IsNullOrEmpty(c.Reason[0])) ||
                        (c.Phase2 == true && c.Forced[1] && string.IsNullOrEmpty(c.Reason[1]))) {
                        stop = false;
                        break;
                    }

                Completed = stop;
                _meter.Commissioned = stop;
                if (_break)
                    break;
            }

            _break = false;
        }

        private void Cease()
        {
            _break = true;
        }

        #endregion
    }
}
