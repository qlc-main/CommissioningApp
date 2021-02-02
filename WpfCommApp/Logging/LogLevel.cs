using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp
{
    public enum LogLevel
    {
        /// <summary>
        /// Logs everything
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Logs info, warning, errors and critical messages
        /// </summary>
        Info = 2,

        /// <summary>
        /// Logs warning, errors and critical messages
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Logs error and critical messages
        /// </summary>
        Error = 4,

        /// <summary>
        /// Logs critical messages
        /// </summary>
        Critical = 5
    }
}
