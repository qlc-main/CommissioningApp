using Hellang.MessageBus;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using WpfCommApp.Managers;
using WpfCommApp.Helpers;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class ConfigurationViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private bool _allFunctionsStopped;
        private bool _break;
        private int _channelSize;
        private string _comboBoxText;
        private bool _completed;
        private string _id;
        private Task _task;

        private ICommand _notCommissioned;
        private ICommand _setPrimary;
        private ICommand _start;
        private IAsyncCommand _stop;

        private Meter _meter;

        #endregion

        #region Properties

        public bool AllFunctionsStopped
        {
            get { return _allFunctionsStopped; }
            set
            {
                if (_allFunctionsStopped != value)
                {
                    _allFunctionsStopped = value;
                    OnPropertyChanged(nameof(AllFunctionsStopped));
                }
            }
        }

        public int ChannelSize
        {
            get { return _channelSize; }
            set
            {
                if (_channelSize != value)
                {
                    _channelSize = value;
                    OnPropertyChanged(nameof(ChannelSize));
                }
            }
        }

        public string ComboBoxText
        {
            get { return _comboBoxText; }
            set
            {
                if (_comboBoxText != value)
                {
                    _comboBoxText = value;
                    OnPropertyChanged(nameof(ComboBoxText));
                }
            }
        }

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

        public int FontSize { get; set; }

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

        public ICommand SetPrimary
        {
            get
            {
                if (_setPrimary == null)
                    _setPrimary = new RelayCommand(p => PrimaryConfiguration());

                return _setPrimary;
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
                    _stop = new AsyncRelayCommand(Stop);

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
            _comboBoxText = "100";
            Meter = Globals.Meters[_id];
            ChannelSize = Meter.Size;
            CTTypes = (Application.Current.Properties["cttypes"] as string[]);
            FontSize = 19;
        }

        #endregion

        #region Methods

        #region Public

        public async void Dispose()
        {
        }

        #endregion

        #region Private

        private void ChannelChanged()
        {
            int total = 0;
            foreach (Channel c in Meter.Channels)
                if (c.CTType != "" || c.Primary != "" || c.Secondary != "")
                    total++;
            ChannelSize = total;
        }

        private void PrimaryConfiguration()
        {
            foreach(Channel c in Meter.Channels)
            {
                if (c.CTType != "" || c.Secondary != "")
                    c.Primary = ComboBoxText;
            }

            Globals.Logger.LogInformation($"Set the primary for all channels to {ComboBoxText}");
        }

        private void Start()
        {
            _task = Globals.Tasker.Run(Watcher);
            AllFunctionsStopped = false;
        }

        private async Task Stop()
        {
            _break = true;
            while (!_task.IsCompleted)
                await Task.Delay(500);
            AllFunctionsStopped = true;
        }

        /// <summary>
        /// Remove the Primary, Secondary, and CTType which will decommission the channel
        /// and not allow it to be commissioned on the next page
        /// </summary>
        /// <param name="id"></param>
        private void Uncommission(int id)
        {
            // Marks specified channel as not available for commissioning
            _meter.Channels[id - 1].Primary = "";
            _meter.Channels[id - 1].Secondary = "";
            _meter.Channels[id - 1].CTType = "";
            Globals.Logger.LogInformation($"Marked channel {id} as uncommissioned");

            // Modifies the ChannelSize property given the number of uncommissioned channels
            ChannelChanged();
            OnPropertyChanged(nameof(Meter));
            Globals.Logger.LogInformation($"Meter {_meter.ID} now has {ChannelSize} channels for commissioning.");
        }

        private void Watcher()
        {
            // Loop while true so that function doesn't exit prematurely
            while(_break) { }

            bool complete;
            string previousMsg = string.Empty;
            while(true)
            {
                complete = true;
                ChannelChanged();
                foreach(Channel c in Meter.Channels)
                {
                    if ((!string.IsNullOrEmpty(c.CTType) && (string.IsNullOrEmpty(c.Primary) || string.IsNullOrEmpty(c.Secondary))) ||
                        (string.IsNullOrEmpty(c.CTType) && (!string.IsNullOrEmpty(c.Primary) || !string.IsNullOrEmpty(c.Secondary))))
                    {
                        // Construct log message
                        string msg = $"Meter {Meter.ID} Channel {c.ID} requires ";
                        if (!string.IsNullOrEmpty(c.CTType))
                        {
                            if (string.IsNullOrEmpty(c.Primary))
                                msg += "Primary";
                            else if (string.IsNullOrEmpty(c.Secondary))
                                msg += "Secondary";
                            else
                                msg = string.Empty;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(c.Primary))
                                msg += "removal of Primary or population of CT Type";
                            else if (!string.IsNullOrEmpty(c.Secondary))
                                msg += "removal of Secondary or population of CT Type";
                            else
                                msg = string.Empty;
                        }

                        // Log message only if different from previous message
                        if (previousMsg != msg && !string.IsNullOrEmpty(msg))
                        {
                            Globals.Logger.LogInformation(msg);
                            previousMsg = msg;
                        }

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
