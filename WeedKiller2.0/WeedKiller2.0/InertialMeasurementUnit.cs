using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeedKiller2._0
{
    public class InertialMeasurementUnit
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
        public InertialMeasurementUnit(String port)
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

            closeExistingConnection();
            serialPort.Open();
        }

        public void closeExistingConnection()
        {
            //Close Serial Port
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        public double getCurrentYaw()
        {
            return readSerial();
        }

        // readSerail keeps attempting to read the serial until a successful message has been read
        // A successful message is one that contains 35 bytes or more before TERMINATING_BYTE
        // Incomming buffer is discarded after successful read
        // This prevents the buffer from overflowing with old data

        private double readSerial()
        {
            bool successful = false;
            double yaw = 0.0;
            while (!successful)
            {
                try
                {
                    int message = serialPort.ReadByte();
                    string messageString = "";

                    while (message != TERMINATING_BYTE)
                    {
                        messageString += Convert.ToChar(message);
                        message = serialPort.ReadByte();
                    }
                    if (messageString.Length >= 35)
                    {
                        string[] data = messageString.Split(',', '=');
                        yaw = Double.Parse(data[1]) * (Math.PI / 180);
                        successful = true;
                    }
                }
                catch
                {
                    
                }
            }
            serialPort.DiscardInBuffer();
            return yaw;
        }
    }
}
