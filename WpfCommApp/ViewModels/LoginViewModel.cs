
namespace WpfCommApp
{
    public class LoginViewModel : ObservableObject, IPageViewModel
    {
        public LoginViewModel()
        {

        }

        public string EmailAddress { get; set; }
        public string Password { get; set; }

        public string Name
        {
            get
            {
                return "Login Page";
            }
        }

    }
}
