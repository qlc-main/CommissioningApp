using Hellang.MessageBus;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    public class ReviewViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private bool _break;
        private bool _completed;
        private string[] _ctTypes;
        private Dictionary<int, string> _dispositions;
        private int _idx;
        private Meter _meter;

        #endregion

        #region Properties

        public ObservableCollection<Channel> Channels
        {
            get
            {
                if (_meter == null)
                    _meter = (Application.Current.Properties["meters"] as ObservableCollection<Meter>)[IDX];

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

            private set
            {
                if (_completed != value)
                {
                    _completed = value;

                    // Enable the forward button if necessary details completely filled in
                    if (value)
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete());

                    // Disable the forward button if at least one necessary detail is unavailable
                    else
                        // Send message to enable forward button
                        (Application.Current.Properties["MessageBus"] as MessageBus)
                            .Publish(new ScreenComplete("disable"));
                }
            }
        }

        public string[] CTTypes
        {
            get { return _ctTypes; }
        }

        public string Disposition
        {
            get { return _dispositions[_meter.Disposition]; }
        }

        public string FSReturn
        {
            get { return _meter.FSReturn ? "No" : "Yes"; }
        }

        public int IDX
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        public Meter Meter
        {
            get { return _meter; }
        }

        public string Name { get { return "Review"; } }

        public string Notes
        {
            get { return _meter.Notes; }
            set
            {
                _meter.Notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public string OprComplete
        {
            get { return _meter.OprComplete ? "Yes" : "No"; }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructors

        public ReviewViewModel(int idx)
        {
            _idx = idx;
            _completed = true;
            _ctTypes = new string[3] { "flex", "solid", "split" };
            _dispositions = new Dictionary<int, string>() {
                                { 10, "No Problem Found"},
                                { 12, "Wrong Site Wiring"},
                                { 75, "Follow Up Required"},
                                { 113, "Follow Up Resolved"},
                                { 41, "No Meter Communication"},
                                { 78, "Reversed CT(s)"},
                                { 79, "Reversed Phase(s)"} };
        }

        #endregion

        #region Methods

        #endregion

        #region Commands

        #endregion
    }
}
