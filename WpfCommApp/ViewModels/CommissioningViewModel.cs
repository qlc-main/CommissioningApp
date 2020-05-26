
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using Hellang.MessageBus;
using System.Linq;

namespace WpfCommApp
{
    public class CommissioningViewModel : ObservableObject, IPageViewModel
    {

        #region Fields

        private bool _break;
        private bool _channelComm;
        private bool[][] _checker;
        private bool _completed;
        private int[][][] _diff;
        private string _id;
        private string _idx;
        private string[] _oldPhaseAText;
        private string[] _oldPhaseBText;

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

        public string[][] PhaseAText { get; }

        public string[][] PhaseBText { get; }

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
            PhaseAText = new string[4][];
            PhaseBText = new string[4][];
            _checker = new bool[2][];
            _diff = new int[2][][];
            _id = id;
            _idx = idx;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            Disposition = (Application.Current.Properties["dispositions"] as Dictionary<string, int>);
            FontSize = 17;
            LedControlHeight = 22;

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
            for (int i = 0; i < PhaseAText[0].Length; i++)
            {
                // Logic for if this is a two man commissioning team (can still be implemented for a one man team)
                if (twoMan)
                {
                    if (float.TryParse(_oldPhaseAText[i], out oldP))
                    {
                        newP = float.Parse(PhaseAText[0][i]);
                        var math = (newP - oldP) / ((oldP + newP) / 2);
                        if (math > 1)
                            _checker[0][i] = true;
                        else if (math < -1)
                        {
                            if (Meter.Channels[i].Phase1 == false && newP == 0)
                            {
                                Meter.Channels[i].Phase1 = true;
                                Meter.Channels[i].Forced[0] = false;
                            }

                            _checker[0][i] = false;
                        }
                    }

                    if (float.TryParse(_oldPhaseBText[i], out oldP))
                    {
                        newP = float.Parse(PhaseBText[0][i]);
                        var math = (newP - oldP) / ((oldP + newP) / 2);
                        if (math > 1)
                            _checker[1][i] = true;
                        else if (math < -1)
                        {
                            if (Meter.Channels[i].Phase2 == false && newP == 0)
                            {
                                Meter.Channels[i].Phase2 = true;
                                Meter.Channels[i].Forced[1] = false;
                            }

                            _checker[1][i] = false;
                        }
                    }
                }

                // Toggle logic that will most likely be used for one man team
                else
                {
                    // If the old value can be parsed then continue with trying to compare the values
                    if (float.TryParse(_oldPhaseAText[i], out oldP))
                    {
                        newP = float.Parse(PhaseAText[0][i]);

                        // Perform function to determine whether or not the values differ enough to
                        // register a change
                        if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1)
                        {
                            _diff[0][i][0]++;
                            _diff[0][i][1] = 0;

                            // If there are 3 changes registered and this phase has not been set
                            // then mark this phase as a suggested option and turn off the forced
                            // value for this phase
                            if (_diff[0][i][0] > 2 && Meter.Channels[i].Phase1 == null &&
                                !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                            {
                                Meter.Channels[i].Phase1 = false;
                                Meter.Channels[i].Forced[0] = false;
                                _diff[0][i][0] = 0;
                            }
                        }
                        else
                        {
                            _diff[0][i][1]++;

                            // If there is not a significant enough of a change for 5 times in a row
                            // Zero out the difference and same registers
                            if (_diff[0][i][1] == 5)
                            {
                                _diff[0][i][0] = 0;
                                _diff[0][i][1] = 0;
                            }
                        }
                    }

                    if (float.TryParse(_oldPhaseBText[i], out oldP))
                    {
                        newP = float.Parse(PhaseBText[0][i]);
                        if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1)
                        {
                            _diff[1][i][0]++;
                            _diff[1][i][1] = 0;

                            if (_diff[1][i][0] > 2 && Meter.Channels[i].Phase2 == null &&
                                !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                            {
                                Meter.Channels[i].Phase2 = false;
                                Meter.Channels[i].Forced[1] = false;
                                _diff[1][i][0] = 0;
                            }
                        }
                        else
                        {
                            _diff[1][i][1]++;

                            if (_diff[1][i][1] == 5)
                            {
                                _diff[1][i][0] = 0;
                                _diff[1][i][1] = 0;
                            }
                        }
                    }
                }
            }

            _oldPhaseAText = PhaseAText[0].Clone() as string[];
            _oldPhaseBText = PhaseBText[0].Clone() as string[];
        }

