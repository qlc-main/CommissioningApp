using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class Meter : ObservableObject
    {
        #region Fields

        private bool _commissioned;
        private int _disposition;
        private string _floor;
        private bool _fsReturn;
        private string _id;
        private string _location;
        private string _notes;
        private bool _oprComplete;
        private string _plcReason;
        private bool _plcVerified;
        private int _size;
        private bool? _viewing;

        private ObservableCollection<Channel> _channels;

        #endregion

        #region Properties

        public ObservableCollection<Channel> Channels
        {
            get { return _channels; }
            set
            {
                _channels = value;
                OnPropertyChanged(nameof(Channels));
            }
        }

        public bool Commissioned
        {
            get { return _commissioned; }
            set
            {
                if (_commissioned != value)
                    _commissioned = value;
            }
        }

        public int Disposition
        {
            get { return _disposition; }
            set
            {
                if (_disposition != value)
                {
                    _disposition = value;
                    OnPropertyChanged(nameof(Disposition));
                }
            }
        }

        public string Floor
        {
            get { return _floor; }
            set
            {
                if (_floor != value)
                {
                    _floor = value;
                    OnPropertyChanged(nameof(Floor));
                }
            }
        }

        public bool FSReturn
        {
            get { return _fsReturn; }
            set
            {
                if (_fsReturn != value)
                {
                    _fsReturn = value;
                    OnPropertyChanged(nameof(FSReturn));
                }
            }
        }

        public string ID
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(ID));
                }
            }
        }

        public string Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged(nameof(Location));
                }
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

        public bool OprComplete
        {
            get { return _oprComplete; }
            set
            {
                if (_oprComplete != value)
                {
                    _oprComplete = value;
                    OnPropertyChanged(nameof(OprComplete));
                }
            }
        }

        public string PLCReason
        {
            get { return _plcReason; }
            set
            {
                if (_plcReason != value)
                {
                    _plcReason = value;
                    OnPropertyChanged(nameof(PLCReason));
                }
            }
        }

        public bool PLCVerified
        {
            get { return _plcVerified; }
            set
            {
                if (_plcVerified != value)
                {
                    _plcVerified = value;
                    OnPropertyChanged(nameof(PLCVerified));
                }
            }
        }

        public int Size
        {
            get { return _size; }
            set { if (_size != value) { _size = value; OnPropertyChanged(nameof(Size)); } }
        }

        public bool? Viewing
        {
            get { return _viewing; }
            set
            {
                if (_viewing != value)
                {
                    _viewing = value;
                    OnPropertyChanged(nameof(Viewing));
                }
            }
        }

        #endregion

        #region Constructors

        public Meter()
        {
            _disposition = -1;
            _notes = "";
            Channels = new ObservableCollection<Channel>();
        }

        #endregion

        #region Methods

        public void Save(string dir)
        {
            StreamWriter sw = new StreamWriter(string.Format("{0}//{1}.txt", dir, _id));
            sw.WriteLine("S/N,Floor,Location,PLC Verified,Disposition,FS Return,Opr Complete");
            sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", _id, _floor, _location, _plcVerified ? "Yes" : "No", _disposition, _fsReturn ? "1" : "0", _oprComplete ? "1" : "0"));
            sw.WriteLine("CT,Serial,Apartment,C/B#,CT Type,Primary,Secondary,Multiplier,Commissioned,Forced,Reason,Notes");
            foreach (Channel c in Channels)
                c.Save(sw);
            sw.Close();
        }

        #endregion
    }
}
