using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public class Serial
    {
        #region Fields
        private bool _used;
        private bool _default;
        private string _name;

        #endregion

        #region Properties

        public bool Used
        {
            get { return _used; }
            set { if (_used != value) _used = value; }
        }

        public bool Default
        {
            get { return _default; }
            set { if (_default != value) _default = value; }
        }

        public string Name
        {
            get { return _name; }
            set { if (_name != value) _name = value; }
        }

        #endregion

        #region Constructors
        public Serial(string name, bool def)
        {
            _name = name;
            _used = false;
            _default = def;
        }

        #endregion
    }
}
