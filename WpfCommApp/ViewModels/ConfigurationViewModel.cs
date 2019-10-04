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
        private ICommand _saveAndContinue;
        private ICommand _loadMeter;
        private bool _completed;
        private int _idx;

        #endregion

        #region Properties
        public string Name
        {
            get
            {
                return "Configuration";
            }
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

        public int Idx
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
                    _meter = (Application.Current.Properties["meters"] as List<Meter>)[Idx];

                    // this code will move when i make the number of channels dependent on user input
                    for (int i = 0; i < 12; i++)
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

        #endregion

        #region Commands

        public ICommand SaveAndContinue
        {
            get
            {
                if (_saveAndContinue == null)
                    _saveAndContinue = new RelayCommand(p => SaveContinue());

                return _saveAndContinue;
            }
        }

        public ICommand LoadMeter
        {
            get
            {
                if (_loadMeter == null)
                    _loadMeter = new RelayCommand(p => GetMeter());

                return _loadMeter;
            }
        }
        #endregion

        #region Constructor

        public ConfigurationViewModel()
        {
        }

        #endregion

        #region Methods

        private void GetMeter()
        {
            Meter = (Application.Current.Properties["meters"] as List<Meter>)[Idx];
            for (int i = 0; i < 12; i++)
                Meter.Channels.Add(new Channel(i + 1));

            OnPropertyChanged(nameof(Channels));
        }

        private void SaveContinue()
        {
            _completed = true;
            List<Meter> meters = (Application.Current.Properties["meters"] as List<Meter>);
            int idx = meters.FindIndex(m => m.ID == Meter.ID);
            if (idx != -1)
                meters[idx] = _meter;
            Application.Current.Properties["meters"] = meters;

            // Send message to enable forward button
            (Application.Current.Properties["MessageBus"] as MessageBus)
                .Publish(new ScreenComplete("cont"));
        }

        #endregion
    }
}
