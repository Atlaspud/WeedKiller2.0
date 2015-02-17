using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    public class Motion
    {
        #region Global Variables

        private WheelSpeedSensor wheelSpeedSensor;
        private InertialMeasurementUnit imu;
        private double currentXPosition;
        private double currentYPosition;
        private double initialYaw;
        private int integrationCounter;
        private double x0, y0, x1, y1;
        private double startTime, endTime;
        private Position currentPosition;
        DateTime timeStamp;

        #endregion

        #region Constructor and Initialisation

        public Motion(String wheelSpeedPort, String imuPort)
        {
            wheelSpeedSensor = new WheelSpeedSensor(wheelSpeedPort);
            imu = new InertialMeasurementUnit(imuPort);
            resetOrigin();
        }

        public void resetOrigin()
        {
            currentXPosition = 0;
            currentYPosition = 0;
            integrationCounter = 0;
            x0 = 0; x1 = 0;
            y0 = 0; y1 = 0;
            startTime = 0;
            currentPosition = new Position(DateTime.Now, startTime, 0.0, 0.0, 0);
        }

        #endregion

        #region Class Run Method

        public Position run()
        {
            updatePosition();
            return currentPosition;
        }

        #endregion

        #region Motion Obtain and Calculation Functions

        private void updatePosition()
        {
            double newVelocity = wheelSpeedSensor.getCurrentVelocity();
            double newYaw = imu.getCurrentYaw();
            timeStamp = DateTime.Now;
            double newTime = timeStamp.Ticks / TimeSpan.TicksPerMillisecond;
            trapezoidalRule(timeStamp, newTime, newYaw, newVelocity);
        }

        // Calculates the current x and y coordinates based on velocity, yaw, and the time between readings
        private void trapezoidalRule(DateTime timestamp, double newTime, double newYaw, double newVelocity)
        {
            if (integrationCounter == 0)
            {
                startTime = newTime;
                initialYaw = newYaw;
                newYaw = 0;
                x0 = newVelocity * Math.Sin(newYaw);
                y0 = newVelocity * Math.Cos(newYaw);
                integrationCounter = 1;
            }
            else if (integrationCounter == 1)
            {
                endTime = newTime;
                x1 = newVelocity * Math.Sin(newYaw - initialYaw);
                y1 = newVelocity * Math.Cos(newYaw - initialYaw);

                double x = (x0 + x1) / 2.0 * (endTime - startTime) / 1000.0;
                double y = (y0 + y1) / 2.0 * (endTime - startTime) / 1000.0;

                currentXPosition += x;
                currentYPosition += y;

                currentPosition = new Position(timestamp, (endTime - startTime), currentXPosition, currentYPosition, newYaw - initialYaw);

                startTime = endTime;
                x0 = x1;
                y0 = y1;
            }
        }

        #endregion
    }
}
