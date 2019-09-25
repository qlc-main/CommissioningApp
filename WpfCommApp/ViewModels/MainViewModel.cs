using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WpfCommApp
{
    
    public class MainViewModel : ObservableObject
    {
        #region Fields

        private ICommand _forwardPage;
        private ICommand _backwardPage;

        private IPageViewModel _current;
        private List<IPageViewModel> _pages;

        private bool _forwardEnabled;

        #endregion

        #region Properties
        public bool ForwardEnabled
        {
            get
            {
                return _forwardEnabled;
            }

            set
            {
                if (_forwardEnabled != value)
                {
                    _forwardEnabled = value;
                    OnPropertyChanged(nameof(ForwardEnabled));
                }
            }
        }
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

        #endregion

        #region Commands

        public ICommand ForwardPage
        {
            get
            {
                if (_forwardPage == null)
                {
                    _forwardPage = new RelayCommand(p => Forward());
                }

                return _forwardPage;
            }
        }

        public ICommand BackwardPage
        {
            get
            {
                if (_backwardPage == null)
                {
                    _backwardPage = new RelayCommand(p => Backward());
                }

                return _backwardPage;
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public MainViewModel()
        {
            Pages.Add(new LoginViewModel());
            Pages.Add(new ConnectViewModel());
            Pages.Add(new ConfigurationViewModel());
            Pages.Add(new CommissioningViewModel());

            CurrentPage = Pages[0];
            ForwardEnabled = true;
        }

        #endregion

        #region Methods
        private void Forward()
        {
            var idx = _pages.IndexOf(_current);
            if (idx < _pages.Count - 1)
            {
                CurrentPage = Pages[idx + 1];
                ForwardEnabled = false;
            }
        }

        private void Backward()
        {
            var idx = _pages.IndexOf(_current);
            if (idx > 0)
            {
                CurrentPage = Pages[idx - 1];
            }
        }
        #endregion
    }
}
