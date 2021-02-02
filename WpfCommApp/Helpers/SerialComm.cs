using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfCommApp.Helpers;

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
        private SerialPort _serial;

        // private int _count;

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

        public bool NewFirmware { get; set; }

        public string SerialBuffer { get; private set; }

        /// <summary>
        /// Serial Number of the meter that this instance is connected
        /// </summary>
        public string SerialNo { get; private set; }

        #endregion

        #region Constructor

        public SerialComm()
        {
            // _count = 0;
            NewFirmware = false;
        }

        #endregion

        #region Methods

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
        public async Task<string> GetVersion()
        {
            // Issues version command, waits for the data to be present
            // Reads buffer until command line prompt received
            // Returns result to calling function
            await WriteToSerial("ver");
            await ReadBuffer(true);
#if DEBUG            
            Console.Write(SerialBuffer + ' ');
#endif
            return SerialBuffer;
        }

        /// <summary>
        /// Retrieves child serial numbers for the MC5N meters
        /// </summary>
        /// <returns>Comma separated list of ordered child serial numbers</returns>
        public async Task<string> GetChildSerial()
        {
            // Initiates phase diagnostic command and waits for response
            await WriteToSerial("mscan -Gp");

            // If Phase Diagnostic command is successful, store the contents within the buffer and then return buffer
            // for further use
            await ReadBuffer(true);
#if DEBUG
            Console.Write(SerialBuffer + ' ');
#endif
            string buffer = SerialBuffer;
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

        /// <summary>
        /// Logins into the meter head for a MC5N meter using pre supplied passwords
        /// </summary>
        /// <returns>Boolean indicating success of login attempt</returns>
        public async Task<bool> Login()
        {
            // Makes 10 attempts to get serial number of currently connected meter
            int attempts = 0;
            carriageReturn = false;
            while (true)
            {
                // Modifies communication method if this is first login attempt
                // and 2 previous attempts at retrieving serial number failed
                if (attempts == 2)
                    carriageReturn = true;

                // Exits function 10 unsuccessful attempts
                else if (attempts == 10)
                {
#if DEBUG
                    Console.WriteLine("Experiencing an issue communicating with meter head.");
#endif
                    return false;
                }

                // Writes command to serial to request meter serial number
                await WriteToSerial("attn -D");
                if (await ReadBuffer())
                    break;

                // If specified number of bytes was not received, throw away anything that was received
                // increment counter and try again
                attempts++;
            }

#if DEBUG
            // Writes buffer to the command line and retrieves serial number
            Console.WriteLine(SerialBuffer);
#endif
            string[] split = SerialBuffer.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            if (SerialBuffer.Contains("attn"))
                SerialNo = split[2];
            else
                SerialNo = split[0];

            // Attempts to use all of the stored passwords once to login into the meter
            // Waits for command prompt if this is a successive attempt to login to a meter
            // Exits function by returning true if login was successful
            foreach(string s in pwds)
            {
                await WriteToSerial(String.Format("attn -S{0} {1}", SerialNo, s));
                await ReadBuffer(true);
#if DEBUG
                Console.Write(SerialBuffer + " ");
#endif
                if (prompt.IsMatch(SerialBuffer))
                    return true;
#if DEBUG
                Console.Write("\r\n");
#endif
            }

            // Login was unsuccessful if this code is reached so return false
            return false;
        }

        /// <summary>
        /// Retrieves Phase Diagnostic for MC5N meter
        /// </summary>
        /// <param name="token">Cancellation token that is polled to see if the user wishes to exit this function</param>
        /// <returns>Phase Diagnostic for meter in string format</returns>
        public async Task<string> PhaseDiagnostic(CancellationToken token)
        {
            // Reads serial number after each PhaseDiagnostic read to determine if
            // still attached to the same meter
            await WriteToSerial("attn -d");
            await ReadBuffer(true);
            string newSerial = "";
            if (string.IsNullOrEmpty(SerialBuffer))
            {
                string oldSerialNo = SerialNo;
                if (await Login())
                {
                    if (oldSerialNo != SerialNo)
                        return "switch";
                    else
                        newSerial = SerialNo;
                }
                else
                    return "failed";
            }
            else
            {
                foreach (string line in SerialBuffer.Split(new char[1] { '\n' }))
                {
                    if (Regex.IsMatch(line.TrimEnd('\r'), @"^[0-9 ]+$"))
                    {
                        newSerial = line.Split(new char[0])[0];
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(newSerial))
            {
                Console.WriteLine("Unable to get meter serial number");
                return "failings";
            }

            // New Serial number so propagate message up to switch meter
            else if (SerialNo != newSerial)
            {
                SerialNo = newSerial;
                return "switch";
            }

            // Initiates phase diagnostic command and waits for response
            if (NewFirmware)
            {
                string totalBuffer = "";
                foreach (int i in System.Linq.Enumerable.Range(0, 12))
                {
                    await WriteToSerial(String.Format("md -p -#2 -@{0}", i + 32));
                    if (!(await ReadBuffer(true, token)))
                    {
                        if (string.IsNullOrEmpty(SerialBuffer))
                            return "failed";
                        else
                            return "";
                    }

                    totalBuffer += SerialBuffer;
                }

                // Set value of buffer only if correct number of lines were returned
                if (totalBuffer.Count(c => c == '\n') == 72)
                    SerialBuffer = totalBuffer;
                else
                {
                    Console.WriteLine("Corrupt data received from serial port");
                    return "invalid";
                }
            }
            else
            {
                await WriteToSerial("mscan -Gp");

                // Store serial contents in buffer
                if (!(await ReadBuffer(true, token)))
                {
                    if (string.IsNullOrEmpty(SerialBuffer))
                        return "failed";
                    else
                        return "";
                }
            }
#if DEBUG
            Console.Write(SerialBuffer + ' ');
#endif
            return SerialBuffer;
        }

        /// <summary>
        /// Opens serial connection and attempts to log into meter
        /// </summary>
        /// <param name="com">Port that this instance is attempting to connect</param>
        /// <returns>Serial Number of meter if there was a successful login</returns>
        public async Task<string> SetupSerial(string com)
        {
            COM = com;
            _serial = new SerialPort(com, 19200);
            try
            {
                _serial.Open();
                await Login();
            }
            catch (UnauthorizedAccessException)
            {
                SerialNo = "";
                Globals.Logger.LogInformation($"Port {com} is already open");
            }

            return SerialNo;
        }

        // Low level functions that interact directly with serial object
        #region Serial Communication Functions

        /// <summary>
        /// Writes data to serial object
        /// </summary>
        /// <param name="data">Data in string format to be sent</param>
        /// <param name="echo">Boolean indicating whether the data should be echoed to user interface</param>
        private async Task WriteToSerial(string data)
        {
            // Echos data and then writes data to serial depending on carriage return value
            // some meters need a Carriage Return and Line Feed, others are ok with just 
            // line feed
#if DEBUG
            Console.WriteLine(data);
#endif
            _serial.DiscardInBuffer();
            _serial.DiscardOutBuffer();
            if (!carriageReturn)
                _serial.WriteLine(data);
            else
                _serial.Write(data + "\r\n");
            await Task.Delay(500);
        }

        /// <summary>
        /// Reads the contents of the buffer from the serial object
        /// </summary>
        /// <param name="cmd">Boolean indicating whether or not to continue reading until command prompt is received</param>
        /// <param name="token"></param>
        /// <returns>Boolean, whether the read attempt was successful or not</returns>
        private async Task<bool> ReadBuffer(bool cmd = false, CancellationToken token = default)
        {
            int counter = 0;
            SerialBuffer = string.Empty;
            while ((!cmd && _serial.BytesToRead == 22) || (cmd && counter < 1000 && ((_serial.BytesToRead == 0 && string.IsNullOrEmpty(SerialBuffer)) ||
                !endPrompt.IsMatch(SerialBuffer))))
            {
                if (_serial.BytesToRead > 0)
                {
                    SerialBuffer += _serial.ReadExisting();
                    if (!cmd)
                        break;
                } else if (string.IsNullOrEmpty(SerialBuffer) && counter > 300)
                    break;

                // Cancels current function execution because user requested to end execution
                if (token.IsCancellationRequested)
                    break;

                await Task.Delay(5);
                counter++;
            }

            if ((string.IsNullOrEmpty(SerialBuffer) && (!cmd || counter > 300)) || token.IsCancellationRequested)
                return false;

            return true;
        }

        #endregion

        #endregion
    }
}
