
using Hellang.MessageBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfCommApp.Helpers;

namespace WpfCommApp
{
    public class CommissioningViewModel : ObservableObject, IPageViewModel
    {

        #region Fields

        private bool _break;
        private bool _channelComm;
        private bool[,] _allowCheck;
        private bool _completed;
        private DifferenceTracker _difference;
        private string _id;
        private string _idx;
        private string[] _oldPhase1Text;
        private string[] _oldPhase2Text;

        private ICommand _mp;
        private ICommand _pd;
        private ICommand _spd;

        private Meter _meter;
        private SerialComm _serial;

        #endregion

        #region Properties

        public int LedControlHeight { get; set; }

        public bool Completed
        {
            get { return _completed; }
            set
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

        public bool DisplayUpdated { get; set; }

        public Dictionary<string, int> Disposition { get; }

        public int FontSize { get; set; }

        public Meter Meter
        {
            get { return _meter; }
            set
            {
                _meter = value;
                OnPropertyChanged(nameof(Meter));
            }
        }

        public string Name { get { return "Commissioning"; } }

        public string[,] Phase1Text { get; }

        public string[,] Phase2Text { get; }

        public int Threshold { get; set; }

        public string VoltageA { get; set; }

        public string VoltageB { get; set; }

        public string VoltageC { get; set; }

        #endregion

        #region Commands

        public ICommand StartAsync
        {
            get
            {
                if (_pd == null)
                    _pd = new RelayCommand(p => Start());

                return _pd;
            }
        }

        public ICommand StopAsync
        {
            get
            {
                if (_spd == null)
                    _spd = new RelayCommand(p => Stop());

                return _spd;
            }
        }

        public ICommand ModifyPhase
        {
            get
            {
                if (_mp == null)
                    _mp = new RelayCommand(p => PhaseClicked((p as object[])[0] as string, (p as object[])[1] as Channel));

                return _mp;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Basic constructor takes in a string id to look up the meter attached to this instance and int
        /// to access the comm object that this instance should use.
        /// </summary>
        /// <param name="id">Index to retrieve Meter object</param>
        /// <param name="idx">Index to retrieve Serial object</param>
        public CommissioningViewModel(string id, string idx)
        {
            _id = id;
            _idx = idx;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            _allowCheck = new bool[Meter.Channels.Count, 2];
            _difference = new DifferenceTracker(Meter.Channels.Count);
            Phase1Text = new string[Meter.Channels.Count,5];
            Phase2Text = new string[Meter.Channels.Count,5];
            Disposition = (Application.Current.Properties["dispositions"] as Dictionary<string, int>);
            FontSize = 17;
            LedControlHeight = 22;
            Threshold = 10;
            DisplayUpdated = false;

            // _count = 0;
        }

        #endregion

        #region Methods

        #region Public 

        #endregion

        #region Private

        /// <summary>
        /// This function monitors the values retrieved from the Phase Diagnostic and evaluates
        /// if there was "enough" of a change to register a change.
        /// </summary>
        private void Detection()
        {
            float oldP, newP;
            bool twoMan = true;
            for (int i = 0; i < Phase1Text.GetLength(0); i++)
            {
                // Logic for if this is a two man commissioning team (can still be implemented for a one man team)
                if (twoMan)
                {
                    if (float.TryParse(_oldPhase1Text[i], out oldP))
                    {
                        newP = float.Parse(Phase1Text[i,0]);
                        if (newP - oldP > Threshold)
                            _allowCheck[i,0] = true;
                        else if (oldP - newP > Threshold)
                        {
                            if (Meter.Channels[i].Phase1 == false)
                            {
                                Meter.Channels[i].Phase1 = true;
                                Meter.Channels[i].Forced[0] = false;
                                Globals.Logger.LogInformation($"Autodetected load for {Meter.ID} on channel {i + 1}, phase 1.");
                            }

                            _allowCheck[i,0] = false;
                        }
                    }

                    if (float.TryParse(_oldPhase2Text[i], out oldP))
                    {
                        newP = float.Parse(Phase2Text[i,0]);
                        // var math = (newP - oldP) / ((oldP + newP) / 2);
                        if (newP - oldP > Threshold)
                            _allowCheck[i,1] = true;
                        else if (oldP - newP > Threshold)
                        {
                            if (Meter.Channels[i].Phase2 == false)
                            {
                                Meter.Channels[i].Phase2 = true;
                                Meter.Channels[i].Forced[1] = false;
                                Globals.Logger.LogInformation($"Autodetected load for {Meter.ID} on channel {i + 1}, phase 2.");
                            }

                            _allowCheck[i,1] = false;
                        }
                    }
                }

                // Toggle logic that will most likely be used for one man team
                else
                {
                    // If the old value can be parsed then continue with trying to compare the values
                    if (float.TryParse(_oldPhase1Text[i], out oldP))
                    {
                        newP = float.Parse(Phase1Text[i,0]);

                        // Perform function to determine whether or not the values differ enough to
                        // register a change
                        if (oldP - newP > Threshold)
                        {
                            _difference.IncChannelPhaseDiffRegister(i, 0);
                            _difference.ZeroChannelPhaseSameRegister(i, 0);

                            // If there are 3 changes registered and this phase has not been set
                            // then mark this phase as a suggested option and turn off the forced
                            // value for this phase
                            if (_difference.GetChannelPhaseDiffRegister(i, 0) > 2 && Meter.Channels[i].Phase1 == null &&
                                !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                            {
                                Meter.Channels[i].Phase1 = false;
                                Meter.Channels[i].Forced[0] = false;
                                _difference.ZeroChannelPhaseDiffRegister(i, 0);
                                Globals.Logger.LogInformation($"Autodetected load for {Meter.ID} on channel {i + 1}, phase 1.");
                            }
                        }
                        else
                        {
                            _difference.IncChannelPhaseSameRegister(i, 0);

                            // If there is not a significant enough of a change for 5 times in a row
                            // Zero out the difference and same registers
                            if (_difference.GetChannelPhaseSameRegister(i, 0) == 5)
                            {
                                _difference.ZeroChannelPhaseDiffRegister(i, 0);
                                _difference.ZeroChannelPhaseSameRegister(i, 0);
                                Globals.Logger.LogInformation($"Timeout limit reached for {Meter.ID} on channel {i + 1}, phase 1.");
                            }
                        }
                    }

                    if (float.TryParse(_oldPhase2Text[i], out oldP))
                    {
                        newP = float.Parse(Phase2Text[i,0]);
                        if (oldP - newP > Threshold)
                        {
                            _difference.IncChannelPhaseDiffRegister(i, 1);
                            _difference.ZeroChannelPhaseSameRegister(i, 1);

                            if (_difference.GetChannelPhaseDiffRegister(i, 1) > 2 && Meter.Channels[i].Phase2 == null &&
                                !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                            {
                                Meter.Channels[i].Phase2 = false;
                                Meter.Channels[i].Forced[1] = false;
                                _difference.ZeroChannelPhaseDiffRegister(i, 1);
                                Globals.Logger.LogInformation($"Autodetected load for {Meter.ID} on channel {i + 1}, phase 1.");
                            }
                        }
                        else
                        {
                            _difference.IncChannelPhaseSameRegister(i, 1);

                            if (_difference.GetChannelPhaseSameRegister(i, 1) == 5)
                            {
                                _difference.ZeroChannelPhaseDiffRegister(i, 1);
                                _difference.ZeroChannelPhaseSameRegister(i, 1);
                                Globals.Logger.LogInformation($"Timeout limit reached for {Meter.ID} on channel {i + 1}, phase 1.");
                            }
                        }
                    }
                }
            }

            foreach(var s in Enumerable.Range(0,Phase1Text.GetLength(0)))
            {
                _oldPhase1Text[s] = Phase1Text[s,0];
                _oldPhase2Text[s] = Phase2Text[s,0];
            }
        }

        /// <summary>
        /// Command used to initiate the asynchronous Phase Diagnostic request from meters
        /// </summary>
        private void Start()
        {
            Globals.Tasker.Run(PhaseDiagnostic);
            Scan();
        }

        /// <summary>
        /// Code used to determine if the user is allowed to change the state of control 
        /// that is connected to the phase of a particular channel
        /// </summary>
        /// <param name="phase">String representing particular phase</param>
        /// <param name="c">Channel associated with this control binding</param>
        private void PhaseClicked(string phase, Channel c)
        {
            // Prevents user from suggesting more than one channel at a time
            // User can suggest separate phase of channel if there is already another phase suggested
            bool contains = Meter.Channels.Any(channel => (channel.Phase1 == false || channel.Phase2 == false) && channel.ID != c.ID);

            if (phase == "Phase1" && _allowCheck[c.ID - 1, 0])
            {
                if (c.Phase1 == null && !contains)
                    c.Phase1 = false;
                else if (c.Phase1 == false)
                    c.Phase1 = null;
            }
            else if (phase == "Phase2" && _allowCheck[c.ID - 1, 1])
            {
                if (c.Phase2 == null && !contains)
                    c.Phase2 = false;
                else if (c.Phase2 == false)
                    c.Phase2 = null;
            }
        }

        /// <summary>
        /// Initializes this instance of the class in order to continuously call 
        /// Phase Diagnostic for Meter.
        /// </summary>
        /// <returns></returns>
        private async Task PhaseDiagnostic()
        {
            // Initialize variables if this is the first time 
            if (_serial == null)
            {
                _serial = (Application.Current.Properties["serial"] as Dictionary<string, SerialComm>)[_idx];

                int channel = 1;
                int length = Meter.Channels.Count;
                _oldPhase1Text = new string[length];
                _oldPhase2Text = new string[length];

                for (int i = 0; i < 5; i++)
                {
                    if (i == 4)
                    {
                        int cnt = 0;
                        for (int j = 0; j < length; j++)
                        {
                            int res = (cnt++ % 3);
                            Phase1Text[j,i] = res == 0 ? "A" : res == 1 ? "B" : "C";
                            res = (cnt++ % 3);
                            Phase2Text[j,i] = res == 0 ? "A" : res == 1 ? "B" : "C";
                            _allowCheck[j,0] = false;
                            _allowCheck[j,1] = false;
                            _oldPhase1Text[j] = string.Empty;
                            _oldPhase2Text[j] = string.Empty;
                        }
                    }
                    else if (i == 3)
                    {
                        // Displays the CT banks while commissioning
                        for (int j = 0; j < length; j++)
                        {
                            Phase1Text[j,i] = channel++.ToString();
                            Phase2Text[j,i] = channel++.ToString();
                        }
                    }
                    else
                    {
                        for (int j = 0; j < length; j++)
                        {
                            Phase1Text[j,i] = string.Empty;
                            Phase2Text[j,i] = string.Empty;
                        }
                    }
                }
            }

            // wait for the value to be reset
            while (_break) { }

            // Continuous loop to retrieve the phase diagnostic for a meter
            while (true)
            {
                // Requests the Phase Diagnostic from the meter, if successful
                // continue with discovering whether or not there was a large enough
                // change
                var retVal = Process();
                if (retVal.Contains("Amps"))
                {
                    UpdateMessage();
                    Detection();
                }
                // Switches the connected meter object to this serial comm
                else if (retVal == "switch")
                {
                    // Command to change the meter being actively evaluated and 
                    // associated with the serial port
                    (Application.Current.Properties["MessageBus"] as MessageBus)
                        .Publish(new MessageCenter("switchMeters"));

                    // Loop here until external code sets break state to avoid race condition
                    while (!_break) { }
                }
                // The Process function returns false when user closed the Attempting to reconnect
                // popup window or if the meter was switched so this function forces
                // the user to go back a page and cancels the phase diagnostic reads
                else if (string.IsNullOrEmpty(retVal))
                {
                    (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new MessageCenter("backward"));

                    // Loop here until external code sets break state to avoid race condition
                    while (!_break) { }
                }

                if (_break)
                    break;
            }

            // reset break value so we can continuously execute this function 
            // if we need to revisit this page
            _break = false;
        }

        /// <summary>
        /// Issues Phase Diagnostic call for meter 
        /// </summary>
        /// <returns>Boolean indicating success of Phase Diagnostic call</returns>
        private string Process()
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var task = _serial.PhaseDiagnostic(token);

            // Waits X seconds for Phase Diagnostic call to finish execution if not then display
            // modal to user and attempt to reconnect
            int timeout = _serial.NewFirmware ? 7000 : 5000;
            if (!task.Wait(timeout, token))
            {
                // Create thread that launches modal window for user and continues to poll
                // original process to determine if it has completed
                Thread t = new Thread(new ThreadStart(async () =>
                {
                    InfoView info = null;
                    InfoViewModel ifvm = new InfoViewModel(task, token, tokenSource, "Serial Connection Lost", "Attempting to Reconnect");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        info = new InfoView
                        {
                            Owner = Application.Current.MainWindow,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            DataContext = ifvm
                        };

                        info.Show();
                    });

                    var monitor = await ifvm.Poll();
                    if (monitor)
                    {
                        // Close the window if the program has successfully re-established communication
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ifvm.UserClosedWindow = false;
                            info.Close();
                        });
                    }
                    else
                    {
                        // Wait for the task to complete
                        while (task.Status != TaskStatus.RanToCompletion) { }

                        // exit this function
                        return;
                    }
                }));

                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                t.Join();
            }

            // Iterate over each line and extract the voltage, current and kW for each channel and phase of the meter
            string buffer = task.Result;
            int meter = 0;
            bool phase1 = true;
            foreach (string s in buffer.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string[] cols = s.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                // if not data row continue to next line
                if (s.Contains("Amps") || (!_serial.NewFirmware && cols.Length != 10) || (_serial.NewFirmware && cols.Length != 9))
                    continue;

                if (!_serial.NewFirmware)
                {
                    meter = Convert.ToInt32(cols[0]) - 32;
                    phase1 = cols[3] == "1" ? true : false;
                    if (phase1)
                    {
                        Phase1Text[meter, 0] = float.Parse(cols[6]).ToString("0.00");
                        float watts = float.Parse(cols[8]);
                        Phase1Text[meter, 1] = (watts / 1000).ToString("0.00");
                        double temp = Math.Atan2(float.Parse(cols[9]), watts);

                        // Set the voltages for the Meter Information box
                        if (Phase1Text[meter, 4] == "A")
                            VoltageA = cols[7] + " V";
                        else if (Phase1Text[meter, 4] == "B")
                            VoltageB = cols[7] + " V";
                        else if (Phase1Text[meter, 4] == "C")
                            VoltageC = cols[7] + " V";

                        if (double.IsNaN(temp))
                            Phase1Text[meter, 2] = "--";
                        else
                            Phase1Text[meter, 2] = temp.ToString("0.00");
                    }
                    else
                    {
                        Phase2Text[meter, 0] = float.Parse(cols[6]).ToString("0.00");
                        float watts = float.Parse(cols[8]);
                        Phase2Text[meter, 1] = (watts / 1000).ToString("0.00");
                        double temp = Math.Atan2(float.Parse(cols[9]), watts);

                        // Set the voltages for the Meter Information box
                        if (Phase2Text[meter, 3] == "A")
                            VoltageA = cols[7] + " V";
                        else if (Phase2Text[meter, 3] == "B")
                            VoltageB = cols[7] + " V";
                        else if (Phase2Text[meter, 3] == "C")
                            VoltageC = cols[7] + " V";

                        if (double.IsNaN(temp))
                            Phase2Text[meter, 2] = "--";
                        else
                            Phase2Text[meter, 2] = temp.ToString("0.00");
                    }
                }
                else
                {
                    phase1 = cols[0] == "1" ? true : false;
                    if (phase1)
                    {
                        Phase1Text[meter, 0] = float.Parse(cols[3]).ToString("0.00");
                        float watts = float.Parse(cols[5]);
                        Phase1Text[meter, 1] = (watts / 1000).ToString("0.00");
                        double temp = Math.Atan2(float.Parse(cols[6]), watts);

                        // Set the voltages for the Meter Information box
                        if (Phase1Text[meter, 4] == "A")
                            VoltageA = cols[4] + " V";
                        else if (Phase1Text[meter, 4] == "B")
                            VoltageB = cols[4] + " V";
                        else if (Phase1Text[meter, 4] == "C")
                            VoltageC = cols[4] + " V";

                        if (double.IsNaN(temp))
                            Phase1Text[meter, 2] = "--";
                        else
                            Phase1Text[meter, 2] = temp.ToString("0.00");
                    }
                    else
                    {
                        Phase2Text[meter, 0] = float.Parse(cols[3]).ToString("0.00");
                        float watts = float.Parse(cols[5]);
                        Phase2Text[meter, 1] = (watts / 1000).ToString("0.00");
                        double temp = Math.Atan2(float.Parse(cols[6]), watts);

                        // Set the voltages for the Meter Information box
                        if (Phase2Text[meter, 3] == "A")
                            VoltageA = cols[4] + " V";
                        else if (Phase2Text[meter, 3] == "B")
                            VoltageB = cols[4] + " V";
                        else if (Phase2Text[meter, 3] == "C")
                            VoltageC = cols[4] + " V";

                        if (double.IsNaN(temp))
                            Phase2Text[meter, 2] = "--";
                        else
                            Phase2Text[meter, 2] = temp.ToString("0.00");

                        meter++;
                    }
                }
            }

            OnPropertyChanged(nameof(Phase1Text));
            OnPropertyChanged(nameof(Phase2Text));

            return buffer;
        }

