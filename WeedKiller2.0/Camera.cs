using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlyCapture2Managed;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WeedKiller2._0
{
    class Camera
    {
        #region Global Variables

        private uint cameraSerialNumber;

        private ManagedGigECamera camera;
        private CameraProperty exposure;
        private CameraProperty brightness;
        private CameraProperty frameRate;
        private CameraProperty gain;
        private CameraProperty shutter;
        private CameraProperty temperature;
        private CameraProperty whiteBalance;

        private ManagedImage rawImage, convertedImage;

        #endregion

        #region Constructor &  Main Initialisation

        public Camera(uint serialNumber)
        {
            this.cameraSerialNumber = serialNumber;
            camera = new ManagedGigECamera();
            ManagedBusManager busManager = new ManagedBusManager();
            ManagedPGRGuid guid = busManager.GetCameraFromSerialNumber(serialNumber);
            camera.Connect(guid);
            InitialiseCamera();
        }

        private void InitialiseCamera()
        {
            //Image size = 1280x1024, raw8 format, no offset
            GigEImageSettings imageSettings = new GigEImageSettings();
            imageSettings.offsetX = 0;
            imageSettings.offsetY = 0;
            imageSettings.height = 1024;
            imageSettings.width = 1280;
            imageSettings.pixelFormat = PixelFormat.PixelFormatRaw8;
            camera.SetGigEImageSettings(imageSettings);

            exposure = new CameraProperty(PropertyType.AutoExposure);
            brightness = new CameraProperty(PropertyType.Brightness);
            frameRate = new CameraProperty(PropertyType.FrameRate);
            shutter = new CameraProperty(PropertyType.Shutter);
            whiteBalance = new CameraProperty(PropertyType.WhiteBalance);
            gain = new CameraProperty(PropertyType.Gain);
            temperature = new CameraProperty(PropertyType.Temperature);

            initiliseExposure();
            initiliseBrightness();
            initiliseFrameRate();
            initiliseShutter();
            initiliseGain();
            initiliseWhiteBalance();

            rawImage = new ManagedImage();
            convertedImage = new ManagedImage();

            setAutoExposure();
            setAutoBrightness();
            setFrameRate(0.1);
            setAutoShutter();
            //setAutoGain(0,255);
            //setWhiteBalance(1023,1023);

        }



        #endregion

        #region Start & Stop Capture

        public void start()
        {
            camera.StartCapture();
        }

        public void stop()
        {
            camera.StopCapture();
        }

        #endregion

        #region GetImage

        public Image<Bgr, Byte> getImage()
        {
            camera.RetrieveBuffer(rawImage);
            rawImage.Convert(PixelFormat.PixelFormatBgr, convertedImage);
            return new Image<Bgr, Byte>(convertedImage.bitmap);
        }

        #endregion

        #region Exposure Initialisation & Methods

        private void initiliseExposure()
        {
            exposure.onOff = true;
            exposure.autoManualMode = true;
            exposure.absControl = true;
            camera.SetProperty(exposure);
        }

        public void setExposure(double value)
        {
            exposure.autoManualMode = false;
            exposure.absValue = (float)value;
            camera.SetProperty(exposure);
        }

        public void setAutoExposure(double lower = 1, double upper = 1023)
        {
            exposure.autoManualMode = true;
            camera.SetProperty(exposure);
            uint range = (uint)(33554432 + lower * Math.Pow(2, 12) + upper);
            camera.WriteRegister(0x1088, range);
        }

        #endregion

        #region Brightness Initialisation & Methods

        private void initiliseBrightness()
        {
            brightness.onOff = true;
            brightness.autoManualMode = true;
            brightness.absControl = true;
            camera.SetProperty(brightness);
        }

        public void setBrightness(double value)
        {
            brightness.autoManualMode = false;
            brightness.absValue = (float)value;
            camera.SetProperty(brightness);
        }

        public void setAutoBrightness(double lower = 0, double upper = 0)
        {
            brightness.autoManualMode = true;
            camera.SetProperty(brightness);
        }

        #endregion

        #region FrameRate Initialisation & Methods

        private void initiliseFrameRate()
        {
            frameRate.onOff = true;
            frameRate.autoManualMode = false;
            frameRate.absControl = true;
            frameRate.absValue = (float)0.5;
            camera.SetProperty(frameRate);
        }

        public void setFrameRate(double value)
        {
            frameRate.autoManualMode = false;
            frameRate.absValue = (float)value;
            camera.SetProperty(frameRate);
        }

        #endregion

        #region Shutter Initialisation & Methods

        private void initiliseShutter()
        {
            shutter.onOff = true;
            shutter.autoManualMode = true;
            shutter.absControl = true;
            camera.SetProperty(shutter);
        }

        public void setShutterSpeed(double value)
        {
            shutter.autoManualMode = false;
            shutter.absValue = (float)value;
            camera.SetProperty(shutter);
        }

        public void setAutoShutter(double lower = 1, double upper = 4095)
        {
            shutter.autoManualMode = true;
            camera.SetProperty(shutter);
            uint range = (uint)(33554432 + lower * Math.Pow(2, 12) + upper);
            camera.WriteRegister(0x1098, range);
        }

        #endregion

        #region Gain Initialisation & Methods

        private void initiliseGain()
        {
            gain.onOff = true;
            gain.autoManualMode = true;
            gain.absControl = true;
            camera.SetProperty(gain);
            camera.WriteRegister(0x10A0, 0x020000FF);
        }

        public void setGain(double value)
        {
            gain.autoManualMode = false;
            camera.SetProperty(gain);
        }

        public void setAutoGain(double lower = 0, double upper = 255)
        {
            gain.autoManualMode = true;
            gain.absControl = true;
            camera.SetProperty(gain);
            uint range = (uint)(33554432 + lower * Math.Pow(2, 12) + upper);
            camera.WriteRegister(0x10A0, range);
        }

        #endregion

        #region WhiteBalance Initialisation & Methods

        private void initiliseWhiteBalance()
        {
            whiteBalance.onOff = true;
            whiteBalance.autoManualMode = true;
            camera.SetProperty(whiteBalance);
        }

        public void setWhiteBalance(double red, double blue)
        {
            whiteBalance.autoManualMode = false;
            camera.SetProperty(whiteBalance);
        }

        public void setAutoWhiteBalance(double redLower, double redUpper, double blueLower, double blueUpper)
        {
            whiteBalance.onOff = true;
            whiteBalance.autoManualMode = true;
            camera.SetProperty(whiteBalance);
        }

        #endregion

        #region Temperature Initialisation & Methods

        public void enableTemperature()
        {
            temperature.onOff = true;
            temperature.absControl = true;
            camera.SetProperty(temperature);
        }

        public void disableTemperature()
        {
            temperature.onOff = false;
            camera.SetProperty(temperature);
        }

        #endregion

        #region Static Methods

        public static int GetNumberOfCameras()
        {
            ManagedBusManager busManager = new ManagedBusManager();
            return (int)busManager.GetNumOfCameras();
        }

        #endregion
    }
}
