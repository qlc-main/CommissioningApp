using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class ScreenComplete
    {
        #region Fields
        private string _command;
        #endregion

        #region Properties
        public string Command
        {
            get
            {
                return _command;
            }
        }
        #endregion

        #region Constructors
        public ScreenComplete()
            : this("")
        {
        }

        public ScreenComplete(string command)
        {
            _command = command;
        }
        #endregion

    }
}
