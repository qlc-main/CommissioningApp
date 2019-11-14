
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
        private bool _completed;
        private string _id;
        private string _idx;
        private string[] _oldPhaseAText;
        private string[] _oldPhaseBText;

        private ICommand _pd;
        private ICommand _spd;

        private Dictionary<string, int>[][] _diff;
        private Meter _meter;
        private SerialComm _serial;

        #endregion

        #region Properties

        public Dictionary<string, int> Disposition { get; }

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

        public string IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

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
                    _pd = new RelayCommand(p => GetPhaseDiagnostic());

                return _pd;
            }
        }

        public ICommand StopAsync
        {
            get
            {
                if (_spd == null)
                    _spd = new RelayCommand(p => StopPhaseDiagnostic());

                return _spd;
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
            _diff = new Dictionary<string, int>[2][];
            _id = id;
            _idx = idx;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            Disposition = (Application.Current.Properties["dispositions"] as Dictionary<string, int>);
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
            for (int i = 0; i < PhaseAText[0].Length; i++)
            {
                // If the old value can be parsed then continue with trying to compare the values
                if (float.TryParse(_oldPhaseAText[i], out oldP))
                {
                    newP = float.Parse(PhaseAText[0][i]);

                    // Perform function to determine whether or not the values differ enough to
                    // register a change
                    if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1)
                    {
                        _diff[0][i]["diff"]++;
                        _diff[0][i]["same"] = 0;

                        // If there are 3 changes registered and this phase has not been set
                        // then mark this phase as a suggested option and turn off the forced
                        // value for this phase
                        if (_diff[0][i]["diff"] > 2 && Meter.Channels[i].Phase1 == null && 
                            !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                        {
                            Meter.Channels[i].Phase1 = false;
                            Meter.Channels[i].Forced[0] = false;
                            _diff[0][i]["diff"] = 0;
                        }
                    }
                    else
                    {
                        _diff[0][i]["same"]++;

                        // If there is not a significant enough of a change for 5 times in a row
                        // Zero out the difference and same registers
                        if (_diff[0][i]["same"] == 5)
                        {
                            _diff[0][i]["diff"] = 0;
                            _diff[0][i]["same"] = 0;
                        }
                    }
                }

                if (float.TryParse(_oldPhaseBText[i], out oldP))
                {
                    newP = float.Parse(PhaseBText[0][i]);
                    if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1)
                    {
                        _diff[1][i]["diff"]++;
                        _diff[1][i]["same"] = 0;

                        if (_diff[1][i]["diff"] > 2 && Meter.Channels[i].Phase2 == null &&
                            !Meter.Channels.Any(x => x.Phase1 == false || x.Phase2 == false))
                        {
                            Meter.Channels[i].Phase2 = false;
                            Meter.Channels[i].Forced[1] = false;
                            _diff[1][i]["diff"] = 0;
                        }
                    }
                    else
                    {
                        _diff[1][i]["same"]++;

                        if (_diff[1][i]["same"] == 5)
                        {
                            _diff[1][i]["diff"] = 0;
                            _diff[1][i]["same"] = 0;
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
        private void GetPhaseDiagnostic()
        {
            Task.Run(PhaseDiagnostic);
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
                _diff[0] = new Dictionary<string, int>[length];
                _diff[1] = new Dictionary<string, int>[length];
                _oldPhaseAText = new string[length];
                _oldPhaseBText = new string[length];

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
                            _diff[0][j] = new Dictionary<string, int>() { { "same", 0 }, { "diff", 0 } };
                            _diff[1][j] = new Dictionary<string, int>() { { "same", 0 }, { "diff", 0 } };
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
                if (Process())
                {
                    Scan();
                    Detection();

                    if (_break)
                        break;
                }
                else
                {
                    // The Process function returns false when user closed the Attempting to reconnect
                    // popup window or if the meter was switched so this function forces
                    // the user to go back a page and cancels the phase diagnostic reads
                    if (_serial.SerialNo != _meter.ID)
                    {
                        // Command to change the meter being actively evaluated and 
                        // associated with the serial port
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new MessageCenter("switchMeters"));

                        break;
                    }
                    else
                    {
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                                .Publish(new MessageCenter("backward"));

                        // Loop here until external code sets break state to avoid race condition
                        while (!_break) { }

                        break;
                    }
                }
            }

            // reset break value so we can continuously execute this function 
            // if we need to revisit this page
            _break = false;
        }

        /// <summary>
        /// Issues Phase Diagnostic call for meter 
        /// </summary>
        /// <returns>Boolean indicating success of Phase Diagnostic call</returns>
        private bool Process()
        {
            var tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var task = Task.Factory.StartNew(() => _serial.PhaseDiagnostic(token), token);

            // Waits 5 seconds for Phase Diagnostic call to finish execution if not then display
            // modal to user and attempt to reconnect 
            if (!task.Wait(5000, token))
            {
                // Create thread that launches modal window for user and continues to poll
                // original process to determine if it has completed
                Thread t = new Thread(new ThreadStart(() =>
                {
                    SerialDisconnectView sd = null;
                    SerialDisconnectViewModel sdvm = new SerialDisconnectViewModel(task, token, tokenSource);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        sd = new SerialDisconnectView
                        {
                            Owner = Application.Current.MainWindow,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            DataContext = sdvm
                        };

                        sd.Show();
                    });

                    var monitor = Task.Run(sdvm.Poll);
                    monitor.Wait();

                    if (monitor.Result)
                    {
                        // Close the window if the program has successfully re-established communication
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            sdvm.UserClosedWindow = false;
                            sd.Close();
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
            foreach (string s in buffer.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string[] cols = s.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                // if not data row continue to next line
                if (cols.Length != 10 || s.Contains("$lot"))
                    continue;

                int meter = Convert.ToInt32(cols[0]) - 32;
                bool phaseA = cols[3] == "1" ? true : false;
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

            OnPropertyChanged(nameof(PhaseAText));
            OnPropertyChanged(nameof(PhaseBText));

            if (string.IsNullOrEmpty(buffer))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Sets the Completed variable for the page, only set completed if at least one 
        /// phase of one channel has been commissioned
        /// </summary>
        private void Scan()
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
            if (_channelComm && (Meter.Disposition > 0 && !string.IsNullOrEmpty(Meter.Floor) && !string.IsNullOrEmpty(Meter.Location)))
            {
                Completed = true;
                return;
            }

            Completed = false;
        }

        /// <summary>
        /// Stops the Phase Diagnostic call loop
        /// </summary>
        private void StopPhaseDiagnostic()
        {
            _break = true;
        }

        #endregion

        #endregion
    }
}
