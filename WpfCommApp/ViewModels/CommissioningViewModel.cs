
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using Hellang.MessageBus;


namespace WpfCommApp
{
    public class CommissioningViewModel : ObservableObject, IPageViewModel
    {

        #region Fields

        private bool _break;
        private bool _completed;

        private Dictionary<string, int>[][] _diff;

        private int _idx;

        private Meter _meter;

        private string[] _oldPhaseAText;
        private string[] _oldPhaseBText;
        private string[][] _phaseAText;
        private string[][] _phaseBText;

        private SerialComm _serial;

        private ICommand _pd;
        private ICommand _spd;

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
            set
            {
                if (_completed != value)
                {
                    _completed = value;

                    // Enable the forward button if at least one channel is commissioned
                    if (value)
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete());

                    // Disable the forward button if no channels are commissioned
                    else
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete("disable"));
                }
            }
        }

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public string Name { get { return "Commissioning"; } }

        public string[][] PhaseAText
        {
            get { return _phaseAText; }
        }

        public string[][] PhaseBText
        {
            get { return _phaseBText; }
        }

        #endregion

        #region Commands

        public ICommand StartAsync
        {
            get
            {
                if (_pd == null)
                    _pd = new RelayCommand(p => GetPD());

                return _pd;
            }
        }

        public ICommand StopAsync
        {
            get
            {
                if (_spd == null)
                    _spd = new RelayCommand(p => SPD());

                return _spd;
            }
        }

        #endregion

        #region Constructors

        public CommissioningViewModel(int idx)
        {
            _phaseAText = new string[4][];
            _phaseBText = new string[4][];
            _diff = new Dictionary<string, int>[2][];
            _idx = idx;
        }

        #endregion

        #region Methods

        private void GetPD()
        {
            Task.Run(PD);
        }

        private void SPD()
        {
            _break = true;
        }

        private async Task PD()
        {
            // Initialize variables if this is the first time 
            if (_serial == null)
            {
                _serial = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>)[IDX];
                _meter = (Application.Current.Properties["meters"] as ObservableCollection<Meter>)[IDX];
                OnPropertyChanged(nameof(Channels));

                int length = Channels.Count;
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

            while (true)
            {
                Process();
                Scan();
                Detection();

                if (_break)
                    break;
            }

            // reset break value so we can continuously execute this function 
            // if we need to revisit this page
            _break = false;
        }

        private void Process()
        {
            string buffer = _serial.PD();
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
                    _phaseAText[0][meter] = float.Parse(cols[6]).ToString("0.00");
                    float watts = float.Parse(cols[8]);
                    _phaseAText[1][meter] = (watts / 1000).ToString("0.00");
                    double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[9]), 2));
                    if (double.IsNaN(temp))
                        _phaseAText[2][meter] = "--";
                    else
                        _phaseAText[2][meter] = temp.ToString("0.00");
                }
                else
                {
                    _phaseBText[0][meter] = float.Parse(cols[6]).ToString("0.00");
                    float watts = float.Parse(cols[8]);
                    _phaseBText[1][meter] = (watts / 1000).ToString("0.00");
                    double temp = watts / Math.Sqrt(Math.Pow(watts, 2) + Math.Pow(float.Parse(cols[9]), 2));
                    if (double.IsNaN(temp))
                        _phaseBText[2][meter] = "--";
                    else
                        _phaseBText[2][meter] = temp.ToString("0.00");
                }

                if (_break)
                    return;
            }

            OnPropertyChanged(nameof(PhaseAText));
            OnPropertyChanged(nameof(PhaseBText));
        }

        private void Detection()
        {
            for (int i = 0; i < PhaseAText[0].Length; i++)
            {
                // check if the values differ
                float oldP, newP;
                if (float.TryParse(_oldPhaseAText[i], out oldP)) {
                    newP = float.Parse(PhaseAText[0][i]);
                    if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1) {
                        _diff[0][i]["diff"]++;
                        _diff[0][i]["same"] = 0;
                    } else
                        _diff[0][i]["same"]++;

                    // if the same for 3 times clear diff reg
                    if (_diff[0][i]["same"] == 3)
                    {
                        _diff[0][i]["diff"] = 0;
                        _diff[0][i]["same"] = 0;
                    }

                    // if diff reg counts to 3 change phase and then clear reg
                    if (_diff[0][i]["diff"] == 3 && Channels[i].Phase1 == null)
                    {
                        Channels[i].Phase1 = false;
                        Channels[i].Forced[0] = false;
                        _diff[0][i]["diff"] = 0;
                    }
                }

                if (float.TryParse(_oldPhaseBText[i], out oldP))
                {
                    newP = float.Parse(PhaseBText[0][i]);
                    if (Math.Abs(oldP - newP) / ((oldP + newP) / 2) > 1)
                    {
                        _diff[1][i]["diff"]++;
                        _diff[1][i]["same"] = 0;
                    }
                    else
                        _diff[1][i]["same"]++;

                    if (_diff[1][i]["same"] == 3)
                    {
                        _diff[1][i]["diff"] = 0;
                        _diff[1][i]["same"] = 0;
                    }

                    // if diff reg counts to 3 change phase and then clear reg
                    if (_diff[1][i]["diff"] == 3 && Channels[i].Phase2 == null)
                    {
                        Channels[i].Phase2 = false;
                        Channels[i].Forced[1] = false;
                        _diff[1][i]["diff"] = 0;
                    }
                }

                if (_break)
                    return;
                //ThreadPool.QueueUserWorkItem(new WaitCallback(Compare), new object[] { _oldPhaseAText[0], _phaseAText[0], i, 'a' });
                //ThreadPool.QueueUserWorkItem(new WaitCallback(Compare), new object[] { _oldPhaseBText[0], _phaseBText[0], i, 'b' });
            }

            _oldPhaseAText = PhaseAText[0].Clone() as string[];
            _oldPhaseBText = PhaseBText[0].Clone() as string[];
        }

        private void Scan()
        {
            foreach (Channel c in Channels)
            {
                if (c.Phase1 == true || c.Phase2 == true) {
                    Completed = true;
                    return;
                }

                if (_break)
                    return;
            }

            Completed = false;
        }
        
        #endregion
    }
}
