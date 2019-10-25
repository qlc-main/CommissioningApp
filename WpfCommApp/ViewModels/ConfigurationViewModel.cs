using Hellang.MessageBus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    public class ConfigurationViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private int _idx;
        private bool _completed;
        private string[] _ctTypes;

        private Meter _meter;

        private ICommand _notCommissioned;

        #endregion

        #region Properties

        public Meter Meter
        {
            get
            {
                return _meter;
            }

            private set
            {
                _meter = value;
                OnPropertyChanged(nameof(Meter));
            }
        }

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public bool Completed
        {
            get { return _completed; }
        }

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                {
                    Meter = (Application.Current.Properties["meters"] as ObservableCollection<Meter>)[IDX];

                    if (Meter.Channels.Count == 0)
                    {
                        var serial = (Application.Current.Properties["serial"] as ObservableCollection<SerialComm>)[IDX];
                        string[] serials = serial.GetChildSerial().Split(',');
                        // this code will move when i make the number of channels dependent on user input
                        for (int i = 0; i < _meter.Size; i++)
                        {
                            _meter.Channels.Add(new Channel(i + 1));
                            //_meter.Channels[i].Serial = serials[i];
                            _meter.Channels[i].Serial = i < serials.Length ? serials[i] : "";       // delete later, used for debugging purposes
                        }

                        // to here 
                    }
                }

                return _meter.Channels;
            }
            set
            {
                _meter.Channels = value;
                OnPropertyChanged(nameof(Channels));
            }
        }

        public string[] CTTypes
        {
            get { return _ctTypes; }
        }

        public string Name { get { return "Configuration"; } }

        #endregion

        #region Commands

        public ICommand NotCommissioned
        {
            get
            {
                if (_notCommissioned == null)
                    _notCommissioned = new RelayCommand(p => Uncommission((int) p));

                return _notCommissioned;
            }
        }

        #endregion

        #region Constructor

        public ConfigurationViewModel(int idx)
        {
            _idx = idx;
            _completed = true;
            _ctTypes = new string[3] { "flex", "solid", "split" };
        }

        #endregion

        #region Methods

        private void Uncommission(int id)
        {
            Channels[id - 1].Primary = "";
            Channels[id - 1].Secondary = "";
            Channels[id - 1].CTType = "";
            OnPropertyChanged(nameof(Channels));
        }

        #endregion
    }
}
