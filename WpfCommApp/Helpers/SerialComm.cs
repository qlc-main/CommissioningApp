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

        public SerialComm()
        {

        }

        #region Methods
        public string SetupSerial(string com)
        {
            _com = com;
            _serial = new SerialPort(com, 19200);
            _serial.Open();

            Login();

            return serialNo;
        }

        public void Login()
        {
            int attempts = 0;
            while (true)
            {
                // Modify way that we send data
                if (attempts > 1)
                {
                    carriageReturn = true;
                    Thread.Sleep(2000);
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    //WriteToSerial("");
                    Thread.Sleep(2000);
                }
                else if (attempts == 10)
                {
                    Console.WriteLine("Experiencing an issue with communicating with the meter head.");
                    Environment.Exit(-1);
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

            //ReadBuffer();
            Console.WriteLine(serialBuffer);
            string[] split = serialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            if (serialBuffer.Contains("attn"))
                serialNo = split[2];
            else
                serialNo = split[0];

            attempts = 0;
            while (attempts < 50)
            {
                WriteToSerial(String.Format("attn -S{0} {1}", serialNo, pwds[attempts % 8]));
                ReadBuffer();
                Console.Write(serialBuffer + " ");
                if (prompt.IsMatch(serialBuffer))
                    break;
                Console.Write("\r\n");

                attempts++;
            }

            if (attempts == 50)
            {
                Console.WriteLine("Unable to successfully login, exitting program now!");
                Environment.Exit(-2);
            }
        }

        public string PD()
        {
            serialBuffer = "";
            WriteToSerial("mscan -Gp", true);
            while (_serial.BytesToRead == 0) { }
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
            //byte[] buf2 = new byte[sp.BytesToRead];
            //sp.Read(buf2, 0, sp.BytesToRead);
            //serialBuffer = Encoding.UTF8.GetString(buf2, 0, buf2.Length).Trim();
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
