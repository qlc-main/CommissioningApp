using Hellang.MessageBus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    public class ReviewViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private Meter _meter;
        private IAsyncCommand _monitor;
        private ICommand _stop;
        private int _idx;
        private bool _completed;
        private bool _break;

        #endregion

        #region Properties

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

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                    _meter = (Application.Current.Properties["meters"] as List<Meter>)[Idx];

                return _meter.Channels;
            }
            set
            {
                _meter.Channels = value;
                OnPropertyChanged(nameof(Channels));
            }
        }

        public int Idx
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public string Notes
        {
            get { return _meter.Notes; }
            set
            {
                _meter.Notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public IAsyncCommand Monitor
        {
            get
            {
                if (_monitor == null)
                    _monitor = new AsyncRelayCommand(Watcher, () => { return true; });

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

        public ReviewViewModel()
        {
        }

        #endregion

        #region Methods

        #endregion

        #region Commands
        private async Task Watcher()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(2500);

                bool stop = true;
                foreach (Channel c in Channels)
                    if (((c.Phase1 == true || c.Phase2 == true) && (
                        string.IsNullOrEmpty(c.ApartmentNumber) ||
                        string.IsNullOrEmpty(c.BreakerNumber))) ||
                        (c.Phase1 == true && c.Forced[0] && string.IsNullOrEmpty(c.ForcedReason[0])) ||
                        (c.Phase2 == true && c.Forced[1] && string.IsNullOrEmpty(c.ForcedReason[1]))) {
                        stop = false;
                        break;
                    }

                Completed = stop;
                _meter.Commissioned = stop;
                if (_break)
                    break;
            }
        }

        private void Cease()
        {
            _break = true;
        }

        #endregion
    }
}
