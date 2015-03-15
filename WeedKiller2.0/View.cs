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
    public partial class View : Form
    {
        #region Global Variables

        // Motion constants
        private const float DISTANCE_TRAVELLED_THRESHOLD = 0.13f; // half image size which is 25.6cm x 20.48cm
        private const string WSS_SERIAL_PORT = "COM9";
        private const string IMU_SERIAL_PORT = "COM6";

        // Motion Objects
        private Motion motionController;
        private Thread motionThread;

        // Motion Volatiles
        private volatile Position currentPosition;
        private volatile Position lastImageCapturedPosition;
        private volatile bool stop = false;

        // Sprayer constants
        private const string SPRAYER_RELAY_PORT = "COM4";

        // Sprayer Objects
        Position[] sprayerPositions;
        Sprayer sprayer;

        // Vision
        private Dictionary<uint, Camera> cameras;
        private int cameraCount;
        Position[] cameraPositions = new Position[8];
        public static readonly uint[] SerialNumbers = new uint[8]
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

        #endregion

        #region Main Initilisation

        public View()
        {
            InitializeComponent();
            initialiseSystem();
        }

        private Boolean initialiseMotion()
        {
            try
            {
                currentPosition = new Position(0, 0);
                motionController = new Motion(WSS_SERIAL_PORT, IMU_SERIAL_PORT);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace,"Error",MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        // find number of cameras connected
        // setup cameras and add to array

        private Boolean initialiseVision()
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

                return true;
            }
            else
            {
                return false;
            }
        }

        private Boolean initialiseSprayer()
        {
            try
            {
                sprayer = new Sprayer(SPRAYER_RELAY_PORT);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Boolean initialiseSystem()
        {
            if (!initialiseVision())
            {
                MessageBox.Show("Failed initialise Cameras, Check Cameras and try again");
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseMotion())
            {
                MessageBox.Show("Failed initialise Motion, Check connections and try again");
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseSprayer())
            {
                MessageBox.Show("Failed initialise Sprayers, Check connections and try again");
                runBtn.Text = "Reinitialise";
                return false;
            }
            for (int i = 0; i < 8; i++)
            {
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SerialNumbers[i], currentPosition);
            }
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
            UpdateChart(currentPosition);
            return true;
        }

        #endregion

        #region Form Events

        private void runBtn_Click(object sender, EventArgs e)
        {
            if (runBtn.Text == "Start")
            {
                runBtn.Text = "Stop";
                startSystem();
            }
            else if (runBtn.Text == "Stop")
            {
                runBtn.Text = "Start";
                stopSystem();
            }
            else
            {
                if (initialiseSystem())
                {
                    runBtn.Text = "Start";
                }
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
            sprayer.startSensors();
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
            sprayer.stopSensors();
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
                updateCameras();
                // Check Sprayer
                updateSprayers();
                Thread.Sleep(20);
            }
        }

        public void updateCameras()
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

            for (int i = 0; i < 8; i++)
            {
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SerialNumbers[i], currentPosition);
            }
        }

        public void processImage()
        {
            // Aquire Images
            List<Image<Bgr, Byte>> cameraImages = new List<Image<Bgr, Byte>>();
            for (int i = 0; i < cameraCount; i++)
            {
                cameraImages.Add(cameras[SerialNumbers[i]].getImage());
                AppendLine(String.Format("Camera {0} Image Captured", SerialNumbers[i]));
            }
            cameraPictureBox.Image = cameraImages[0].Bitmap;

            // Process Images
            List<Image<Gray, Byte>> processedImages = new List<Image<Gray, Byte>>();
            for (int i = 0; i < cameraCount; i++)
            {
                Image<Gray, Byte> sampleImage = ImageProcessor.thresholdImage(cameraImages[i]);
                sampleImage = ImageProcessor.morphology(sampleImage);
                List<int[]> windowLoactionArray = ImageProcessor.findWindows(sampleImage);
                if (windowLoactionArray.Count != 0)
                {
                    AppendLine(String.Format("Target Found at Camera: {0}", i + 1));
                    Target target = new Target(lastImageCapturedPosition, SerialNumbers[i]);
                    sprayer.addTarget(target);
                    this.BeginInvoke(new Action(() =>
                        motionChart.Series["Target"].Points.AddXY(target.getPosition().getXPosition(), target.getPosition().getYPosition())
                        ));
                }
                processedImages.Add(sampleImage);
            }
            //imageProcessorPictureBox.Image = processedImages[0].Bitmap;
            imageProcessorPictureBox.Image = cameraImages[6].Bitmap;
        }

        private void updateSprayers()
        {
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
            sprayer.setCurrentPosition(currentPosition);
            sprayer.checkTargetLocations();
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

                motionChart.Series["Camera"].Points.Clear();
                motionChart.Series["Sprayer"].Points.Clear();

                for (int i = 0; i < 8; i++)
                {
                    motionChart.Series["Camera"].Points.AddXY(cameraPositions[i].getXPosition(), cameraPositions[i].getYPosition());
                    motionChart.Series["Sprayer"].Points.AddXY(sprayerPositions[i].getXPosition(), sprayerPositions[i].getYPosition());
                }
            }
        }

        #endregion
    }
}
