using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WpfCommApp.Managers;

namespace WpfCommApp.Helpers
{
    public static class Globals
    {
        public static string LogFile = $"Logs\\{DateTime.Now.ToString("yyyyMMddhhmmss")}.log";
        public static Logger Logger = new Logger();
        public static Managers.File File = new Managers.File();
        public static Tasker Tasker = new Tasker();
    }
}
