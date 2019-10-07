using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        #endregion

        #region Constructors
        public Meter()
        {
            _notes = "";
            Channels = new ObservableCollection<Channel>();
        }

        #endregion

        #region Methods

        #endregion
    }
}
