using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class Channel : ObservableObject
    {
        #region Fields
        private readonly int _id;
        private string _primary;
        private bool? _phase1;
        private bool? _phase2;
        private bool[] _forced;
        private string _secondary;
        private string _breakerNumber;
        private string _apartmentNumber;
        private string _notes;
        private string[] _reason;

        #endregion

        #region Properties
        public int ID
        {
            get
            {
                return _id;
            }
        }

        public string Primary
        {
            get
            {
                return _primary;
            }
            set
            {
                if (_primary != value)
                {
                    if (string.IsNullOrEmpty(_secondary) && string.IsNullOrEmpty(value))
                    {
                        Reason = new string[2] { "NC", "NC" };
                        BreakerNumber = "NC";
                        ApartmentNumber = "NC";
                        Notes = "Not Commissioned";
                    }
                    else if (string.IsNullOrEmpty(_primary))
                    {
                        Reason = new string[2] { string.Empty, string.Empty };
                        BreakerNumber = string.Empty;
                        ApartmentNumber = string.Empty;
                        Notes = string.Empty;
                    }

                    _primary = value;
                    OnPropertyChanged(nameof(Primary));
                }
            }
        }

        public string Secondary
        {
            get { return _secondary; }
            set
            {
                if (_secondary != value)
                {
                    if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(_primary))
                    {
                        Reason = new string[2] { "NC", "NC" };
                        BreakerNumber = "NC";
                        ApartmentNumber = "NC";
                        Notes = "Not Commissioned";
                    }
                    else if (string.IsNullOrEmpty(_secondary))
                    {
                        Reason = new string[2] { string.Empty, string.Empty };
                        BreakerNumber = string.Empty;
                        ApartmentNumber = string.Empty;
                        Notes = string.Empty;
                    }

                    _secondary = value;
                    OnPropertyChanged(nameof(Secondary));
                }
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
                    {
                        _forced[0] = true;
                        Reason = new string[] { "NC", _reason[1] };
                    }
                    else
                        Reason = new string[] { string.Empty, _reason[1] };

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
                    {
                        _forced[1] = true;
                        Reason = new string[] { _reason[0], "NC" };
                    }
                    else
                        Reason = new string[] { _reason[0], string.Empty };

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

        public string[] Reason
        {
            get { return _reason; }
            set
            {
                _reason = value;
                OnPropertyChanged(nameof(Reason));
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
            _primary = "100";
            _secondary = "0.1";
            _forced = new bool[2] { true, true };
            _phase1 = null;
            _phase2 = null;
            _apartmentNumber = string.Empty;
            _breakerNumber = string.Empty;
            _reason = new string[2] { "NC", "NC" };
        }

        #endregion

        #region Methods

        public void Save (StreamWriter sw)
        {
            if (Phase1 == true)
                sw.WriteLine(string.Format("{0},{1},{2}/{3},{4},{5}", ID * 2 - 1, ApartmentNumber, Primary, Secondary, BreakerNumber, Reason[0] + " - " + Notes));
            else
                sw.WriteLine(string.Format("{0},Not Commissionned,Not Commissionned,Not Commissionned,Not Commissionned,Not Commissionned", ID * 2 - 1));

            if (Phase2 == true)
                sw.WriteLine(string.Format("{0},{1},{2}/{3},{4},{5}", ID * 2, ApartmentNumber, Primary, Secondary, BreakerNumber, Reason[1] + " - " + Notes));
            else
                sw.WriteLine(string.Format("{0},Not Commissionned,Not Commissionned,Not Commissionned,Not Commissionned,Not Commissionned", ID * 2));
        }

        #endregion
    }
}
