﻿using Hellang.MessageBus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfCommApp.Helpers;

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
                var comms = Globals.Serials;
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
            IsBusy = false;
            COMPORT = "          ";
        }

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Code used to create/open meter object and create a tab for this item
        /// </summary>
        public async Task<string> CreateNewMeter()
        {
            Dictionary<string, Meter> meters = Globals.Meters;

            // Logs into meter and retrieves it's ID
            _id = await _serial.SetupSerial(_comPort);
            if (_id == "")
            {
                InfoView info = null;
                InfoViewModel ifvm = new InfoViewModel("Unable to Connect", "COM Port already open, close port or choose another COM...");

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

                return null;
            }

            var query = ComPorts.Select((value, index) => new { value, index })
                    .Where(x => x.value.Name == _comPort)
                    .Select(x => x.index)
                    .Take(1);
            ComPorts[query.ElementAt(0)].Used = true;

            // Creates new meter if imported meters do not match current meter
            if (!meters.ContainsKey(_id))
            {
                Globals.Logger.LogInformation($"Creating new meter for {_id}");
                meters.Add(_id, new Meter());
                var meter = meters[_id];
                meter.ID = _id;

                // Sets the size of the meter based on the type/version of meter connected
                string version = await _serial.GetVersion();
                string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                string inspect = "";
                bool stop = false;
                foreach(string line in lines)
                {
                    if (line.ToLower().Contains("software"))
                        stop = true;
                    else if (stop)
                    {
                        inspect = line;
                        break;
                    }
                }

                var cols = inspect.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries);
                string[] serials = (await _serial.GetChildSerial()).Split(',');
                if (cols[1] == "59330354")
                {
                    meter.Firmware = cols[1];
                    _serial.NewFirmware = true;
                    // Retrieves child serial numbers and assigns them to respective channel
                    for (int i = 0; i < 12; i++)
                    {
                        meter.Channels.Add(new Channel(i + 1));
                        meter.Channels[i].Serial = serials[i];
                    }

                    Globals.Logger.LogInformation($"Meter {_id} has firmware version {meter.Firmware}.");
                }
                else if (cols[1] == "59330353")
                {
                    meter.Firmware = cols[1];
                    // Retrieves child serial numbers and assigns them to respective channel
                    for (int i = 0; i < 12; i++)
                    {
                        meter.Channels.Add(new Channel(i + 1));
                        meter.Channels[i].Serial = serials[i];
                    }
                    Globals.Logger.LogInformation($"Meter {_id} has firmware version {meter.Firmware}.");
                }
                else
                {
                    InfoView info = null;
                    InfoViewModel ifvm = new InfoViewModel("Bad Firmware Version", $"Invalid Firmware Version {cols[1]}");

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

                    return null;
                }
            }
            else
            {
                // Firmware was not populated from the file so read directly from the meter
                if (meters[_id].Firmware == null)
                {
                    string version = await _serial.GetVersion();
                    string[] lines = version.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                    string inspect = "";
                    bool stop = false;
                    foreach (string line in lines)
                    {
                        if (line.ToLower().Contains("software"))
                            stop = true;
                        else if (stop)
                        {
                            inspect = line;
                            break;
                        }
                    }

                    var cols = inspect.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries);
                    meters[_id].Firmware = cols[1];
                    Globals.Logger.LogInformation($"Meter {_id} has firmware version {meters[_id].Firmware}.");
                }

                // This is an unknown firmware version, print message to user and exit
                if (meters[_id].Firmware != "59330353" && meters[_id].Firmware != "59330354")
                {
                    InfoView info = null;
                    InfoViewModel ifvm = new InfoViewModel("Bad Firmware Version", $"Invalid Firmware Version {meters[_id].Firmware}");

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

                    return null;
                }
                else
                    _serial.NewFirmware = (meters[_id].Firmware == "59330354");
            }

            return _id;
        }

        public async void Dispose()
        {
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
                var comms = Globals.Serials;
                var query = ComPorts.Select((value, index) => new { value, index })
                    .Where(x => x.value.Name == _comPort)
                    .Select(x => x.index)
                    .Take(1);
                var idx = query.ElementAt(0);

                // Closes the port only if it is currently open
                if (ComPorts[idx].Used)
                {
                    // Send message to close the tab attached that is attached to the serial port that is being closed
                    (Application.Current.Properties["MessageBus"] as MessageBus)
                        .Publish(new MessageCenter("closeTab", new Tuple<string, string>(_comPort, comms[_comPort].SerialNo)));

                    comms.Remove(_comPort);
                    ComPorts[idx].Used = false;
                    Globals.Logger.LogInformation($"Closed serial port {_comPort}");
                }
            }
            finally
            {
                IsBusy = false;
                COMPORT = "          ";
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
                var tokenSource = new CancellationTokenSource();
                CancellationToken token = tokenSource.Token;
                var task = EstablishSerial(token);

                // Waits 5 seconds for Phase Diagnostic call to finish execution if not then display
                // modal to user and attempt to reconnect 
                if (!task.Wait(500, token))
                {
                    // Create thread that launches modal window for user and continues to poll
                    // original process to determine if it has completed
                    Thread t = new Thread(new ThreadStart(async () =>
                    {
                        InfoView info = null;
                        InfoViewModel ifvm = new InfoViewModel(task, tokenSource, "Meter Serial Connection", "Connecting to Meter");

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

                        var monitor = ifvm.Poll();
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

        private async Task EstablishSerial(CancellationToken token)
        {
            IsBusy = true;
            Dictionary<string, SerialComm> comms = Globals.Serials;

            if (!comms.ContainsKey(COMPORT))
            {
                comms.Add(COMPORT, new SerialComm());
                _serial = comms[COMPORT];
            }
            else
            {
                _serial = comms[COMPORT];
                if (_serial.IsOpen)
                    return;
            }

            Globals.Logger.LogInformation($"Opened serial port {COMPORT}");
            if (await CreateNewMeter() == null)
            {
                _serial.Close();
                IsBusy = false;
                Completed = false;
                return;
            }

            Completed = true;

            // Send message to create new tab that will be able to commission a meter
            (Application.Current.Properties["MessageBus"] as MessageBus)
                .Publish(new MessageCenter("newTab", new Tuple<string, string>(_comPort, _id)));
        }

        #endregion

        #endregion
    }
}
