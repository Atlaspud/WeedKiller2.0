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
    class SprayerRelay
    {
        // Serial Port Constants
        private const int BAUD_RATE = 57600;
        private const Parity PARITY = Parity.None;
        private const int DATA_BITS = 8;
        private const StopBits STOP_BITS = StopBits.One;

        //Serial Port Variables
        private SerialPort serialPort;
        private string port;

        public SprayerRelay(String port)
        {
            this.port = port;
            serialPort = new SerialPort(port, BAUD_RATE, PARITY, DATA_BITS, STOP_BITS);

        }

        public String initConnection()
        {
            closeConnection();

            try
            {
                serialPort.Open();
            }
            catch (IOException ex)
            {
                return ex.Message;
            }

            return "Good";
        }

        public void closeConnection()
        {
            //Close Serial Port
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        public void turnOnSprayers(List<int> sprayersToTurnOn)
        {
            int sprayerSerialNumber = 0;

            foreach (int sprayerIndex in sprayersToTurnOn)
            {
                sprayerSerialNumber += (int)Math.Pow(2, (sprayerIndex - 1));
            }

            transmit(sprayerSerialNumber.ToString() + '\n');
        }

        private void transmit(string stringToWrite)
        {
            try
            {
                serialPort.Write(stringToWrite);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace, "Error", MessageBoxButtons.OK);
            }
        }
    }
}
