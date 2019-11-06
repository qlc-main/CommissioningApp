using System.Collections.Generic;

namespace WpfCommApp
{
    public class ContentTab : ObservableObject
    {
        #region Fields
        private int _idx;
        private bool _visible;
        private IPageViewModel _current;
        private List<IPageViewModel> _pages;
        private string _meterSerialNo;

        #endregion

        #region Properties

        public string Name { get { return CurrentPage.Name + (_meterSerialNo == "" ? "" : " (" + _meterSerialNo + ")");  }  }

        public int IDX { get { return _idx; } }

        public List<IPageViewModel> Pages
        {
            get
            {
                if (_pages == null)
                    _pages = new List<IPageViewModel>();

                return _pages;
            }
        }

        public IPageViewModel CurrentPage
        {
            get
            {
                return _current;
            }
            set
            {
                if (_current != value)
                {
                    _current = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public bool Visible
        {
            get { return _visible; }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    OnPropertyChanged(nameof(Visible));
                }
            }
        }

        public string MeterSerialNo
        {
            get { return _meterSerialNo; }
        }

        #endregion

        #region Constructor

        public ContentTab(int idx)
        {
            _idx = idx;
            _meterSerialNo = "";
            Pages.Add(new ConnectViewModel());
            Pages.Add(new LoginViewModel());
            CurrentPage = Pages[0];
            Visible = true;
        }

        public ContentTab(int idx, int serialIdx, string serial)
        {
            _idx = idx;
            _meterSerialNo = serial;

            Pages.Add(new ConfigurationViewModel(serial));
            Pages.Add(new CommissioningViewModel(serial, serialIdx));
            Pages.Add(new ReviewViewModel(serial));
            CurrentPage = Pages[0];
            Visible = true;
        }

        #endregion

        #region Methods

        #endregion
    }
}
