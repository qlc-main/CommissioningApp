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
    public class ConfigurationViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private bool _break;
        private bool _completed;
        private string _id;

        private ICommand _notCommissioned;
        private ICommand _start;
        private ICommand _stop;

        private Meter _meter;

        #endregion

        #region Properties

        public bool Completed {
            get { return _completed; }
            private set
            {
                if (_completed != value)
                {
                    _completed = value;

                    // Enable the forward button if at least one channel is commissioned
                    if (value)
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new MessageCenter());

                    // Disable the forward button if no channels are commissioned
                    else
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new MessageCenter("disableFwd"));
                }
            }
        }

        public string[] CTTypes { get; }

        public Meter Meter
        {
            get { return _meter; }

            private set
            {
                _meter = value;
                OnPropertyChanged(nameof(Meter));
            }
        }

        public string Name { get { return "Configuration"; } }

        #endregion

        #region Commands

        public ICommand NotCommissioned
        {
            get
            {
                if (_notCommissioned == null)
                    _notCommissioned = new RelayCommand(p => Uncommission((int) p));

                return _notCommissioned;
            }
        }

        public ICommand StartAsync
        {
            get
            {
                if (_start == null)
                    _start = new RelayCommand(p => Start());

                return _start;
            }
        }

        public ICommand StopAsync
        {
            get
            {
                if (_stop == null)
                    _stop = new RelayCommand(p => Stop());

                return _stop;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor that initiates the variables necessary for Configuration Page
        /// </summary>
        /// <param name="id"></param>
        public ConfigurationViewModel(string id)
        {
            _id = id;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            CTTypes = (Application.Current.Properties["cttypes"] as string[]);
        }

        #endregion

        #region Methods

        #region Public

        #endregion

        #region Private

        private void Start()
        {
            Task.Run(Watcher);
        }

        private void Stop()
        {
            _break = true;
        }

        /// <summary>
        /// Remove the Primary, Secondary, and CTType which will decommission the channel
        /// and not allow it to be commissioned on the next page
        /// </summary>
        /// <param name="id"></param>
        private void Uncommission(int id)
        {
            _meter.Channels[id - 1].Primary = "";
            _meter.Channels[id - 1].Secondary = "";
            _meter.Channels[id - 1].CTType = "";
            OnPropertyChanged(nameof(Meter));
        }

        private async Task Watcher()
        {
            // Loop while true so that function doesn't exit prematurely
            while(_break) { }

            bool complete;
            while(true)
            {
                complete = true;
                foreach(Channel c in Meter.Channels)
                {
                    if (!string.IsNullOrEmpty(c.CTType) && (string.IsNullOrEmpty(c.Primary) || string.IsNullOrEmpty(c.Secondary)))
                    {
                        complete = false;
                        break;
                    }
                }

                Completed = complete;

                if (_break)
                    break;
            }

            _break = false;
        }

        #endregion

        #endregion
    }
}
