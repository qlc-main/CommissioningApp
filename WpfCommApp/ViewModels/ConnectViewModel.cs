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
using System;

namespace WpfCommApp
{
    public class ConnectViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private IAsyncCommand _serialConn;
        private ICommand _portSelect;
        private IAsyncCommand _closeSerial;

        private SerialComm _serial;
        private int _idx;
        private bool _busy;
        private bool? _connected;
        private string _comPort;
        private ObservableCollection<Serial> _comPorts;

        #endregion

        #region Properties
        public string Name { get { return "Serial Connection"; } }

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

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public ObservableCollection<Serial> ComPorts
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

        public string COMPORT
        {
            get { return _comPort; }
            set
            {
                if (_comPort != value)
                {
                    _comPort = value;
                    OnPropertyChanged(nameof(COMPORT));
                }
            }
        }
        #endregion

        #region Commands
        public IAsyncCommand SerialConnection
        {
            get
            {
                if (_serialConn == null)
                    _serialConn = new AsyncRelayCommand(SetupSerial, CanExec);

                return _serialConn;
            }
        }

        public ICommand SetPort
        {
            get
            {
                if (_portSelect == null)
                    _portSelect = new RelayCommand(p => ChangePort(p as string));

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
            ComPorts = new ObservableCollection<Serial>();
            foreach (string s in SerialPort.GetPortNames())
                ComPorts.Add(new Serial(s, true));

            IsBusy = false;
            IsConnected = null;
            COMPORT = "          ";
        }

        #endregion

        #region Methods
        private async Task SetupSerial()
        {
            try
            {
                IsBusy = true;
                ObservableCollection<SerialComm> comms = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
                ObservableCollection<Meter> meters = (Application.Current.Properties["meters"] as ObservableCollection<Meter>);
                ObservableCollection<Meter> imported = (Application.Current.Properties["importedMeters"] as ObservableCollection<Meter>);
                _serial = comms.Last();
                _comPort = "COM3";      // delete, used for testing
                if (_serial.IsOpen)
                {
                    comms.Add(new SerialComm());
                    _serial = comms.Last();
                    _comPort = "COM4";      // delete, used for testing
                }

                IDX = comms.Count - 1;

                // Logs into meter and retrieves it's ID
                string id = _serial.SetupSerial(_comPort);
                var query = ComPorts.Select((value, index) => new { value, index })
                        .Where(x => x.value.Name == _comPort)
                        .Select(x => x.index)
                        .Take(1);

                // delete if statement later this is more so for debugging purposes
                if (query.Count() != 0)
                    ComPorts[query.ElementAt(0)].Used = true;

                // Creates new meter if imported meter serial nums do not match, current meter
                // or sets the imported meter to last index
                var imports = imported.Where(x => x.ID == id);
                if (imports.Count() == 0)
                {
                    meters.Add(new Meter());
                    meters[IDX].ID = id;
                }
                else
                    meters.Add(imports.ElementAt(0));

                // Sets the size of the meter based on the type of meter connected
                string version = _serial.GetVersion();
                string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines[2].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)[1].StartsWith("593"))
                    meters[IDX].Size = 12;

                IsConnected = true;
                Completed = true;

                // Send message to enable forward button
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new ScreenComplete("switch"));
            }
            catch 
            {
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
                COMPORT = "          ";
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
            COMPORT = p;
        }

        private bool CanExec()
        {
            return !IsBusy;
        }

        #endregion
    }
}
