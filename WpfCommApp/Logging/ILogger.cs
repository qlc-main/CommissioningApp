using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCommApp.Logging
{
    /// <summary>
    /// Logger handles log messages from a <see cref="ILogFactory"/>
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Handles the log message being passed in
        /// </summary>
        /// <param name="message">message to log</param>
        /// <param name="level">level of message</param>
        void Log(string message, LogLevel level);
    }
}
