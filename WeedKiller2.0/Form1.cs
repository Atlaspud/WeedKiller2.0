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
        private const string WSS_SERIAL_PORT = "COM4";
        private const string IMU_SERIAL_PORT = "COM15";

        // Motion Objects
        private Motion motionController;
        private Thread motionThread;

        // Motion Volatiles
        private volatile Position currentPosition;
        private volatile bool stop = false;

        // Sprayer constants
        //private const string SPRAYER_RELAY_PORT = "COM4";

        #endregion

        #region Main Initilisation

        public Form1()
        {
            InitializeComponent();
            InitialiseMotion();
            //initilaiseSprayer();
            UpdateChart(currentPosition);
            AppendLine("" + currentPosition.getXPosition());
        }

        private void InitialiseMotion()
        {
            currentPosition = new Position(0, 0);
            motionController = new Motion(WSS_SERIAL_PORT, IMU_SERIAL_PORT);
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
            motionThread = new Thread(getMotion);
            motionThread.Start();
        }

        public void stopSystem()
        {
            stop = true;
            changeView(true);
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
            Position currentPosition;
            while (!stop)
            {
                currentPosition = motionController.run();
                UpdateChart(currentPosition);
                AppendLine("x: " + currentPosition.getXPosition() + " y: " + currentPosition.getYPosition());


                Thread.Sleep(5);
            }
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
