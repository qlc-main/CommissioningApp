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

        private bool _completed;
        private string[] _ctTypes;
        private string _id;

        private ICommand _notCommissioned;

        private Meter _meter;

        #endregion

        #region Properties

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                    _meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[ID];

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
        }

        public string[] CTTypes
        {
            get { return _ctTypes; }
        }

        public string ID
        {
            get { return _id; }
            set { if (_id != value) _id = value; }
        }

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

        public ConfigurationViewModel(string id)
        {
            _id = id;
            _completed = true;
            _ctTypes = new string[3] { "flex", "solid", "split" };
        }

        #endregion

        #region Methods

        #region Public

        #endregion

        #region Private

        private void Uncommission(int id)
        {
            Channels[id - 1].Primary = "";
            Channels[id - 1].Secondary = "";
            Channels[id - 1].CTType = "";
            OnPropertyChanged(nameof(Channels));
        }

        #endregion

        #endregion
    }
}
