using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            //List<Meter> meters = Application.Current.Properties["meters"] as List<Meter>;

            //// Delete this section once finished testing
            //meters[0].Channels = new ObservableCollection<Channel>();
            //for (int i = 0; i < 12; i++)
            //    meters[0].Channels.Add(new Channel(i + 1));
            //meters[0].Channels[0].Phase1 = true;
            //meters[0].Channels[0].Forced[0] = false;
            //Application.Current.Properties["meters"] = meters;
            //// Delete to here

            Pages.Add(new ConnectViewModel());
            Pages.Add(new ConfigurationViewModel());
            Pages.Add(new CommissioningViewModel());
            Pages.Add(new ReviewViewModel());
            Pages.Add(new LoginViewModel());

            CurrentPage = Pages[0];
        }

        #endregion

        #region Methods
        private void Forward()
        {
            var idx = _pages.IndexOf(_current);
            if (idx < _pages.Count - 1)
            {
                CurrentPage = Pages[idx + 1];
                CurrentPage.Idx = Pages[idx].Idx;
                if (CurrentPage.Completed)
                    ForwardEnabled = true;
                else
                    ForwardEnabled = false;
            }
        }

        private void Backward()
        {
            var idx = _pages.IndexOf(_current);
            if (idx > 0)
            {
                CurrentPage = Pages[idx - 1];
                CurrentPage.Idx = Pages[idx].Idx;
                ForwardEnabled = true;
            }
        }
        #endregion
    }
}
