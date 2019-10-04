using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Linq;
using Hellang.MessageBus;

namespace WpfCommApp
{
    public class ConnectViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private IAsyncCommand _serialConn;
        private ICommand _portSelect;
        private IAsyncCommand _closeSerial;

        private SerialComm _serial;
        private string _comport;
        private int _idx;
        private bool _busy;
        private bool? _connected;
        private ObservableCollection<string> _comPorts;

        #endregion

        #region Properties
        public string Name
        {
            get
            {
                return "Serial Connection";
            }
        }

        public string COM
        {
            get
            {
                return _comport;
            }

            set
            {
                if (value != _comport)
                {
                    _comport = value;
                    OnPropertyChanged(nameof(COM));
                }
            }
        }

        public bool IsBusy
        {
            get
            {
                return _busy;
            }
            private set
            {
                if (_busy != value)
                {
                    _busy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public bool? IsConnected
        {
            get
            {
                return _connected;
            }
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(IsAvailable));
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                return IsConnected == true;
            }
        }

        public bool IsAvailable
        {
            get
            {
                return (_connected == true) ? false : true;
            }
        }

        public int Idx
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public ObservableCollection<string> ComPorts
        {
            get
            {
                return _comPorts;
            }
            set
            {
                if (_comPorts == null)
                    _comPorts = value;
            }
        }

        public bool Completed { get; private set; }

        #endregion

        #region Commands
        public IAsyncCommand SerialConnection
        {
            get
            {
                if (_serialConn == null)
                {
                    _serialConn = new AsyncRelayCommand(SetupSerial, CanExec);
                }

                return _serialConn;
            }
        }

        public ICommand SetPort
        {
            get
            {
                if (_portSelect == null)
                {
                    _portSelect = new RelayCommand(p => ChangePort((string) p));
                }

                return _portSelect;
            }
        }

        public IAsyncCommand BreakSerial
        {
            get
            {
                if (_closeSerial == null)
                    _closeSerial = new AsyncRelayCommand(CloseSerial, CanExec);

                return _closeSerial;
            }
        }
        #endregion

        #region Constructor
        public ConnectViewModel()
        {
            COM = "          ";
            ComPorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            IsBusy = false;
            IsConnected = null;
        }

        #endregion

        #region Methods
        private async Task SetupSerial()
        {
            try
            {
                IsBusy = true;
                List<SerialComm> comms = (Application.Current.Properties["serial"] as List<SerialComm>);
                if (comms.Count == 1)
                {
                    _serial = comms[0];
                    Idx = 0;
                }
                else
                {
                    comms.Add(new SerialComm());
                    _serial = comms.Last();
                    Idx = comms.Count - 1;
                }

                List<Meter> meters = (Application.Current.Properties["meters"] as List<Meter>);
                string id = _serial.SetupSerial(_comport);
                meters[Idx].ID = id;
                IsConnected = true;
                Completed = true;

                // Send message to enable forward button
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new ScreenComplete());
            }
            catch
            {
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CloseSerial()
        {
            try
            {
                IsBusy = true;
                _serial.Close();
                IsConnected = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ChangePort(string p)
        {
            COM = p;
        }

        private bool CanExec()
        {
            return !IsBusy;
        }

        #endregion
    }
}
