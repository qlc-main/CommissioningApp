using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using WpfCommApp.Helpers;
using WpfCommApp.Logging;

namespace WpfCommApp.Managers
{
    /// <summary>
    /// Standard log factory for WpfCommApp
    /// Logs details to the Debug, Console, Trace and log files
    /// </summary>
    public class Logger
    {

        #region Protected Properties

        /// <summary>
        /// List of loggers in this factory
        /// </summary>
        protected List<ILogger> mLoggers = new List<ILogger>();

        /// <summary>
        /// Lock object to keep factory thread safe
        /// </summary>
        protected object mLoggerLock = new object();

        #endregion

        #region Public Properties

        public LogOutputLevel LogOutputLevel { get; set; }

        /// <summary>
        /// If true, includes the origin of where the log message was generated
        /// </summary>
        public bool IncludeLogOriginDetails { get; set; } = true;

        #endregion

        #region Public Events

        public event Action<(string Message, LogLevel Level)> NewLog = (details) => { };

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Logger(LogOutputLevel level = LogOutputLevel.Info)
        {
            LogOutputLevel = level;
            AddLogger(new FileLogger(Globals.LogFile));
        }
        
        #endregion

        #region Public Methods

        public void AddLogger(ILogger logger)
        {
            lock(mLoggerLock)
            {
                if (!mLoggers.Contains(logger))
                    mLoggers.Add(logger);
            }
        }

        public void RemoveLogger(ILogger logger)
        {
            lock (mLoggerLock)
            {
                if (mLoggers.Contains(logger))
                    mLoggers.Remove(logger);
            }
        }

        public void LogDebug(string message, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Debug, origin, filePath, lineNumber);
        }

        public void LogInformation(string message, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Info, origin, filePath, lineNumber);
        }

        public void LogWarning(string message, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Warning, origin, filePath, lineNumber);
        }

        public void LogError(string message, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Error, origin, filePath, lineNumber);
        }

        public void LogCritical(string message, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Critical, origin, filePath, lineNumber);
        }

        #endregion

        #region Private Methods

        private void Log(string message, LogLevel level = LogLevel.Info, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            // Do not log this message if it is below the loggers level
            if ((int)level < (int)LogOutputLevel)
                return;

            // Modify the logged message
            if (IncludeLogOriginDetails)
                message = $"[{Path.GetFileName(filePath)}:{origin}:{lineNumber}] {message}";

            // Log to all loggers
            mLoggers.ForEach(logger => logger.Log(message, level));

            // Inform listeners 
            NewLog.Invoke((message, level));
        }

        #endregion
    }
}
