﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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

        // Serial port connections
        private const string WSS_SERIAL_PORT = "COM7";
        private const string SPRAYER_RELAY_PORT = "COM11";
        private const string IMU_SERIAL_PORT = "COM4";
        private const string LIGHT_SENSOR_SERIAL_PORT = "COM3";

        // Motion constants
        private const float DISTANCE_TRAVELLED_THRESHOLD = 0.16f; // in metres

        // Motion Objects
        private Motion motionController;
        private Thread systemThread;

        // Motion Volatiles
        private volatile Position currentPosition;
        private volatile Position lastImageCapturedPosition;
        private volatile bool stop = false;

        // Light sensor objects
        private LightSensor lightSensor;

        // Light sensor volatiles
        private volatile float[] illuminance;
        private volatile double[] gain;

        // Sprayer Objects
        Position[] sprayerPositions;
        Sprayer sprayer;

        // Vision
        private List<Image<Bgr,byte>> originalImages;
        private List<Image<Bgr,byte>> colourCorrectedImages;
        private List<Image<Gray, byte>> maskImages;
        private List<Image<Bgr, byte>> classifiedImages;

        private HOGDescriptor hogDescriptor;
        private BinaryClassifier binaryClassifier;

        private Dictionary<uint, Camera> cameras;
        private int cameraCount;
        Position[] cameraPositions = new Position[8];
        public static readonly uint[] SERIAL_NUMBERS = new uint[8]
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
        string workingDirectory;

        // Image processing variables
        int WINDOW_SIZE = 96;

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
                    cameras.Add(SERIAL_NUMBERS[i], new Camera(SERIAL_NUMBERS[i]));
                    //cameras[SERIAL_NUMBERS[i]].setCameraProfile(Camera.CameraProfile.gainVsIlluminance);
                    cameraSelectionCombo.Items.Add(SERIAL_NUMBERS[i]);
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

        private Boolean initialiseImageProcessor()
        {
            try
            {
                hogDescriptor = new HOGDescriptor();
            }
            catch (Exception)
            {
                return false;
            }
            string inputFile = Environment.CurrentDirectory + "\\lut\\input.tif";
            string outputFile = Environment.CurrentDirectory + "\\lut\\output.tif";
            if (File.Exists(inputFile) && File.Exists(outputFile))
            {
                ImageProcessor.loadLUT(new Image<Bgr, byte>(inputFile), new Image<Bgr, byte>(outputFile));
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean initialiseMachineLearning()
        {
            try
            {
                binaryClassifier = new BinaryClassifier();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public Boolean initialiseLightSensor()
        {
            try
            {
                lightSensor = new LightSensor(LIGHT_SENSOR_SERIAL_PORT);
                gain = new double[cameraCount];
                illuminance = new float[cameraCount];
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void createNewWorkingDirectory()
        {
            workingDirectory = Environment.CurrentDirectory;
            string[] folders = Directory.GetDirectories(workingDirectory);
            workingDirectory = workingDirectory + "\\" + (folders.Length + 1);
            Directory.CreateDirectory(workingDirectory);
        }

        public Boolean initialiseSystem()
        {
            if (!initialiseVision())
            {
                MessageBox.Show("Failed to initialise cameras. Check camera connections and try again.");
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseMotion())
            {
                MessageBox.Show("Failed to initialise motion. Check IMU and WSS connections and try again.", "Error", MessageBoxButtons.OK);
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseSprayer())
            {
                MessageBox.Show("Failed to initialise sprayers. Check sprayer connections and try again.", "Error", MessageBoxButtons.OK);
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseImageProcessor())
            {
                MessageBox.Show("Failed to initialise ImageProcessor, make sure LUT files are located in local directory and HOG parameters are realisable.", "Error", MessageBoxButtons.OK);
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseMachineLearning())
            {
                MessageBox.Show("Failed to initialise Machine Learning, make sure the predictor models are located in the local directory.", "Error", MessageBoxButtons.OK);
                runBtn.Text = "Reinitialise";
                return false;
            }
            if (!initialiseLightSensor())
            {
                MessageBox.Show("Failed to initialise light sensor. Check light sensor connection and try again.", "Error", MessageBoxButtons.OK);
                return false;
            }
            for (int i = 0; i < 8; i++)
            {
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SERIAL_NUMBERS[i], currentPosition);
            }
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
            updateMotionChart(currentPosition);
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
            createNewWorkingDirectory();
            currentPosition = new Position(0, 0);
            stop = false;
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SERIAL_NUMBERS[i]].start();
            }
            systemThread = new Thread(systemLoop);
            sprayer.startSensors();
            systemThread.Start();
        }

        public void stopSystem()
        {
            stop = true;
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SERIAL_NUMBERS[i]].stop();
            }
            sprayer.stopSensors();
        }

        #endregion

        #region Threads

        public void systemLoop()
        {
            while (!stop)
            {
                updateIlluminanceAndGain();
                currentPosition = motionController.run();
                updateMotionChart(currentPosition);
                updateCameras();
                updateSprayers();
                updateLabels();
                Thread.Sleep(20);
            }
        }

        public void updateIlluminanceAndGain()
        {
            illuminance = lightSensor.getCurrentReadings();
            for (int i = 0; i < cameraCount; i++)
            {
                gain[i] = -0.024 * illuminance[i] + 24;
                //cameras[SERIAL_NUMBERS[i]].setGain(gain[i]);
            }
        }

        private void updateLabels()
        {
            this.BeginInvoke(new Action(() =>
            {
                if (!cameraSelectionCombo.SelectedItem.Equals("None"))
                {
                    uint serial = (uint)cameraSelectionCombo.SelectedItem;
                    illuminanceLabel.Text = String.Format("{0}", illuminance[Array.IndexOf(SERIAL_NUMBERS, serial)]);
                    shutterSpeedLabel.Text = String.Format("{0}", cameras[serial].getShutter());
                    gainLabel.Text = String.Format("{0}", cameras[serial].getGain());
                    exposureLabel.Text = String.Format("{0}", cameras[serial].getExposureValue());
                    brightnessLabel.Text = String.Format("{0}", cameras[serial].getBrightness());
                    whiteBalanceLabel.Text = String.Format("{0}-{1}", cameras[serial].getWhiteBalance()[0], cameras[serial].getWhiteBalance()[1]);
                }
            }));
        }

        public void updateCameras()
        {
            if (lastImageCapturedPosition != null)
            {
                double xDiff = currentPosition.getXPosition() - lastImageCapturedPosition.getXPosition();
                double yDiff = currentPosition.getYPosition() - lastImageCapturedPosition.getYPosition();
                if (DISTANCE_TRAVELLED_THRESHOLD <= Math.Sqrt(xDiff * xDiff + yDiff * yDiff))
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
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SERIAL_NUMBERS[i], currentPosition);
            }
        }

        public void processImage()
        {
            originalImages = new List<Image<Bgr,byte>>(cameraCount);
            colourCorrectedImages = new List<Image<Bgr,byte>>(cameraCount);
            maskImages = new List<Image<Gray,byte>>(cameraCount);
            classifiedImages = new List<Image<Bgr,byte>>(cameraCount);

            for (int i = 0; i < cameraCount; i++)
            {
                // Acquisition
                originalImages.Add(cameras[SERIAL_NUMBERS[i]].getImage());
                AppendLine(String.Format("Camera {0} Image Captured", SERIAL_NUMBERS[i]));

                // Segmentation
                Image<Bgr, byte> colourCorrectedImage = ImageProcessor.applyLUT(originalImages[i]);
                Image<Gray,byte> maskImage = ImageProcessor.thresholdImage(colourCorrectedImage);
                maskImage = ImageProcessor.morphology(maskImage);

                colourCorrectedImages.Add(colourCorrectedImage);
                maskImages.Add(maskImage);
                classifiedImages.Add(originalImages[i].Clone());

                // Window extraction
                List<int[]> windowLocationArray = ImageProcessor.findWindows(maskImage, WINDOW_SIZE);
                double[] imageDescriptor = new double[] { 0, 0 };
                for (int n = 0; n < windowLocationArray.Count(); n++)
                {
                    int[] location = windowLocationArray[n];
                    Rectangle roi = new Rectangle(location[0], location[1], WINDOW_SIZE, WINDOW_SIZE);
                    Image<Bgr, byte> window = ImageProcessor.extractROI(originalImages[i], roi);
                    Image<Gray, byte> mask = ImageProcessor.extractROI(maskImage, roi);

                    //Feature extraction
                    double[] windowDescriptor = hogDescriptor.compute(window, mask);

                    //Stage one classification
                    Prediction windowPrediction = binaryClassifier.predictWindow(windowDescriptor);
                    if (windowPrediction.label) imageDescriptor[0]++;
                    else imageDescriptor[1]++;

                    //Fancy classification output images
                    double blue = 0;
                    double red = 0;
                    if (windowPrediction.probability <= 0.5)
                    {
                        red = windowPrediction.probability * 510;
                        blue = 255;
                    }
                    else
                    {
                        red = 255;
                        blue = 510 - windowPrediction.probability * 510;
                    }
                    classifiedImages[i].Draw(roi, new Bgr(blue, 0, red), 3);
                }

                // Stage two classification - is image lantana based on number of lantana/non-lantana windows?
                Prediction imagePrediction = binaryClassifier.predictImage(imageDescriptor);

                //If image classified positive
                if (imagePrediction.label)
                {
                    AppendLine(String.Format("Lantana found at camera: {0}", i + 1));
                    Target target = new Target(lastImageCapturedPosition, SERIAL_NUMBERS[i]);
                    sprayer.addTarget(target);
                    this.BeginInvoke(new Action(() =>motionChart.Series["Target"].Points.AddXY(target.getPosition().getXPosition(), target.getPosition().getYPosition())));
                }
            }
            updatePictureBoxes();
        }

        private void updatePictureBoxes()
        {
            this.BeginInvoke(new Action(() =>
            {
                if (!cameraSelectionCombo.SelectedItem.Equals("None"))
                {
                    uint serial = (uint)cameraSelectionCombo.SelectedItem;
                    int index = Array.IndexOf(SERIAL_NUMBERS, serial);
                    pictureBox1.Image = originalImages[index].Bitmap;
                    pictureBox2.Image = colourCorrectedImages[index].Bitmap;
                    pictureBox3.Image = maskImages[index].Bitmap;
                    pictureBox4.Image = classifiedImages[index].Bitmap;
                }
            }));
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

        private void updateMotionChart(Position position)
        {
            if (this.motionChart.InvokeRequired)
            {
                UpdateChartCallback d = new UpdateChartCallback(updateMotionChart);
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
