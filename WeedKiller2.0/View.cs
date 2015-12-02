using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WeedKiller2._0
{
    public partial class View : Form
    {
        #region Global Constants
        
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
        private const float DISTANCE_BETWEEN_FRAMES = 0.10f; // metres
        private const int WINDOW_SIZE = 96; // pixels

        #endregion

        #region Global Variables

        // System
        private Thread systemThread;
        private volatile bool stopFlag = false;
        string workingDirectory;
        private int directoryCount;

        // Motion
        private Motion motionController;
        private Position[] sprayerPositions;
        private Position[] cameraPositions;
        private volatile Position currentPosition;
        private volatile Position previousPosition;

        // Cameras
        private Dictionary<uint, Camera> cameras;
        private int cameraCount;
        private List<List<Image<Bgr, byte>>> imageHistory;
        private StreamWriter imageLog;

        // Image Processing
        private HOGDescriptor hogDescriptor;
        private BinaryClassifier binaryClassifier;

        // Light Sensor
        private LightSensor lightSensor;
        private volatile float[] illuminance;
        private volatile double[] gain;

        // Sprayers
        private Sprayer sprayer;
        private List<Target> targetsToDraw;

        #endregion

        #region Main Initilisation

        /// <summary>
        /// Form constructor, initialises GUI and system.
        /// </summary>
        public View()
        {
            InitializeComponent();
            initialiseCOMPorts();
            initialiseSystem();
        }

        /// <summary>
        /// Generate COM port list and bind to combo boxes. Set default ports.
        /// </summary>
        private void initialiseCOMPorts()
        {
            comboBoxWSS.DataSource = SerialPort.GetPortNames();
            comboBoxLightSensor.DataSource = SerialPort.GetPortNames();
            comboBoxSprayers.DataSource = SerialPort.GetPortNames();
            comboBoxIMU.DataSource = SerialPort.GetPortNames();
            
            comboBoxLightSensor.SelectedItem = "COM3";
            comboBoxIMU.SelectedItem = "COM4";
            comboBoxWSS.SelectedItem = "COM7";
            comboBoxSprayers.SelectedItem = "COM11";
        }

        /// <summary>
        /// Button for refreshing COM port combo boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            initialiseCOMPorts();
        }

        /// <summary>
        /// Set current position to zero and create instance of Motion controller.
        /// </summary>
        /// <returns></returns>
        private Boolean initialiseMotion()
        {
            try
            {
                currentPosition = new Position(0, 0);
                motionController = new Motion();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace,"Error",MessageBoxButtons.OK);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Count number of connected cameras and initialise connections.
        /// </summary>
        /// <returns></returns>
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
                    cameras[SERIAL_NUMBERS[i]].setCameraProfile(Camera.CameraProfile.indoorProfile);
                    cameraSelectionCombo.Items.Add(SERIAL_NUMBERS[i]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Create new instance of sprayer class.
        /// </summary>
        /// <returns></returns>
        private Boolean initialiseSprayer()
        {
            try
            {
                sprayer = new Sprayer((string)comboBoxSprayers.SelectedItem);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Perform image processing overhead up front for HOG generation and colour correction LUT.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Perform machine learning overhead up front by building predictor models from input files.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Create new instance of light sensor class, with gain and illuminance values for all cameras.
        /// </summary>
        /// <returns></returns>
        public Boolean initialiseLightSensor()
        {
            try
            {
                lightSensor = new LightSensor((string)comboBoxLightSensor.SelectedItem);
                gain = new double[cameraCount];
                illuminance = new float[cameraCount];
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
                MessageBox.Show("Failed to initialise cameras. Check camera connections and try again.");
                runButton.Text = "Reinitialise";
                return false;
            }
            if (!initialiseMotion())
            {
                MessageBox.Show("Failed to initialise motion. Check IMU and WSS connections and try again.", "Error", MessageBoxButtons.OK);
                runButton.Text = "Reinitialise";
                return false;
            }
            if (!initialiseSprayer())
            {
                MessageBox.Show("Failed to initialise sprayers. Check sprayer connections and try again.", "Error", MessageBoxButtons.OK);
                runButton.Text = "Reinitialise";
                return false;
            }
            if (!initialiseImageProcessor())
            {
                MessageBox.Show("Failed to initialise ImageProcessor, make sure LUT files are located in local directory and HOG parameters are realisable.", "Error", MessageBoxButtons.OK);
                runButton.Text = "Reinitialise";
                return false;
            }
            if (!initialiseMachineLearning())
            {
                MessageBox.Show("Failed to initialise Machine Learning, make sure the predictor models are located in the local directory.", "Error", MessageBoxButtons.OK);
                runButton.Text = "Reinitialise";
                return false;
            }
            if (!initialiseLightSensor())
            {
                MessageBox.Show("Failed to initialise light sensor. Check light sensor connection and try again.", "Error", MessageBoxButtons.OK);
                runButton.Text = "Reinitialise";
                return false;
            }
            cameraPositions = new Position[cameraCount];
            for (int i = 0; i < 8; i++)
            {
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SERIAL_NUMBERS[i], currentPosition);
            }
            targetsToDraw = new List<Target>();
            sprayerPositions = Position.CalculateGlobalSprayerPositions(currentPosition);
            updateMotionChart(currentPosition);
            return true;
        }

        #endregion

        #region Form Events

        private void runButton_Click(object sender, EventArgs e)
        {
            if (runButton.Text == "Start")
            {
                runButton.Text = "Stop";
                runButton.Enabled = false;
                startSystem();
                runButton.Enabled = true;
            }
            else if (runButton.Text == "Stop")
            {
                runButton.Text = "Start";
                runButton.Enabled = false;
                stopSystem();
                runButton.Enabled = true;
            }
            else
            {
                if (initialiseSystem())
                {
                    runButton.Text = "Start";
                }
            }
        }

        #endregion

        #region Program Start and Stop Methods

        public void startSystem()
        {
            stopFlag = false;
            currentPosition = new Position(0, 0);
            if (checkBoxRecord.Checked)
            {
                createNewWorkingDirectory();
                imageHistory = new List<List<Image<Bgr, byte>>>();
                imageLog = new StreamWriter(workingDirectory + "\\images" + directoryCount + ".csv", true);
                imageLog.WriteLine("Time,Camera,Frame,Positive Windows,Negative Windows,Image Score,Image Label");
                motionController.setupLogging(true, workingDirectory + "\\motion" + directoryCount + ".csv");
            }            
            motionController.initConnection((string)comboBoxWSS.SelectedItem, (string)comboBoxIMU.SelectedItem);
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SERIAL_NUMBERS[i]].start();
            }
            sprayer.startSensors();
            systemThread = new Thread(systemLoop);
            systemThread.Start();
            enableDisableGUI();
        }

        public void stopSystem()
        {
            stopFlag = true;
            motionController.closeConnection();
            for (int i = 0; i < cameraCount; i++)
            {
                cameras[SERIAL_NUMBERS[i]].stop();
            }
            sprayer.stopSensors();
            if (checkBoxRecord.Checked)
            {
                imageLog.Close();
                Thread saveImagesThread = new Thread(saveImageHistory);
                saveImagesThread.Start();
            }
            enableDisableGUI();
        }

        /// <summary>
        /// Create new working directory for data logging.
        /// </summary>
        private void createNewWorkingDirectory()
        {
            workingDirectory = Environment.CurrentDirectory;
            string[] folders = Directory.GetDirectories(workingDirectory);
            directoryCount = folders.Length;
            workingDirectory = workingDirectory + "\\" + directoryCount;
            Directory.CreateDirectory(workingDirectory);
            Directory.CreateDirectory(workingDirectory + "\\images\\");
        }

        private void enableDisableGUI()
        {
            refreshButton.Enabled = !refreshButton.Enabled;
            comboBoxWSS.Enabled = !comboBoxWSS.Enabled;
            comboBoxLightSensor.Enabled = !comboBoxLightSensor.Enabled;
            comboBoxIMU.Enabled = !comboBoxIMU.Enabled;
            comboBoxSprayers.Enabled = !comboBoxSprayers.Enabled;
            checkBoxGraph.Enabled = !checkBoxGraph.Enabled;
            checkBoxImages.Enabled = !checkBoxImages.Enabled;
            checkBoxRecord.Enabled = !checkBoxRecord.Enabled;
        }

        #endregion

        #region Threads

        public void systemLoop()
        {
            while (!stopFlag)
            {
                //updateIlluminanceAndGain();
                currentPosition = motionController.run();
                if (checkBoxGraph.Checked) updateMotionChart(currentPosition);
                updateCameras();
                updateSprayers();
                updateLabels();
                Thread.Sleep(20);
            }
        }

        public void saveImageHistory()
        {
            this.BeginInvoke(new Action(() => progressBar.Value = 0));
            for (int frame = 0; frame < imageHistory.Count; frame++)
            {
                for (int camera = 0; camera < cameraCount; camera++)
                {
                    imageHistory[frame][camera].Save(workingDirectory + "\\images\\" + camera + "_" + (frame + 1) + ".tif");
                    int value = (int)(100.0 * (frame * cameraCount + camera + 1) / imageHistory.Count / cameraCount);
                    value = value < 0 ? 0 : value;
                    value = value > 100 ? 100 : value;
                    this.BeginInvoke(new Action(() => progressBar.Value = value));
                }
            }
        }

        public void updateIlluminanceAndGain()
        {
            illuminance = lightSensor.getCurrentReadings();
            for (int i = 0; i < cameraCount; i++)
            {
                gain[i] = -0.024 * illuminance[i] + 24;
                cameras[SERIAL_NUMBERS[i]].setGain(gain[i]);
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
                    gammaLabel.Text = String.Format("{0}", cameras[serial].getGamma());
                }
            }));
        }

        public void updateCameras()
        {
            if (previousPosition != null)
            {
                double xDiff = currentPosition.getXPosition() - previousPosition.getXPosition();
                double yDiff = currentPosition.getYPosition() - previousPosition.getYPosition();
                if (DISTANCE_BETWEEN_FRAMES <= Math.Sqrt(xDiff * xDiff + yDiff * yDiff))
                {
                    previousPosition = currentPosition.clone();
                    new Thread(processImages).Start();
                }
            }
            else
            {
                previousPosition = currentPosition.clone();
                new Thread(processImages).Start();
            }
            for (int i = 0; i < 8; i++)
            {
                cameraPositions[i] = Position.CalculateGlobalCameraPosition(SERIAL_NUMBERS[i], currentPosition);
            }
        }

        public void processImages()
        {
            List<Image<Bgr, byte>> images = null;
            List<Image<Bgr, byte>> originalImages = null;
            List<Image<Bgr, byte>> colourCorrectedImages = null;
            List<Image<Gray, byte>> maskImages = null;
            List<Image<Bgr, byte>> classifiedImages = null;

            if (checkBoxRecord.Checked)
            {
                images = new List<Image<Bgr, byte>>(cameraCount);
            }

            if (checkBoxImages.Checked)
            {
                originalImages = new List<Image<Bgr, byte>>(cameraCount);
                colourCorrectedImages = new List<Image<Bgr, byte>>(cameraCount);
                maskImages = new List<Image<Gray, byte>>(cameraCount);
                classifiedImages = new List<Image<Bgr, byte>>(cameraCount);
            }

            for (int i = 0; i < cameraCount; i++)
            {
                // Acquisition
                Image<Bgr, byte> originalImage = cameras[SERIAL_NUMBERS[i]].getImage();
                if (checkBoxRecord.Checked)
                {
                    images.Add(originalImage);
                }
                // Segmentation
                Image<Bgr, byte> colourCorrectedImage = ImageProcessor.applyLUT(originalImage);
                Image<Gray, byte> maskImage = ImageProcessor.thresholdImage(colourCorrectedImage);
                maskImage = ImageProcessor.morphology(maskImage);

                if (checkBoxImages.Checked)
                {
                    originalImages.Add(originalImage);
                    colourCorrectedImages.Add(colourCorrectedImage);
                    maskImages.Add(maskImage);
                    classifiedImages.Add(originalImage.Clone());
                }

                // Window extraction
                List<int[]> windowLocationArray = ImageProcessor.findWindows(maskImage, WINDOW_SIZE);
                double[] imageDescriptor = new double[] { 0, 0 };
                for (int n = 0; n < windowLocationArray.Count(); n++)
                {
                    int[] location = windowLocationArray[n];
                    Rectangle roi = new Rectangle(location[0], location[1], WINDOW_SIZE, WINDOW_SIZE);
                    Image<Bgr, byte> window = ImageProcessor.extractROI(originalImage, roi);
                    Image<Gray, byte> mask = ImageProcessor.extractROI(maskImage, roi);

                    //Feature extraction
                    double[] windowDescriptor = hogDescriptor.compute(window, mask);

                    //Stage one classification
                    Prediction windowPrediction = binaryClassifier.predictWindow(windowDescriptor);
                    if (windowPrediction.label) imageDescriptor[0]++;
                    else imageDescriptor[1]++;

                    if (checkBoxImages.Checked)
                    {
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
                }

                // Stage two classification - is image lantana based on number of lantana/non-lantana windows?
                Prediction imagePrediction = binaryClassifier.predictImage(imageDescriptor);

                //If image classified positive
                if (imagePrediction.label)
                {
                    AppendLine(String.Format("Lantana found at camera: {0}", i + 1));
                    Target target = new Target(previousPosition, SERIAL_NUMBERS[i]);
                    targetsToDraw.Add(target);
                    sprayer.addTarget(target);
                }
                if (checkBoxRecord.Checked)
                {
                    //"Time", "Camera", "Frame", "Positive Windows", "Negative Windows", "Image Score", "Image Label"
                    imageLog.WriteLine("{0},{1},{2},{3},{4},{5},{6}", previousPosition.getTime().ToString("dd/MM/yyyy hh:mm:ss.fff"), i, (imageHistory.Count + 1), imageDescriptor[0], imageDescriptor[1], imagePrediction.score, imagePrediction.label);
                }
            }
            if (checkBoxRecord.Checked)
            {
                imageHistory.Add(images);
            }
            if (checkBoxImages.Checked)
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!cameraSelectionCombo.SelectedItem.Equals("None"))
                    {
                        uint serial = (uint)cameraSelectionCombo.SelectedItem;
                        int index = Array.IndexOf(SERIAL_NUMBERS, serial);
                        updatePictureBoxes(originalImages[index], colourCorrectedImages[index], maskImages[index], classifiedImages[index]);
                    }
                }));
            }
        }

        private void updatePictureBoxes(Image<Bgr,byte> image1, Image<Bgr, byte> image2, Image<Gray, byte> image3, Image<Bgr, byte> image4)
        {
            this.BeginInvoke(new Action(() =>
            {
                pictureBox1.Image = image1.Bitmap;
                pictureBox2.Image = image2.Bitmap;
                pictureBox3.Image = image3.Bitmap;
                pictureBox4.Image = image4.Bitmap;
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

                
                foreach (Target target in targetsToDraw)
                {
                    motionChart.Series["Target"].Points.AddXY(target.getPosition().getXPosition(), target.getPosition().getYPosition());
                }
                targetsToDraw = new List<Target>();
            }
        }

        #endregion

    }
}
