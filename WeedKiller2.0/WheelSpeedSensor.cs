using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeedKiller2._0
{
    public class WheelSpeedSensor
    {
        // Serial Port Constants
        private const int TERMINATING_BYTE = 10;
        private const int BAUD_RATE = 57600;
        private const Parity PARITY = Parity.None;
        private const int DATA_BITS = 8;
        private const StopBits STOP_BITS = StopBits.One;

        //Serial Port Variables
        private SerialPort serialPort;
        private string port;

        /*
         * Default Constructor requires COM port name 
         * as argument. Port will initialise and be ready 
         * to read on object creation
         */
        public WheelSpeedSensor(string port)
        {
            //Serial Port Config
            this.port = port;
            initConnection();
        }

        /*
         * Initializes the serial connection
         */
        public void initConnection()
        {

            //Serial Port Config
            serialPort = new SerialPort(port, BAUD_RATE, PARITY, DATA_BITS, STOP_BITS);

            closeConnection();
            serialPort.Open();
        }

        public void closeConnection()
        {
            //Close Serial Port
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        public double getCurrentVelocity()
        {
            return readSerial();
        }

        // readSerial keeps attempting to read the serial until a successful message has been read
        // A successful message is one that contains exactly 6 bytes before TERMINATING_BYTE
        // Incomming buffer is discarded after successful read
        // This prevents the buffer from overflowing with old data

        private double readSerial()
        {
            bool successful = false;
            double finalMessage = 0.0;
            while (!successful)
            {
                try
                {
                    string messageString = "";
                    int message = serialPort.ReadByte();

                    while (message != TERMINATING_BYTE)
                    {
                        messageString += Convert.ToChar(message);
                        message = serialPort.ReadByte();
                    }
                    if (messageString.Length == 6)
                    {
                        finalMessage = Double.Parse(messageString);
                        serialPort.DiscardInBuffer();
                        successful = true;
                    }
                }
                catch
                {
                    // failed read, will try again
                }
            }
            return finalMessage;
        }
    }
}