        /// <summary>
        /// Sets the Completed variable for the page, only set completed if at least one 
        /// phase of one channel has been commissioned
        /// </summary>
        private async Task Scan()
        {
            var previousMsg = string.Empty;
            while (true)
            {
                // Check if at least one phase of a channel has been commissioned
                _channelComm = false;
                var phaseCommissioned = false;
                var msg = string.Empty;
                foreach (Channel c in Meter.Channels)
                {
                    if ((c.Phase1 == true || c.Phase2 == true) && !string.IsNullOrEmpty(c.ApartmentNumber)
                        && !string.IsNullOrEmpty(c.BreakerNumber))
                    {
                        _channelComm = true;
                        break;
                    }
                    else if ((c.Phase1 == true || c.Phase2 == true) && string.IsNullOrEmpty(msg))
                    {
                        phaseCommissioned = true;
                        // Develop log messaging to help with troubleshooting
                        if (string.IsNullOrEmpty(c.ApartmentNumber))
                            msg = $"Channel {c.ID} requires Apartment data for {Meter.ID}";
                        else if (string.IsNullOrEmpty(c.BreakerNumber))
                            msg = $"Channel {c.ID} requires Breaker number for {Meter.ID}";
                    }
                }

                // Create log message if there are no channels that have been commissioned
                if (!_channelComm && !phaseCommissioned)
                    msg = $"Requires at least one phase of one channel to be commissioned for {Meter.ID}";

                // if at least one phase and the meter details have been entered mark as complete
                if (_channelComm && Meter.Disposition > 0 && !string.IsNullOrEmpty(Meter.Floor) && !string.IsNullOrEmpty(Meter.Location) && !string.IsNullOrEmpty(Meter.OperationID)
                    && Meter.NoteRequired == "Hidden" && !string.IsNullOrEmpty(Meter.OperationID))
                    Completed = true;
                else
                    Completed = false;

                // Create log message if the page has not been completed
                // and the log message is still empty
                if (!Completed && string.IsNullOrEmpty(msg))
                {
                    if (string.IsNullOrEmpty(Meter.Floor))
                        msg = $"Missing floor for {Meter.ID}";
                    else if (string.IsNullOrEmpty(Meter.Location))
                        msg = $"Missing location for {Meter.ID}";
                    else if (string.IsNullOrEmpty(Meter.OperationID))
                        msg = $"Missing operation ID for {Meter.ID}";
                    else if (Meter.Disposition < 0)
                        msg = $"Missing Meter disposition for {Meter.ID}";
                    else if (Meter.NoteRequired != "Hidden")
                        msg = $"Missing mandatory note due to disposition for {Meter.ID}";
                }
                else if (Completed)
                    msg = $"Next page allowed for {Meter.ID}";

                // Write log message if different from previous message
                // and message is not empty
                if (previousMsg != msg && !string.IsNullOrEmpty(msg))
                {
                    Globals.Logger.LogInformation(msg);
                    previousMsg = msg;
                }

                if (_break)
                    break;

                await Task.Delay(1500);
            }
        }

        /// <summary>
        /// Stops the Phase Diagnostic call loop
        /// </summary>
        private void Stop()
        {
            _break = true;
        }

        private async Task UpdateMessage()
        {
            DisplayUpdated = true;
            await Task.Delay(2500);
            DisplayUpdated = false;
        }

        #endregion

        #endregion
    }
}
