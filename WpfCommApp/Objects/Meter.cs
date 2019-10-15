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
        private string _id;
        private string _notes;
        private int _size;
        private bool? _viewing;
        private ObservableCollection<Channel> _channels;

        #endregion

        #region Properties
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
            _notes = "";
            Channels = new ObservableCollection<Channel>();
        }

        #endregion

        #region Methods

        public void Save(string dir)
        {
            StreamWriter sw = new StreamWriter(string.Format("{0}//{1}.txt", dir, _id));
            sw.WriteLine(string.Format("S/N: {0}\n\n", _id));
            sw.WriteLine("Channel,Unit,CT Ratio,C/B#,Notes");
            foreach (Channel c in Channels)
                c.Save(sw);
            sw.Close();
        }

        #endregion
    }
}
