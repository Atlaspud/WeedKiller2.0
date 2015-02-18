using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    public class Position
    {
        private DateTime time;
        private double changeInTime;
        private double xPosition;
        private double yPosition;
        private double yaw;

        private static readonly double[,] CAMERA_LOCATIONS = new double[8, 2] { { -0.875, 0.45 }, { -0.625, 0.45 }, { -0.375, 0.45 }, { -0.125, 0.45 }, //Sprayer Relay 1 (Sprayers 1-4)
                                                              { 0.125, 0.45 }, { 0.375, 0.45 }, { 0.625, 0.45 }, { 0.875, 0.45 } }; // Sprayer Relay 2 (Sprayers 5-8)


        private static readonly double[,] SPRAYER_LOCATIONS = new double[8, 2] { { -0.875, -1.35 }, { -0.625, -1.35 }, { -0.375, -1.35 }, { -0.125, -1.35 }, //Sprayer Relay 1 (Sprayers 1-4)
                                                              { 0.125, -1.35 }, { 0.375, -1.35 }, { 0.625, -1.35 }, { 0.875, -1.35 } }; // Sprayer Relay 2 (Sprayers 5-8)


        public Position(DateTime time, double changeInTime, double xPosition, double yPosition, double yaw)
        {
            this.time = time;
            this.changeInTime = changeInTime;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.yaw = yaw;
        }

        public Position(double xPosition, double yPosition)
        {
            time = DateTime.Now;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
        }

        public DateTime getTime()
        {
            return time;
        }

        public double getChangeInTime()
        {
            return changeInTime;
        }

        public double getXPosition()
        {
            return xPosition;
        }

        public double getYPosition()
        {
            return yPosition;
        }

        public double getYaw()
        {
            return yaw;
        }

        public Position clone()
        {
            return new Position(this.xPosition, this.yPosition);
        }

        static public Boolean isPositionWithinLimits(double xMin, double xMax, double yMin, double yMax, Position position)
        {
            if (((position.getXPosition() < xMax) & (position.getXPosition() > xMin))
                & ((position.getYPosition() < yMax) & (position.getYPosition() > yMin)))
            {
                return true;
            }
            return false;
        }

        static public Boolean isPositionWithinLimits(Position centerPosition, Position targetPosition, Double radius)
        {
            double tX = targetPosition.getXPosition();
            double tY = targetPosition.getYPosition();
            double cX = centerPosition.getXPosition();
            double cY = centerPosition.getYPosition();
            if ((Math.Pow(tX - cX, 2) + Math.Pow(tY - cY, 2)) <= Math.Pow(radius, 2))
            {
                return true;
            }
            return false;
        }

        //static public Position CalculateGlobalCameraPosition(uint cameraSerial, Position currentPosition)
        //{
        //    int cameraNumber = Array.IndexOf(Camera.SerialNumbers, cameraSerial);

        //    double rotatedCameraXPosition = (CAMERA_LOCATIONS[cameraNumber, 0] * Math.Cos(-currentPosition.getYaw())) - (CAMERA_LOCATIONS[cameraNumber, 1] * Math.Sin(-currentPosition.getYaw()));
        //    double rotatedCameraYPosition = (CAMERA_LOCATIONS[cameraNumber, 0] * Math.Sin(-currentPosition.getYaw())) + (CAMERA_LOCATIONS[cameraNumber, 1] * Math.Cos(-currentPosition.getYaw()));

        //    double globalCameraXPosition = currentPosition.getXPosition() + rotatedCameraXPosition;
        //    double globalCameraYPosition = currentPosition.getYPosition() + rotatedCameraYPosition;

        //    return new Position(currentPosition.getTime(), currentPosition.getChangeInTime(), globalCameraXPosition, globalCameraYPosition, currentPosition.getYaw());
        //}

        //static public Position[] CalculateGlobalSprayerPositions(Position currentPosition)
        //{
        //    Position[] sprayerGlobalPositions = new Position[8];

        //    for (int i = 0; i < 8; i++)
        //    {
        //        double rotatedXSprayerPosition = (SPRAYER_LOCATIONS[i, 0] * Math.Cos(-currentPosition.getYaw())) - (SPRAYER_LOCATIONS[i, 1] * Math.Sin(-currentPosition.getYaw()));
        //        double rotatedYSprayerPosition = (SPRAYER_LOCATIONS[i, 0] * Math.Sin(-currentPosition.getYaw())) + (SPRAYER_LOCATIONS[i, 1] * Math.Cos(-currentPosition.getYaw()));

        //        double globalXSprayerPosition = currentPosition.getXPosition() + rotatedXSprayerPosition;
        //        double globalYSprayerPosition = currentPosition.getYPosition() + rotatedYSprayerPosition;

        //        sprayerGlobalPositions[i] = new Position(currentPosition.getTime(), currentPosition.getChangeInTime(), globalXSprayerPosition, globalYSprayerPosition, currentPosition.getYaw());
        //    }

        //    return sprayerGlobalPositions;
        //}
    }
}
