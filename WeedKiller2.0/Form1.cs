using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeedKiller2._0
{
    public partial class Form1 : Form
    {
        #region Global Variables

        // Motion constants
        private const float DISTANCE_TRAVELLED_THRESHOLD = 0.256f; // image size which is 25.6cm x 20.48cm
        private const string WSS_SERIAL_PORT = "COM4";
        private const string IMU_SERIAL_PORT = "COM15";

        // Motion Objects
        private Motion motionController;
        private Thread motionThread;

        // Motion Volatiles
        private volatile Position currentPosition;
        private volatile Position lastImageCapturedPosition;
        private volatile bool stop = false;

        // Sprayer constants
        //private const string SPRAYER_RELAY_PORT = "COM4";

        // Vision
        private static readonly uint[] SerialNumbers = new uint[8]
        {
            13421033,
            13421041,
            13421043,
            13421046,
            13421051,
            13421053,
            13421056,
            13421057
        };

        private Dictionary<uint, Camera> cameras;
        private int cameraCount;

        #endregion

        #region Main Initilisation

        public Form1()
        {
            InitializeComponent();
            initialiseMotion();
            initiliseVision();
            //initilaiseSprayer();
            UpdateChart(currentPosition);
            AppendLine("" + currentPosition.getXPosition());
        }

        private void initialiseMotion()
        {
            currentPosition = new Position(0, 0);
            motionController = new Motion(WSS_SERIAL_PORT, IMU_SERIAL_PORT);
        }

        // find number of cameras connected
        // setup cameras and add to array

        private void initiliseVision()
        {
            cameraCount = Camera.GetNumberOfCameras();

            if (cameraCount != 0)
            {
                AppendLine(String.Format("Cameras Found: {0}", cameraCount));
                cameras = new Dictionary<uint, Camera>(cameraCount);
                for (int i = 0; i < cameraCount; i++)
                {
                    cameras.Add(SerialNumbers[i], new Camera(SerialNumbers[i]));
                }
            }
            else
            {
                MessageBox.Show("The ethernet bus manager has failed to find the camera(s). Ensure the camera(s) are connected and correctly configured.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);
            }
        }

        //private void initialiseSprayer()
        //{
        //    sprayer = new Sprayer(SPRAYER_RELAY_PORT);
        //}

        #endregion

        #region Form Events

        private void runBtn_Click(object sender, EventArgs e)
        {
            if (runBtn.Text == "Start")
            {
                runBtn.Text = "Stop";
                startSystem();
            }
            else
            {
                runBtn.Text = "Start";
                stopSystem();
            }
        }

        #endregion

        #region Program Start and Stop Methods

        public void startSystem()
        {
            currentPosition = new Position(0, 0);
            changeView(false);
            stop = false;
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SerialNumbers[i]].start();
            }
            motionThread = new Thread(getMotion);
            motionThread.Start();
        }

        public void stopSystem()
        {
            stop = true;
            changeView(true);
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SerialNumbers[i]].stop();
            }
        }

        public void changeView(Boolean yesOrNo)
        {
            cameraSelectionCombo.Enabled = yesOrNo;
            frameRateCombo.Enabled = yesOrNo;
            thresholdIntensityCombo.Enabled = yesOrNo;
            morphologySizeCombo.Enabled = yesOrNo;
            windowSizeCombo.Enabled = yesOrNo;
            binningRatioCombo.Enabled = yesOrNo;
        }

        #endregion

        #region Threads

        public void getMotion()
        {
            while (!stop)
            {
                currentPosition = motionController.run();
                UpdateChart(currentPosition);
                // Testing Camera
                checkForNewImage();
                //
                Thread.Sleep(5);
            }
        }

        public void checkForNewImage()
        {
            if (lastImageCapturedPosition != null)
            {
                double xDiff = currentPosition.getXPosition() - lastImageCapturedPosition.getXPosition();
                double yDiff = currentPosition.getYPosition() - lastImageCapturedPosition.getYPosition();
                if (DISTANCE_TRAVELLED_THRESHOLD <= Math.Sqrt(Math.Pow(xDiff, 2) + Math.Pow(yDiff, 2)))
                {
                    lastImageCapturedPosition = currentPosition.clone();
                    new Thread(processImage).Start();
                }
            }
            else
            {
                lastImageCapturedPosition = currentPosition.clone();
                new Thread(processImage).Start();
            }
        }

        public void processImage()
        {
            List<Image<Bgr, Byte>> cameraImages = new List<Image<Bgr, byte>>();
            for (int i = 0; i < cameraCount; i++)
            {
                cameraImages.Add(cameras[SerialNumbers[0]].getImage());
                AppendLine(String.Format("Camera {0} Image Captured", SerialNumbers[i]));
            }
            cameraPictureBox.Image = cameraImages[0].Bitmap;
        }

        #endregion

        #region View Invoke Methods

        delegate void AppendLineCallback(string text);

        public void AppendLine(string text)
        {
            
            if (this.logTextBox.InvokeRequired)
            {
                AppendLineCallback d = new AppendLineCallback(AppendLine);
                this.BeginInvoke(d, new object[] { text });
            }
            else
            {
                this.logTextBox.AppendText(text + Environment.NewLine);
            }
        }

        delegate void UpdateChartCallback(Position position);

        private void UpdateChart(Position position)
        {
            if (this.motionChart.InvokeRequired)
            {
                UpdateChartCallback d = new UpdateChartCallback(UpdateChart);
                this.BeginInvoke(d, new object[] {position});
            }
            else
            {
                double x = position.getXPosition();
                double y = position.getYPosition();

                motionChart.ChartAreas[0].AxisX.Maximum = (int)(x + 5);
                motionChart.ChartAreas[0].AxisX.Minimum = (int)(x - 5);
                motionChart.ChartAreas[0].AxisY.Maximum = (int)(y + 5);
                motionChart.ChartAreas[0].AxisY.Minimum = (int)(y - 5);

                motionChart.Series["IMU"].Points.AddXY(x, y);

                //motionChart.Series["Camera"].Points.Clear();
                //motionChart.Series["Sprayer"].Points.Clear();


            }
        }

        #endregion
    }
}
