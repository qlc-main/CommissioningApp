using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class Channel : ObservableObject
    {
        #region Fields
        private readonly int _id;
        private int _primary;
        private bool? _phase1;
        private bool? _phase2;
        private bool[] _forced;
        private string _secondaryString;
        private string _breakerNumber;
        private string _apartmentNumber;
        private string _notes;
        private string[] _forcedReason;

        #endregion

        #region Properties
        public int ID
        {
            get
            {
                return _id;
            }
        }

        public int Primary
        {
            get
            {
                return _primary;
            }
            set
            {
                if (_primary != value)
                    _primary = value;
            }
        }

        public float Secondary
        {
            get
            {
                if (float.TryParse(_secondaryString, out float val))
                    return val;
                else
                    return 0.0f;
            }
        }

        public string SecondaryString
        {
            get { return _secondaryString; }
            set
            {
                if (_secondaryString != value)
                    _secondaryString = value;
            }
        }

        public bool[] Forced
        {
            get { return _forced; }
            set { _forced = value; }
        }

        public bool? Phase1
        {
            get { return _phase1; }
            set
            {
                if (_phase1 != value)
                {
                    _phase1 = value;
                    if (value == null)
                        _forced[0] = true;
                    OnPropertyChanged(nameof(Phase1));
                }
            }
        }

        public bool? Phase2
        {
            get { return _phase2; }
            set
            {
                if (_phase2 != value)
                {
                    _phase2 = value;
                    if (value == null)
                        _forced[1] = true;
                    OnPropertyChanged(nameof(Phase2));
                }
            }
        }

        public bool Commissioned
        {
            get
            {
                return _phase1 == true && _phase2 == true;
            }
        }

        public string BreakerNumber
        {
            get
            {
                return _breakerNumber;
            }
            set
            {
                if (_breakerNumber != value)
                    _breakerNumber = value;
            }
        }

        public string ApartmentNumber
        {
            get
            {
                return _apartmentNumber;
            }
            set
            {
                if (_apartmentNumber != value)
                    _apartmentNumber = value;
            }
        }

        public string[] ForcedReason
        {
            get { return _forcedReason; }
            set
            {
                _forcedReason = value;
                OnPropertyChanged(nameof(ForcedReason));
            }
        }

        public string Notes
        {
            get { return _notes; }
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged(nameof(Notes));
                }
            }
        }

        #endregion

        #region Constructors

        public Channel(int id)
        {
            _id = id;
            _primary = 100;
            _secondaryString = "0.1";
            _forced = new bool[2] { true, true };
            _phase1 = null;
            _phase2 = null;
            _apartmentNumber = string.Empty;
            _breakerNumber = string.Empty;
            _forcedReason = new string[2] { string.Empty, string.Empty };
        }

        #endregion
    }
}
