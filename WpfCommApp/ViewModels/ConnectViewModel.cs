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
using System.Management;

namespace WpfCommApp
{
    public class ConnectViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private bool _busy;
        private bool? _connected;
        private string _comPort;

        private IAsyncCommand _closeSerial;
        private ICommand _portSelect;
        private IAsyncCommand _serialConn;

        private SerialComm _serial;
        private ObservableCollection<Serial> _comPorts;

        #endregion

        #region Properties

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

        public bool IsAvailable
        {
            get
            {
                return (_connected == true) ? false : true;
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

        public string Name { get { return "Serial Connection"; } }

        #endregion

        #region Commands

        public IAsyncCommand BreakSerial
        {
            get
            {
                if (_closeSerial == null)
                    _closeSerial = new AsyncRelayCommand(CloseSerial, () => { return !IsBusy; }) ;

                return _closeSerial;
            }
        }

        public IAsyncCommand SerialConnection
        {
            get
            {
                if (_serialConn == null)
                    _serialConn = new AsyncRelayCommand(SetupSerial, () => { return !IsBusy; });

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

        #endregion

        #region Constructor

        public ConnectViewModel()
        {
            ComPorts = new ObservableCollection<Serial>();
            foreach(ManagementObject m in new ManagementObjectSearcher("root\\cimv2",
                                          "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"").Get())
            {
                string com = m["Name"] as string;
                int start = com.IndexOf("(") + 1;
                com = com.Substring(start, com.Length - start - 1);

                if (!(m["Name"] as string).Contains("Virtual"))
                    ComPorts.Add(new Serial(com, false));
                else
                    ComPorts.Add(new Serial(com, true));
            }

            IsBusy = false;
            IsConnected = null;
            COMPORT = "          ";
        }

        #endregion

        #region Methods

        #region Public

        #endregion

        #region Private

        /// <summary>
        /// Sets the port that will be used initiate the serial connection
        /// </summary>
        /// <param name="p">String representing com port</param>
        private void ChangePort(string p)
        {
            COMPORT = p;
        }

        /// <summary>
        /// Closes open serial connection
        /// </summary>
        /// <returns></returns>
        private async Task CloseSerial()
        {
            try
            {
                IsBusy = true;
                var comms = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
                var port = comms.Select((value, index) => new { value, index })
                        .Where(x => x.value.COM == _comPort)
                        .Select(x => x.index).First();

                // Closes the port only if it is currently open
                if (comms[port].IsOpen)
                {
                    comms[port].Close();

                    // Send message to close the tab attached that is attached to the serial port that is being closed
                    (Application.Current.Properties["MessageBus"] as MessageBus)
                        .Publish(new MessageCenter("closeTab", new Tuple<int, string>(port, comms[port].SerialNo)));
                }
                IsConnected = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Connects a serial port to a meter and creates a new meter object, progressing user to next page
        /// </summary>
        /// <returns></returns>
        private async Task SetupSerial()
        {
            try
            {
                IsBusy = true;
                ObservableCollection<SerialComm> comms = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>);
                Dictionary<string, Meter> meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);
                _serial = comms.Last();

                if (_serial.IsOpen)
                {
                    comms.Add(new SerialComm());
                    _serial = comms.Last();
                }

                // Logs into meter and retrieves it's ID
                string id = _serial.SetupSerial(_comPort);
                var query = ComPorts.Select((value, index) => new { value, index })
                        .Where(x => x.value.Name == _comPort)
                        .Select(x => x.index)
                        .Take(1);
                ComPorts[query.ElementAt(0)].Used = true;

                // Creates new meter if imported meters do not match current meter
                if (!meters.ContainsKey(id))
                {
                    meters.Add(id, new Meter());
                    meters[id].ID = id;

                    // Sets the size of the meter based on the type/version of meter connected
                    string version = _serial.GetVersion();
                    string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (lines[2].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)[1].StartsWith("593"))
                    {
                        meters[id].Size = 12;

                        // Retrieves child serial numbers and assigns them to respective channel
                        string[] serials = _serial.GetChildSerial().Split(',');
                        for (int i = 0; i < meters[id].Size; i++)
                        {
                            meters[id].Channels.Add(new Channel(i + 1));
                            meters[id].Channels[i].Serial = serials[i];
                        }
                    }
                }

                IsConnected = true;
                Completed = true;

                // Send message to create new tab that will be able to commission a meter
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new MessageCenter("newTab", new Tuple<int, string>(comms.Count - 1, id)));
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

        #endregion

        #endregion
    }
}
