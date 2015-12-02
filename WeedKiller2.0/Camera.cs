using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlyCapture2Managed;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Windows.Forms;
using System.Threading;

namespace WeedKiller2._0
{
    class Camera
    {
        #region Global Variables

        public enum CameraProfile { defaultProfile, shutterSweep, exposureValueSweep, shutterVsIlluminance, gainVsIlluminance, indoorProfile };

        private uint cameraSerialNumber;

        private ManagedGigECamera camera;
        private CameraProperty autoExposure;
        private CameraProperty brightness;
        private CameraProperty frameRate;
        private CameraProperty gain;
        private CameraProperty shutter;
        private CameraProperty temperature;
        private CameraProperty whiteBalance;
        private CameraProperty gamma;

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
            initialiseCamera();
        }

        private void initialiseCamera()
        {
            //Image size = 1280x1024, raw8 format, no offset
            GigEImageSettings imageSettings = new GigEImageSettings();
            imageSettings.offsetX = 0;
            imageSettings.offsetY = 0;
            imageSettings.height = 1024;
            imageSettings.width = 1280;
            imageSettings.pixelFormat = PixelFormat.PixelFormatRgb;
            camera.SetGigEImageSettings(imageSettings);

            autoExposure = new CameraProperty(PropertyType.AutoExposure);
            brightness = new CameraProperty(PropertyType.Brightness);
            frameRate = new CameraProperty(PropertyType.FrameRate);
            shutter = new CameraProperty(PropertyType.Shutter);
            whiteBalance = new CameraProperty(PropertyType.WhiteBalance);
            gain = new CameraProperty(PropertyType.Gain);
            temperature = new CameraProperty(PropertyType.Temperature);
            gamma = new CameraProperty(PropertyType.Gamma);

            setCameraProfile(CameraProfile.defaultProfile);

            rawImage = new ManagedImage();
            convertedImage = new ManagedImage();
        }

        #endregion

        #region Camera Profiles

