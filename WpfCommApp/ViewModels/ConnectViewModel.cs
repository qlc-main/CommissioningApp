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
        private IAsyncCommand _phaseDiag;
        private ICommand _portSelect;

        private SerialComm _serial;
        private string _comport;
        private bool _busy;
        private bool _connected;
        private bool _attempted;
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

        public bool IsConnected
        {
            get
            {
                return _connected;
            }
            private set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public bool IsAttempted
        {
            get
            {
                return _attempted;
            }
            private set
            {
                if (_attempted != value)
                {
                    _attempted = value;
                    OnPropertyChanged(nameof(IsAttempted));
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                return !IsBusy;
            }
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

        public IAsyncCommand PhaseDiag
        {
            get
            {
                if (_phaseDiag == null)
                {
                    _phaseDiag = new AsyncRelayCommand(PD, CanExec);
                }

                return _phaseDiag;
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

        #endregion

        #region Constructor
        public ConnectViewModel()
        {
            ComPorts = new ObservableCollection<string>(SerialPort.GetPortNames().ToList().OrderBy(p => p));
            IsBusy = false;
        }
        #endregion

        #region Methods
        private async Task SetupSerial()
        {
            try
            {
                IsBusy = true;
                _serial = (SerialComm)Application.Current.Properties["serial"];
                _serial.SetupSerial(_comport);
                IsConnected = true;
                IsAttempted = true;

                // Send message to enable forward button
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new ScreenComplete());
            }
            catch
            {
                IsAttempted = true;
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

        private async Task PD()
        {
            try
            {
                IsBusy = true;
                _serial.PD();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExec()
        {
            return !IsBusy;
        }

        #endregion
    }
}
