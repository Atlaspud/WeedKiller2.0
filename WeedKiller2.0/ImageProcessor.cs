﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class ImageProcessor
    {
        // Constants

        private const int IMAGE_HEIGHT = 1023;
        private const int IMAGE_WIDTH = 1279;
        private const int MORPHOLOGY_SIZE = 40;
        private const int BINARY_THRESHOLD = 5;
        private const int Y_DECIMATION = 10;
        private const double CONNECTION_THRESHOLD = 55;
        private static byte[, , ,] LUT;

        //Generate shadow/highlight LUT
        public static unsafe void loadLUT(Image<Bgr, byte> input, Image<Bgr, byte> output)
        {
            int height = input.Height;
            int width = input.Width;
            byte[, ,] inputData = input.Data;
            byte[, ,] outputData = output.Data;
            LUT = new byte[256, 256, 256, 3];

            fixed (byte* outputPointer = outputData)
            fixed (byte* inputPointer = inputData)
            fixed (byte* lutPointer = LUT)
                for (int i = 0; i < height * width; i++)
                {
                    int imageIndex = i * 3;
                    int lutIndex = *(inputPointer + imageIndex) * 196608 + *(inputPointer + imageIndex + 1) * 768 + *(inputPointer + imageIndex + 2) * 3;
                    *(lutPointer + lutIndex) = *(outputPointer + imageIndex);
                    *(lutPointer + lutIndex + 1) = *(outputPointer + imageIndex + 1);
                    *(lutPointer + lutIndex + 2) = *(outputPointer + imageIndex + 2);
                }
        }

        //Perform shadow/highlight with LUT
        public static unsafe Image<Bgr, byte> applyLUT(Image<Bgr, byte> input)
        {
            if (LUT == null) return null;

            int height = input.Height;
            int width = input.Width;

            byte[, ,] inputData = input.Data;
            byte[, ,] outputData = new byte[height, width, 3];

            fixed (byte* outputPointer = outputData)
            fixed (byte* inputPointer = inputData)
            fixed (byte* lutPointer = LUT)
                for (int i = 0; i < height * width; i++)
                {
                    int imageIndex = i * 3;
                    int lutIndex = *(inputPointer + imageIndex) * 196608 + *(inputPointer + imageIndex + 1) * 768 + *(inputPointer + imageIndex + 2) * 3;
                    *(outputPointer + imageIndex) = *(lutPointer + lutIndex);
                    *(outputPointer + imageIndex + 1) = *(lutPointer + lutIndex + 1);
                    *(outputPointer + imageIndex + 2) = *(lutPointer + lutIndex + 2);
                }

            return new Image<Bgr, byte>(outputData);
        }

        // Extract rectangular ROI region from larger image
        static public Image<Bgr, byte> extractROI(Image<Bgr, byte> input, Rectangle roi)
        {
            int inputHeight = input.Height;
            int inputWidth = input.Width;
            int outputHeight = roi.Height;
            int outputWidth = roi.Width;
            int x = roi.X;
            int y = roi.Y;

            byte[, ,] inputData = input.Data;
            byte[, ,] outputData = new byte[outputHeight, outputWidth, 3];
            
            for (int i = y; i < (y + outputHeight); i++)
            {
                for (int j = x; j < (x + outputWidth); j++)
                {
                    for (int n = 0; n < 3; n++)
                    {
                        outputData[(i - y), (j - x), n] = inputData[i, j, n];
                    }
                }
            }
            
            return new Image<Bgr, byte>(outputData);
        }

        static public Image<Gray, byte> extractROI(Image<Gray, byte> input, Rectangle roi)
        {
            int inputHeight = input.Height;
            int inputWidth = input.Width;
            int outputHeight = roi.Height;
            int outputWidth = roi.Width;
            int x = roi.X;
            int y = roi.Y;

            byte[, ,] inputData = input.Data;
            byte[, ,] outputData = new byte[outputHeight, outputWidth, 1];

            for (int i = y; i < (y + outputHeight); i++)
            {
                for (int j = x; j < (x + outputWidth); j++)
                {
                    outputData[(i - y), (j - x), 0] = inputData[i, j, 0];
                }
            }

            return new Image<Gray, byte>(outputData);
        }

        // Thresholds image to single out green colour

        static public Image<Gray, Byte> thresholdImage(Image<Bgr, Byte> image)
        {
            Image<Gray, Byte> outputImage = new Image<Gray, Byte>(IMAGE_WIDTH, IMAGE_HEIGHT);
            Image<Gray, Byte>[] channel = image.Split();
            outputImage = channel[1] - channel[2];
            outputImage._ThresholdBinary(new Gray(BINARY_THRESHOLD), new Gray(255));
            return outputImage;
        }

        // Clean up images using Morphological open and close

        static public Image<Gray, Byte> morphology(Image<Gray, Byte> image)
        {

            Mat kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(MORPHOLOGY_SIZE, MORPHOLOGY_SIZE), new Point(1, 1));
            image._MorphologyEx(Emgu.CV.CvEnum.MorphOp.Open, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            image._MorphologyEx(Emgu.CV.CvEnum.MorphOp.Close, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            return image;
        }

        // Label Connected Components

        static public List<List<int[]>> LabelConnectedComponents(List<int[]> components)
        {
            // Calculate the centroid of each window and perform a radial check with a threshold value to determine connection
            List<List<int[]>> connectedComponents = new List<List<int[]>>();
            for (int i = 0; i < components.Count(); i++)
            {
                int[] component = components[i];
                Boolean newConnection = true;
                for (int k = 0; k < connectedComponents.Count(); k++)
                {
                    if (connectedComponents[k].Contains(component))
                    {
                        newConnection = false;
                        break;
                    }
                }
                if (newConnection)
                {
                    connectedComponents.Add(new List<int[]> { component });
                }
                double[] componentCentroid = getCentroid(component);
                for (int j = i + 1; j < components.Count(); j++)
                {
                    int[] neighbour = components[j];
                    double[] neighbourCentroid = getCentroid(neighbour);
                    if (isConnected(componentCentroid, neighbourCentroid))
                    {
                        for (int k = 0; k < connectedComponents.Count(); k++)
                        {
                            if (connectedComponents[k].Contains(component) && !connectedComponents[k].Contains(neighbour))
                            {
                                connectedComponents[k].Add(neighbour);
                                break;
                            }
                        }
                    }
                }
            }
            return connectedComponents;
        }

        static private Boolean isConnected(double[] coordOne, double[] coordTwo)
        {
            double xDiff = coordOne[0] - coordTwo[0];
            double yDiff = coordOne[1] - coordTwo[1];
            if (CONNECTION_THRESHOLD >= Math.Sqrt(xDiff * xDiff + yDiff * yDiff))
            {
                return true;
            }
            return false;
        }

        static private double[] getCentroid(int[] coordinate)
        {
            return new double[] { coordinate[0] / 2, coordinate[1] / 2 };
        }

        static public Image<Gray, Byte> invertImage(Image<Gray, Byte> binaryMask)
        {
            // Convert image to 2D array
            Byte[, ,] maskData = binaryMask.Data;
            // Check for black pixel, change to black else change to white.
            for (int i = 0; i < binaryMask.Height; i += 1)
            {
                for (int j = 0; j < binaryMask.Width; j += 1)
                {
                    if (maskData[i, j, 0] == 0)
                    {
                        maskData[i, j, 0] = 255;
                    }
                    else
                    {
                        maskData[i, j, 0] = 0;
                    }
                }
            }

            return new Image<Gray, byte>(maskData);
        }

        // Search through image for white pixels

        static public List<int[]> findWindows(Image<Gray, Byte> binaryMask, int windowSize)
        {
            List<int[]> startingLocation = new List<int[]>();
            Byte[, ,] maskData = binaryMask.Data; // y,x structure
            for (int row = 0; row < binaryMask.Height; row += Y_DECIMATION)
            {
                for (int col = 0; col < binaryMask.Width; col += windowSize)
                {
                    if (maskData[row, col, 0] == 255)
                    {
                        int colMaxBack = col - windowSize;

                        while (col > 0 && col > colMaxBack && maskData[row, col, 0] == 255)
                        {
                            --col;
                        }
                        if (checkFit(col, row, maskData, windowSize, startingLocation))
                        {
                            //Image<Gray, byte> test = ImageProcessor.extractROI(binaryMask,new Rectangle(col,row,windowSize,windowSize));
                            //if (bruteForceCheck(test))
                            //{
                            int[] points = { col, row };
                            startingLocation.Add(points);
                            if (col > IMAGE_WIDTH) col = IMAGE_WIDTH;
                            //}
                        }
                        col += windowSize;
                    }
                }
            }
            return startingLocation;
        }

        // Check if window fits, assume it does, then check

        static private Boolean checkFit(int col, int row, Byte[, ,] maskData, int windowSize, List<int[]> startingLocation)
        {
            Boolean x12Fit = true;
            Boolean x22Fit = true;
            Boolean x21Fit = true;

            int windowBoundryX = col + windowSize;
            if (windowBoundryX > IMAGE_WIDTH) return false;
            int windowBoundryY = row + windowSize;
            if (windowBoundryY > IMAGE_HEIGHT) return false;
            int startingPointX = ++col;
            int startingPointY = row;

            // Check if intersects with other windows 
            Rectangle newRect = new Rectangle(new Point(col, row), new Size(windowSize, windowSize));
            foreach (int[] location in startingLocation)
            {
                Rectangle oldRect = new Rectangle(new Point(location[0], location[1]), new Size(windowSize, windowSize));
                if (oldRect.IntersectsWith(newRect))
                {
                    return false;
                }
            }


            // Check X12 corner of box
            for (int checkCol = startingPointX; checkCol < windowBoundryX; checkCol += 10)
            {
                if (checkCol > IMAGE_WIDTH || maskData[row, checkCol, 0] != 255)
                {
                    x12Fit = false;
                    break;
                }
            }

            if (x12Fit == false) return false;

            // Check X22 Corner of box
            for (int checkRow = startingPointY; checkRow < windowBoundryY; checkRow += 10)
            {
                if (checkRow > IMAGE_HEIGHT || maskData[checkRow, windowBoundryX, 0] != 255)
                {
                    x22Fit = false;
                    break;
                }
            }

            if (x22Fit == false) return false;

            // Check X21 Corner of box
            for (int checkCol = windowBoundryX; checkCol > col; checkCol -= 10)
            {
                if (checkCol < 0 || maskData[windowBoundryY, checkCol, 0] != 255)
                {
                    x21Fit = false;
                    break;
                }
            }

            if (x21Fit == false) return false;

            return true;
        }
    }
}
