using Hellang.MessageBus;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfCommApp.Helpers;

namespace WpfCommApp
{
    public class ReviewViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private bool _break;
        private bool _completed;
        private Dictionary<int, string> _dispositions;
        private string _id;

        private ICommand _start;
        private ICommand _stop;

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
                            .Publish(new MessageCenter());

                    // Disable the forward button if at least one necessary detail is unavailable
                    else
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new MessageCenter("disableFwd"));
                }
            }
        }

        public string[] CTTypes { get; }

        public string Disposition
        {
            get { return _dispositions[Meter.Disposition]; }
        }

        public int FontSize { get; set; }

        public string FSReturn
        {
            get { return Meter.FSReturn ? "No" : "Yes"; }
        }

        public int LedControlHeight { get; set; }

        public Meter Meter { get; private set; }

        public string Name { get { return "Review"; } }

        public string OprComplete
        {
            get { return Meter.OprComplete ? "Yes" : "No"; }
        }

        #endregion

        #region Commands

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

        #region Constructors

        public ReviewViewModel(string id)
        {
            _id = id;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            _completed = false;
            CTTypes = (Application.Current.Properties["cttypes"] as string[]);
            _dispositions = (Application.Current.Properties["dispositions"] as Dictionary<string, int>).ToDictionary(x => x.Value, x => x.Key);
            FontSize = 16;
            LedControlHeight = 22;
        }

        #endregion

        #region Methods

        #region Public 

        #endregion

        #region Private

        private void Start()
        {
            Globals.Tasker.Run(Watcher);
        }

        private void Stop()
        {
            _break = true;
        }

        private async Task Watcher()
        {
            // loop here until break value is reset before entering loop
            while (_break) { }

            var previousMsg = string.Empty;
            while (true)
            {
                bool commissioned = true;
                string msg = string.Empty;
                foreach (Channel c in Meter.Channels)
                    if ((c.Phase1 == true && c.Forced[0] && string.IsNullOrEmpty(c.Reason[0])) ||
                        (c.Phase2 == true && c.Forced[1] && string.IsNullOrEmpty(c.Reason[1])))
                    {
                        // Logging message for why forward progress is disabled
                        msg = $"Meter {Meter.ID} Channel {c.ID} requires ";
                        if (c.Forced[0] && string.IsNullOrEmpty(c.Reason[0]))
                            msg += "reason for phase1";
                        else if (c.Forced[0] && string.IsNullOrEmpty(c.Reason[0]))
                            msg += "reason for phase2";

                        commissioned = false;
                        break;
                    }

                // Logging message to confirm availability of forward progress
                if (commissioned)
                    msg = $"Next page allowed for {Meter.ID}";

                // Log message if different and message is not empty
                if (previousMsg != msg && !string.IsNullOrEmpty(msg))
                {
                    Globals.Logger.LogInformation(msg);
                    previousMsg = msg;
                }

                Completed = commissioned;
                Meter.Commissioned = commissioned;
                if (_break)
                    break;
            }

            _break = false;
        }

        #endregion

        #endregion
    }
}
