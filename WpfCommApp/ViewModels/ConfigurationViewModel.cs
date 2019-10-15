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
        private Meter _meter;
        private bool _completed;
        private int _idx;

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
            get
            {
                return _completed;
            }
        }

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                {
                    Meter = (Application.Current.Properties["meters"] as ObservableCollection<Meter>)[IDX];

                    // this code will move when i make the number of channels dependent on user input
                    for (int i = 0; i < _meter.Size; i++)
                        _meter.Channels.Add(new Channel(i + 1));

                    // to here 
                }

                return _meter.Channels;
            }
            set
            {
                _meter.Channels = value;
                OnPropertyChanged(nameof(Channels));
            }
        }

        public string Name { get { return "Configuration"; } }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        public ConfigurationViewModel(int idx)
        {
            _idx = idx;
            _completed = true;
        }

        #endregion

        #region Methods

        #endregion
    }
}
