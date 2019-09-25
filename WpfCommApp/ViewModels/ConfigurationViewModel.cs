using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class ConfigurationViewModel : ObservableObject, IPageViewModel
    {
        public string Name
        {
            get
            {
                return "Configuration";
            }
        }
    }
}
