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

        private string _id;

        private ICommand _notCommissioned;

        private Meter _meter;

        #endregion

        #region Properties

        public bool Completed { get; }

        public string[] CTTypes { get; }

        public Meter Meter
        {
            get { return _meter; }

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

        /// <summary>
        /// Constructor that initiates the variables necessary for Configuration Page
        /// </summary>
        /// <param name="id"></param>
        public ConfigurationViewModel(string id)
        {
            _id = id;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            Completed = true;
            CTTypes = (Application.Current.Properties["cttypes"] as string[]);
        }

        #endregion

        #region Methods

        #region Public

        #endregion

        #region Private

        /// <summary>
        /// Remove the Primary, Secondary, and CTType which will decommission the channel
        /// and not allow it to be commissioned on the next page
        /// </summary>
        /// <param name="id"></param>
        private void Uncommission(int id)
        {
            _meter.Channels[id - 1].Primary = "";
            _meter.Channels[id - 1].Secondary = "";
            _meter.Channels[id - 1].CTType = "";
            OnPropertyChanged(nameof(Meter));
        }

        #endregion

        #endregion
    }
}
