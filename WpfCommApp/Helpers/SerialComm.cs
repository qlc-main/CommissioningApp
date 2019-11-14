﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;

namespace WpfCommApp
{
    public class SerialComm
    {
        #region Fields

        private bool carriageReturn = false;
        private static Regex endPrompt = new Regex(@"(S|P|M|E|CIP)[:#>]$");
        private static Regex prompt = new Regex(@"(S|P|M|E|CIP)[:#>]");
        private static List<string> pwds = new List<string>
                { "-5lEvElbAl", "-3Super3", "-4E9Kgxk20",
                "-5op%%&$6", "-4OP!3c@t4", "-4u5574",  "-3helix3", "-74321fdsa"};
        private string serialBuffer;

        private SerialPort _serial;
        private CancellationToken _token;

        #endregion

        #region Properties

        /// <summary>
        /// Property to inform user if serial port is currently open / in use
        /// </summary>
        public bool IsOpen
        {
            get { return _serial == null ? false : _serial.IsOpen; }
        }

        /// <summary>
        /// The string representation of the com port that this instance is connected
        /// </summary>
        public string COM { get; private set; }

        /// <summary>
        /// Serial Number of the meter that this instance is connected
        /// </summary>
        public string SerialNo { get; private set; }

        #endregion

        #region Constructor

