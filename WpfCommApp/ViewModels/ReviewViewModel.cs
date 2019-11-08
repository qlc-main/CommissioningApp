using Hellang.MessageBus;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    public class ReviewViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private bool _completed;
        private Dictionary<int, string> _dispositions;
        private string _id;

        #endregion

        #region Properties

        public bool Completed
        {
            get { return _completed; }

            private set
            {
                if (_completed != value)
                    _completed = value;
            }
        }

        public string[] CTTypes { get; }

        public string Disposition
        {
            get { return _dispositions[Meter.Disposition]; }
        }

        public string FSReturn
        {
            get { return Meter.FSReturn ? "No" : "Yes"; }
        }

        public Meter Meter { get; private set; }

        public string Name { get { return "Review"; } }

        public string OprComplete
        {
            get { return Meter.OprComplete ? "Yes" : "No"; }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructors

        public ReviewViewModel(string id)
        {
            _id = id;
            Meter = (Application.Current.Properties["meters"] as Dictionary<string, Meter>)[_id];
            _completed = true;
            CTTypes = (Application.Current.Properties["cttypes"] as string[]);
            _dispositions = (Application.Current.Properties["dispositions"] as Dictionary<string, int>).ToDictionary(x => x.Value, x => x.Key);
        }

        #endregion

        #region Methods

        #region Public 

        #endregion

        #region Private

        #endregion

        #endregion
    }
}
