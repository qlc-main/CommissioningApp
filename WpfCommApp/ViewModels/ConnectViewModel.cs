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
        private string _comPort;
        private string _id;

        private IAsyncCommand _closeSerial;
        private IAsyncCommand _newMeter;
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
                var comms = (Application.Current.Properties["serial"] as Dictionary<string, SerialComm>);
                if (comms.ContainsKey(COMPORT))
                    return comms[COMPORT].IsOpen;
                else
                    return null;
            }
        }

        public bool IsConnectEnabled
        {
            get { return COMPORT != "          " && IsConnected != true; }
        }

        public bool IsDisconnectEnabled
        {
            get { return COMPORT != "          " && IsConnected == true; }
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

        public IAsyncCommand NewMeter
        {
            get
            {
                if (_newMeter == null)
                    _newMeter = new AsyncRelayCommand(ConnectToNewMeter, () => { return !IsBusy; });

                return _newMeter;
            }
        }

        public IAsyncCommand SerialConnection
        {
            get
            {
                if (_serialConn == null)
                    _serialConn = new AsyncRelayCommand(OpenSerial, () => { return !IsBusy; });

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
            COMPORT = "          ";
        }

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Code used to create/open meter object and create a tab for this item
        /// </summary>
        public void CreateNewMeter()
        {
            Dictionary<string, Meter> meters = (Application.Current.Properties["meters"] as Dictionary<string, Meter>);

            // Logs into meter and retrieves it's ID
            _id = _serial.SetupSerial(_comPort);
            var query = ComPorts.Select((value, index) => new { value, index })
                    .Where(x => x.value.Name == _comPort)
                    .Select(x => x.index)
                    .Take(1);
            ComPorts[query.ElementAt(0)].Used = true;

            // Creates new meter if imported meters do not match current meter
            if (!meters.ContainsKey(_id))
            {
                meters.Add(_id, new Meter());
                var meter = meters[_id];
                meter.ID = _id;

                // Sets the size of the meter based on the type/version of meter connected
                string version = _serial.GetVersion();
                string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines[2].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)[1].StartsWith("593"))
                {
                    meter.Size = 12;

                    // Retrieves child serial numbers and assigns them to respective channel
                    string[] serials = _serial.GetChildSerial().Split(',');
                    for (int i = 0; i < meter.Size; i++)
                    {
                        meter.Channels.Add(new Channel(i + 1));
                        meter.Channels[i].Serial = serials[i];
                    }
                }
            }
        }

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
        /// Function used to create new meter if there is not a meter object 
        /// connected to this open com port
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToNewMeter()
        {
            if (_serial.IsOpen)
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new MessageCenter("newMeter"));
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
                var comms = (Application.Current.Properties["serial"] as Dictionary<string, SerialComm>);

                // Closes the port only if it is currently open
                if (comms[_comPort].IsOpen)
                {
                    comms[_comPort].Close();

                    // Send message to close the tab attached that is attached to the serial port that is being closed
                    (Application.Current.Properties["MessageBus"] as MessageBus)
                        .Publish(new MessageCenter("closeTab", new Tuple<string, string>(_comPort, comms[_comPort].SerialNo)));
                }
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
        private async Task OpenSerial()
        {
            try
            {
                IsBusy = true;
                Dictionary<string, SerialComm> comms = (Application.Current.Properties["serial"] as Dictionary<string, SerialComm>);

                if (!comms.ContainsKey(COMPORT))
                {
                    comms.Add(COMPORT, new SerialComm());
                    _serial = comms[COMPORT];
                }
                else
                {
                    return;
                }

                CreateNewMeter();

                Completed = true;

                // Send message to create new tab that will be able to commission a meter
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new MessageCenter("newTab", new Tuple<string, string>(_comPort, _id)));
            }
            catch
            {
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