        /// <summary>
        /// Command used to initiate the asynchronous Phase Diagnostic request from meters
        /// </summary>
        private void Start()
        {
                Task.Run(PhaseDiagnostic);
                Task.Run(Scan);
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

            if (phase == "Phase1" && _checker[0][c.ID - 1])
            {
                if (c.Phase1 == null && !contains)
                    c.Phase1 = false;
                else if (c.Phase1 == false)
                    c.Phase1 = null;
            }
            else if (phase == "Phase2" && _checker[1][c.ID - 1])
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

                int length = Meter.Channels.Count;
                _diff[0] = new int[length][];
                _diff[1] = new int[length][];
                _oldPhaseAText = new string[length];
                _oldPhaseBText = new string[length];
                _checker[0] = new bool[length];
                _checker[1] = new bool[length];

                for (int i = 0; i < 4; i++)
                {
                    PhaseAText[i] = new string[length];
                    PhaseBText[i] = new string[length];

                    if (i == 3)
                    {
                        int cnt = 0;
                        for (int j = 0; j < length; j++)
                        {
                            int res = (cnt++ % 3);
                            PhaseAText[i][j] = res == 0 ? "A" : res == 1 ? "B" : "C";
                            res = (cnt++ % 3);
                            PhaseBText[i][j] = res == 0 ? "A" : res == 1 ? "B" : "C";
                            _checker[0][j] = false;
                            _checker[1][j] = false;
                            _diff[0][j] = new int[2] { 0, 0 };
                            _diff[1][j] = new int[2] { 0, 0 };
                            _oldPhaseAText[j] = string.Empty;
                            _oldPhaseBText[j] = string.Empty;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < length; j++)
                        {
                            PhaseAText[i][j] = string.Empty;
                            PhaseBText[i][j] = string.Empty;
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
                    Detection();
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
            var task = Task.Factory.StartNew(() => _serial.PhaseDiagnostic(token), token);

            // Waits 5 seconds for Phase Diagnostic call to finish execution if not then display
            // modal to user and attempt to reconnect
            int timeout = _serial.NewFirmware ? 7000 : 5000;
            if (!task.Wait(timeout, token))
            {
                // Create thread that launches modal window for user and continues to poll
                // original process to determine if it has completed
                Thread t = new Thread(new ThreadStart(() =>
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

                    var monitor = Task.Run(ifvm.Poll);
                    monitor.Wait();

                    if (monitor.Result)
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
            bool phaseA = true;
            foreach (string s in buffer.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string[] cols = s.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                // if not data row continue to next line
                if (s.Contains("Amps") || (!_serial.NewFirmware && cols.Length != 10) || (_serial.NewFirmware && cols.Length != 9))
                    continue;

                if (!_serial.NewFirmware)
                {
                    meter = Convert.ToInt32(cols[0]) - 32;
                    phaseA = cols[3] == "1" ? true : false;
                    if (phaseA)
                    {
                        PhaseAText[0][meter] = float.Parse(cols[6]).ToString("0.00");
                        float watts = float.Parse(cols[8]);
                        PhaseAText[1][meter] = (watts / 1000).ToString("0.00");
                        double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[9]), 2));
                        if (double.IsNaN(temp))
                            PhaseAText[2][meter] = "--";
                        else
                            PhaseAText[2][meter] = temp.ToString("0.00");
                    }
                    else
                    {
                        PhaseBText[0][meter] = float.Parse(cols[6]).ToString("0.00");
                        float watts = float.Parse(cols[8]);
                        PhaseBText[1][meter] = (watts / 1000).ToString("0.00");
                        double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[9]), 2));
                        if (double.IsNaN(temp))
                            PhaseBText[2][meter] = "--";
                        else
                            PhaseBText[2][meter] = temp.ToString("0.00");
                    }
                }
                else
                {
                    phaseA = cols[0] == "1" ? true : false;
                    if (phaseA)
                    {
                        PhaseAText[0][meter] = float.Parse(cols[3]).ToString("0.00");
                        float watts = float.Parse(cols[5]);
                        PhaseAText[1][meter] = (watts / 1000).ToString("0.00");
                        double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[6]), 2));
                        if (double.IsNaN(temp))
                            PhaseAText[2][meter] = "--";
                        else
                            PhaseAText[2][meter] = temp.ToString("0.00");
                    }
                    else
                    {
                        PhaseBText[0][meter] = float.Parse(cols[3]).ToString("0.00");
                        float watts = float.Parse(cols[5]);
                        PhaseBText[1][meter] = (watts / 1000).ToString("0.00");
                        double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[6]), 2));
                        if (double.IsNaN(temp))
                            PhaseBText[2][meter] = "--";
                        else
                            PhaseBText[2][meter] = temp.ToString("0.00");

                        meter++;
                    }
                }
            }

            OnPropertyChanged(nameof(PhaseAText));
            OnPropertyChanged(nameof(PhaseBText));

            return buffer;
        }

        /// <summary>
        /// Sets the Completed variable for the page, only set completed if at least one 
        /// phase of one channel has been commissioned
        /// </summary>
        private async Task Scan()
        {
            while (true)
            {
                // Check if at least one phase of a channel has been commissioned
                _channelComm = false;
                foreach (Channel c in Meter.Channels)
                {
                    if ((c.Phase1 == true || c.Phase2 == true) && !string.IsNullOrEmpty(c.ApartmentNumber)
                        && !string.IsNullOrEmpty(c.BreakerNumber))
                    {
                        _channelComm = true;
                        break;
                    }
                }

                // if at least one phase and the meter details have been entered mark as complete
                if (_channelComm && Meter.Disposition > 0 && !string.IsNullOrEmpty(Meter.Floor) && !string.IsNullOrEmpty(Meter.Location) && !string.IsNullOrEmpty(Meter.OperationID))
                    Completed = true;
                else
                    Completed = false;

                if (_break)
                    break;

                Thread.Sleep(1500);
            }
        }

        /// <summary>
        /// Stops the Phase Diagnostic call loop
        /// </summary>
        private void Stop()
        {
            _break = true;
        }

        #endregion

        #endregion
    }
}
