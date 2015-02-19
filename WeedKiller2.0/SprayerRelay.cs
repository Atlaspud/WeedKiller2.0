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
        private SerialPort serialPort;
        private string port;
        private int baudRate;
        private Parity parity;
        private int dataBits;
        private StopBits stopBits;

        public SprayerRelay(String port)
        {
            //Serial Port Config
            this.port = port;
            baudRate = 57600;
            parity = Parity.None;
            dataBits = 8;
            stopBits = StopBits.One;

            //Serial Port Config
            serialPort = new SerialPort(port, baudRate, parity, dataBits, stopBits);

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




        //private void receive(byte read_bytes)
        //{
        //    byte x;

        //    for (x = 0; x < read_bytes; x++)       // this will call the read function for the passed number times, 
        //    {                                      // this way it ensures each byte has been correctly recieved while
        //        try                                // still using timeouts
        //        {
        //            serialPort.Read(serBuf, x, 1);     // retrieves 1 byte at a time and places in SerBuf at position x
        //        }
        //        catch (Exception)                   // timeout or other error occured, set lost comms indicator
        //        {
        //            serBuf[0] = 255;
        //            MessageBox.Show("read fail");
        //        }
        //    }
        //}
    }
}