        public SerialComm()
        {
            _token = new CancellationToken();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens serial connection and attempts to log into meter
        /// </summary>
        /// <param name="com">Port that this instance is attempting to connect</param>
        /// <returns>Serial Number of meter if there was a successful login</returns>
        public string SetupSerial(string com)
        {
            COM = com;
            _serial = new SerialPort(com, 19200);
            _serial.Open();

            Login(true);

            return SerialNo;
        }

        /// <summary>
        /// Logins into the meter head for a MC5N meter using pre supplied passwords
        /// </summary>
        /// <param name="initial">boolean indicating if this is the first time attempting 
        /// to login or if attempting to login to meter after a previous successful login</param>
        /// <returns>Boolean indicating success of login attempt</returns>
        public bool Login(bool initial)
        {
            // Makes 10 attempts to get serial number of currently connected meter
            int attempts = 0;
            while (true)
            {
                // Modifies communication method if this is first login attempt
                // and 2 previous attempts at retrieving serial number failed
                if (initial && attempts > 1)
                {
                    carriageReturn = true;
                    Thread.Sleep(500);
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    Thread.Sleep(1000);
                }
                // Exits function 10 unsuccessful attempts
                else if (attempts == 10)
                {
                    Console.WriteLine("Experiencing an issue communicating with meter head.");
                    return false;
                }

                // Writes command to serial to request meter serial number
                WriteToSerial("attn -D");

                // Checks if meter serial number was successfully received, if so, 
                // read response and break out of this execution loop
                if (_serial.BytesToRead == 22)
                {
                    ReadBuffer();
                    if (serialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[0].Length == 8)
                        break;
                }

                // If specified number of bytes was not received, throw away anything that was received
                // increment counter and try again
                _serial.DiscardInBuffer();
                attempts++;

                // Exits function if calling function has instructed this execution to terminate
                if (_token.IsCancellationRequested)
                    return false;
            }

            // Writes buffer to the command line and retrieves serial number
            Console.WriteLine(serialBuffer);
            string[] split = serialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            if (serialBuffer.Contains("attn"))
                SerialNo = split[2];
            else
                SerialNo = split[0];

            // Attempts to use all of the stored passwords once to login into the meter
            // Waits for command prompt if this is a successive attempt to login to a meter
            // Exits function by returning true if login was successful
            foreach(string s in pwds)
            {
                WriteToSerial(String.Format("attn -S{0} {1}", SerialNo, s));
                ReadBuffer(!initial);
                Console.Write(serialBuffer + " ");
                if (prompt.IsMatch(serialBuffer))
                    return true;
                Console.Write("\r\n");
            }

            // Login was unsuccessful if this code is reached so return false
            return false;
        }

        /// <summary>
        /// Retrieves Phase Diagnostic for MC5N meter
        /// </summary>
        /// <param name="token">Cancellation token that is polled to see if the user wishes to exit this function</param>
        /// <returns>Phase Diagnostic for meter in string format</returns>
        public string PhaseDiagnostic(CancellationToken token = new CancellationToken())
        {
            // Initiates phase diagnostic command and waits for response
            _token = token;
            serialBuffer = "";
            WriteToSerial("mscan -Gp", true);
            int failed = 0;
            while (_serial.BytesToRead == 0)
            {
                // If response is not received after about 3 seconds then assume
                // that probe was disconnected clear buffers, exit function and
                // try again
                if (++failed % 10 == 0)
                {
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    return "";
                }

                // Cancels current function execution because user requested to end execution
                if (_token.IsCancellationRequested)
                    return "";

                Thread.Sleep(250);
            }

            // If Phase Diagnostic command is successful, store the contents within the buffer and then return buffer
            // for further use
            ReadBuffer(true);
            Console.Write(serialBuffer + ' ');
            string temp = serialBuffer;

            // Reads serial number after each PhaseDiagnostic read to determine if
            // still attached to the same meter
            WriteToSerial("attn -d");
            ReadBuffer(true);
            string newSerial = SerialNo;
            if (!string.IsNullOrEmpty(serialBuffer) && serialBuffer.Contains("attn -d"))
                newSerial = serialBuffer.Split(new char[1] { '\n' })[3].Split(new char[0])[0];
            if (SerialNo != newSerial)
            {
                SerialNo = newSerial;
                return "";
            }
            else
                return temp;
        }

        /// <summary>
        /// Closes Serial Port
        /// </summary>
        public void Close()
        {
            _serial.Close();
        }

        /// <summary>
        /// Gets the Version of the meter that is currently connected
        /// </summary>
        /// <returns>String with the result of the version command</returns>
        public string GetVersion()
        {
            // Issues version command, waits for the data to be present
            // Reads buffer until command line prompt received
            // Returns result to calling function
            serialBuffer = "";
            WriteToSerial("ver", true);
            while (_serial.BytesToRead == 0) { }
            ReadBuffer(true);
            Console.Write(serialBuffer + ' ');
            return serialBuffer;
        }

        /// <summary>
        /// Retrieves child serial numbers for the MC5N meters
        /// </summary>
        /// <returns>Comma separated list of ordered child serial numbers</returns>
        public string GetChildSerial()
        {
            // Issues Phase Diagnostic call (child serial numbers are returned in this call) 
            // Parses the returned string and creates a new comma separated string of the serial numbers
            string buffer = PhaseDiagnostic();
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

        // Low level functions that interact directly with serial object
        #region Serial Communication Functions

        /// <summary>
        /// Writes data to serial object
        /// </summary>
        /// <param name="data">Data in string format to be sent</param>
        /// <param name="echo">Boolean indicating whether the data should be echoed to user interface</param>
        private void WriteToSerial(string data, bool echo = true)
        {
            // Echos data and then writes data to serial depending on carriage return value
            // some meters need a Carriage Return and Line Feed, others are ok with just 
            // line feed
            if (echo)
                Console.WriteLine(data);
            if (!carriageReturn)
                _serial.WriteLine(data);
            else
                _serial.Write(data + "\r\n");
            Thread.Sleep(500);
        }

        /// <summary>
        /// Reads the contents of the buffer from the serial object
        /// </summary>
        /// <param name="cmd">Boolean indicating whether or not to continue reading until command prompt is received</param>
        private void ReadBuffer(bool cmd = false)
        {
            // Stores current buffer contents and continues trying to extract data until command prompt received or 
            // max number of attempts is reached
            serialBuffer = _serial.ReadExisting();
            int counter = 0;
            while (cmd && !endPrompt.IsMatch(serialBuffer) && counter++ < 10)
            {
                serialBuffer += _serial.ReadExisting();
                Thread.Sleep(500);
            }
        }

        #endregion

        #endregion
    }
}