        public void setCameraProfile(CameraProfile cameraProfile)
        {
            switch (cameraProfile)
            {
                case CameraProfile.defaultProfile:
                    setAutoExposure();
                    setAutoGain();
                    setAutoShutter();
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    setAutoGamma();
                    break;

                case CameraProfile.indoorProfile:
                    setExposureValue(2.414);
                    setGain(24.014);
                    setAutoGamma();
                    setShutter(10);
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    break;

                case CameraProfile.exposureValueSweep:
                    setExposureValue(0);
                    setAutoGain();
                    setAutoShutter(1, 256);
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    break;

                case CameraProfile.shutterSweep:
                    setAutoExposure();
                    setAutoGain();
                    setShutter(0);
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    break;

                case CameraProfile.gainVsIlluminance:
                    setExposureValue(1.05);
                    setGain(0);
                    setShutter(0.5);
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    break;

                case CameraProfile.shutterVsIlluminance:
                    setAutoExposure();
                    setAutoGain();
                    setShutter(0);
                    setAutoWhiteBalance();
                    setAutoBrightness();
                    break;
            }
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

        public Image<Bgr, Byte> waitForImage()
        {
            camera.WaitForBufferEvent(rawImage, 0);
            //rawImage.Convert(PixelFormat.PixelFormatBgr, convertedImage);
            rawImage.Convert(convertedImage);
            return new Image<Bgr, Byte>(convertedImage.bitmap);
        }

        public Image<Bgr, Byte> getImage()
        {
            try {
                camera.RetrieveBuffer(rawImage);
                rawImage.Convert(PixelFormat.PixelFormatBgr, convertedImage);
                return new Image<Bgr, Byte>(convertedImage.bitmap);
            } catch (Exception)
            {
                return new Image<Bgr, Byte>(1280, 1024, new Bgr(0, 0, 0));
            }
        }

        #region Camera Property Getters and Setters

        public void setGamma(double value)
        {
            gamma.onOff = true;
            gamma.absControl = true;
            gamma.autoManualMode = false;
            gamma.absValue = (float)value;
            camera.SetProperty(gamma);
        }

        public void setAutoGamma()
        {
            gamma.onOff = true;
            gamma.autoManualMode = true;
            camera.SetProperty(gamma);
        }

        public double getGamma()
        {
            gamma = camera.GetProperty(PropertyType.Gamma);
            return gamma.absValue;
        }

        public double getExposureValue()
        {
            autoExposure = camera.GetProperty(PropertyType.AutoExposure);
            return autoExposure.absValue;
        }

        public void setExposureValue(double value)
        {
            autoExposure.onOff = true;
            autoExposure.absControl = true;
            autoExposure.autoManualMode = false;
            autoExposure.absValue = (float)value;
            camera.SetProperty(autoExposure);
        }

        public void setAutoExposure()
        {
            autoExposure.onOff = true;
            autoExposure.absControl = true;
            autoExposure.autoManualMode = true;
            camera.SetProperty(autoExposure);
        }

        public double getBrightness()
        {
            brightness = camera.GetProperty(PropertyType.Brightness);
            return brightness.absValue;
        }

        public void setBrightness(double value)
        {
            brightness.onOff = true;
            brightness.absControl = true;
            brightness.autoManualMode = false;
            brightness.absValue = (float)value;
            camera.SetProperty(brightness);
        }

        public void setAutoBrightness()
        {
            brightness.onOff = true;
            brightness.absControl = true;
            brightness.autoManualMode = true;
            camera.SetProperty(brightness);
        }

        public double getFrameRate()
        {
            frameRate = camera.GetProperty(PropertyType.FrameRate);
            return brightness.absValue;
        }

        public void setFrameRate(double value)
        {
            frameRate.onOff = true;
            frameRate.absControl = true;
            frameRate.autoManualMode = false;
            frameRate.absValue = (float)value;
            camera.SetProperty(frameRate);
        }

        public void setAutoFrameRate()
        {
            frameRate.onOff = true;
            frameRate.absControl = true;
            frameRate.autoManualMode = true;
            camera.SetProperty(frameRate);
        }

        public double getShutter()
        {
            shutter = camera.GetProperty(PropertyType.Shutter);
            return shutter.absValue;
        }

        public void setShutter(double value)
        {
            shutter.onOff = true;
            shutter.absControl = true;
            shutter.autoManualMode = false;
            shutter.absValue = (float)value;
            camera.SetProperty(shutter);
        }

        public void setAutoShutter(double lower = 1, double upper = 4095)
        {
            shutter.onOff = true;
            shutter.absControl = true;
            shutter.autoManualMode = true;
            camera.SetProperty(shutter);
            uint range = (uint)(33554432 + lower * Math.Pow(2, 12) + upper);
            camera.WriteRegister(0x1098, range);
        }

        public double getGain()
        {
            gain = camera.GetProperty(PropertyType.Gain);
            return gain.absValue;
        }

        public void setGain(double value)
        {
            gain.onOff = true;
            gain.absControl = true;
            gain.autoManualMode = false;
            gain.absValue = (float)value;
            camera.SetProperty(gain);
        }

        public void setAutoGain(double lower = 0, double upper = 255)
        {
            gain.onOff = true;
            gain.absControl = true;
            gain.autoManualMode = true;
            camera.SetProperty(gain);
            uint range = (uint)(33554432 + lower * Math.Pow(2, 12) + upper);
            camera.WriteRegister(0x10A0, range);
        }

        public int[] getWhiteBalance()
        {
            whiteBalance = camera.GetProperty(PropertyType.WhiteBalance);
            return new int[] { (int)whiteBalance.valueA, (int)whiteBalance.valueB };
        }

        public void setWhiteBalance(double red, double blue)
        {
            whiteBalance.onOff = true;
            whiteBalance.autoManualMode = false;
            camera.SetProperty(whiteBalance);
        }

        public void setAutoWhiteBalance()
        {
            whiteBalance.onOff = false;
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
