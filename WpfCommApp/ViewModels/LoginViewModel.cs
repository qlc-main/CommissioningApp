
namespace WpfCommApp
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        #region Fields
        private bool _completed;
        private int _idx;

        #endregion

        #region Constructors

        public LoginViewModel()
        {

        }

        #endregion

        #region Properties

        public string EmailAddress { get; set; }
        public string Password { get; set; }

        public bool Completed
        {
            get
            {
                return _completed;
            }
        }

        public int Idx
        {
            get { return _idx; }
            set { if (_idx != value) _idx = value; }
        }

        #endregion

    }
}
