using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;

namespace WpfCommApp
{
    public class SerialComm
    {
        #region Fields
        private SerialPort _serial;
        private bool echo = true;
        private bool carriageReturn = false;
        private string serialBuffer;
        private string serialNo;
        private string _com;

        private static List<string> pwds = new List<string>
                { "-5lEvElbAl", "-3Super3", "-4E9Kgxk20",
                "-5op%%&$6", "-4OP!3c@t4", "-4u5574",  "-3helix3", "-74321fdsa"};
        private static Regex prompt = new Regex(@"(S|P|M|E|CIP)[:#>]");
        private static Regex endPrompt = new Regex(@"(S|P|M|E|CIP)[:#>]$");

        #endregion

        #region Properties
        public bool IsOpen
        {
            get { return _serial == null ? false: _serial.IsOpen; }
        }

        public string COM { get { return _com; } }

        #endregion

        #region Constructor
        public SerialComm()
        {

        }
        #endregion

        #region Methods

        public string SetupSerial(string com)
        {
            _com = com;
            _serial = new SerialPort(com, 19200);
            _serial.Open();

            Login(true);

            return serialNo;
        }

        public bool Login(bool initial)
        {
            int attempts = 0;
            while (true)
            {
                // Modify way that we send data
                if (attempts > 1)
                {
                    carriageReturn = true;
                    Thread.Sleep(1000);
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    Thread.Sleep(1500);
                }
                else if (attempts == 10)
                {
                    Console.WriteLine("Experiencing an issue with communicating with the meter head.");
                    return false;
                }

                WriteToSerial("attn -D");
                Thread.Sleep(1000);
                if (_serial.BytesToRead == 22)
                {
                    ReadBuffer();
                    if (serialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[0].Length == 8)
                        break;
                }

                _serial.DiscardInBuffer();
                attempts++;
            }

            Console.WriteLine(serialBuffer);
            string[] split = serialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            if (serialBuffer.Contains("attn"))
                serialNo = split[2];
            else
                serialNo = split[0];

            attempts = 0;
            while (attempts < 10)
            {
                WriteToSerial(String.Format("attn -S{0} {1}", serialNo, pwds[attempts % 8]));
                ReadBuffer(!initial);
                Console.Write(serialBuffer + " ");
                if (prompt.IsMatch(serialBuffer))
                    return true;
                Console.Write("\r\n");

                attempts++;
            }

            if (attempts == 50)
                Console.WriteLine("Unable to successfully login, exitting program now!");

            return false;
        }

        public string PD()
        {
            serialBuffer = "";
            WriteToSerial("mscan -Gp", true);
            int failed = 0;
            while (_serial.BytesToRead == 0)
            {
                if (++failed % 10 == 0)
                {
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    if (Login(false))
                        WriteToSerial("mscan -Gp", true);
                }

                Thread.Sleep(500);
            }
            ReadBuffer(true);
            Console.Write(serialBuffer + ' ');
            return serialBuffer;
        }

        public void Close()
        {
            _serial.Close();
        }

        public string GetVersion()
        {
            serialBuffer = "";
            WriteToSerial("ver", true);
            while (_serial.BytesToRead == 0) { }
            ReadBuffer(true);
            Console.Write(serialBuffer + ' ');
            return serialBuffer;
        }

        public string GetChildSerial()
        {
            string buffer = PD();
            string ret = "";
            foreach(string line in buffer.Split(new char[] { '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] entries = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length != 10 || line.Contains("$lot"))
                    continue;

                if (!ret.Contains(entries[1]))
                    ret += entries[1] + ",";
            }

            return ret.TrimEnd(',');
        }

        #region Serial Communication Functions

        private void WriteToSerial(string data, bool echo = true)
        {
            if (echo)
                Console.WriteLine(data);
            if (!carriageReturn)
                _serial.WriteLine(data);
            else
                _serial.Write(data + "\r\n");
        }

        private void ReadBuffer(bool cmd = false)
        {
            Thread.Sleep(1000);
            serialBuffer = _serial.ReadExisting();
            int counter = 0;
            while (cmd && !endPrompt.IsMatch(serialBuffer) && counter++ < 10)
            {
                serialBuffer += _serial.ReadExisting();
                Thread.Sleep(500);
            }
        }

        private void ReadLine(bool wait = false)
        {
            if (wait)
                Thread.Sleep(1000);
            serialBuffer = _serial.ReadLine();
        }

        #endregion

        #endregion
    }
}
