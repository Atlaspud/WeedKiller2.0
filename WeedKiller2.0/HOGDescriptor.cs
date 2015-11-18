using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class HOGDescriptor
    {
        public enum DerivativeMask { CubicCorrected1D, Prewitt3D, Sobel3D };
        public enum BlockNormalisation { None, L1Norm, L2Norm };

        //Settings
        private int windowSize;
        private int blockSize;
        private double blockOverlap;
        private int cellSize;
        private int numberOfBins;
        private bool signedGradient;
        private bool downweightEdges;
        private bool gammaCorrection;
        private bool useColour;
        private int gaussianSmoothing;
        private BlockNormalisation blockNormalisation;
        private DerivativeMask derivativeMask;
        private bool dominantRotationAlignment;

        //Processing variables
        private int blockStride;
        private int blocksPerWindow;
        private int cellsPerBlock;
        private double[,,,] spatialWeights;
        private int[,,,] spatialBins;
        private ConvolutionKernelF[] derivativeMasks;
        int orientationRange;

        /// <summary>
        /// This constructor creates an instance of the HOGDescriptor class and performs overhead computations to reduce workload in computing descriptor vectors.
        /// </summary>
        public HOGDescriptor(
            int windowSize = 96,
            int cellSize = 96,
            int blockSize = 1,
            double blockOverlap = 0,
            int numberOfBins = 180,
            bool signedGradient = false,
            bool downweightEdges = false,
            bool gammaCorrection = true,
            bool useColour = false,
            int gaussianSmoothing = 5,
            bool dominantRotationAlignment = false,
            BlockNormalisation blockNormalisation = BlockNormalisation.L2Norm,
            DerivativeMask derivativeMask = DerivativeMask.Sobel3D)
        {
            //Initialise descriptor variables
            this.windowSize = windowSize;
            this.cellSize = cellSize;
            this.blockSize = blockSize;
            this.blockOverlap = blockOverlap;
            this.numberOfBins = numberOfBins;
            this.signedGradient = signedGradient;
            this.downweightEdges = downweightEdges;
            this.gammaCorrection = gammaCorrection;
            this.useColour = useColour;
            this.gaussianSmoothing = gaussianSmoothing;
            this.dominantRotationAlignment = dominantRotationAlignment;
            this.blockNormalisation = blockNormalisation;
            this.derivativeMask = derivativeMask;

            //Get orientation range
            if (signedGradient) this.orientationRange = 360;
            else this.orientationRange = 180;

            //Compute processing variables
            if (blockSize == 1)
            {
                this.blockStride = blockSize * cellSize;
                this.blocksPerWindow = (windowSize / blockStride) * (windowSize / blockStride);
            }
            else
            {
                if (blockOverlap == 0)
                {
                    this.blockStride = blockSize * cellSize;
                    this.blocksPerWindow = (windowSize / blockStride) * (windowSize / blockStride);
                }
                else
                {
                    this.blockStride = (int)(blockSize * cellSize * (1 - blockOverlap));
                    this.blocksPerWindow = (windowSize / blockStride - 1) * (windowSize / blockStride - 1);
                }
            }
            this.cellsPerBlock = blockSize * blockSize;

            //Pre-compute derivate masks
            derivativeMasks = getDerivativeMasks(derivativeMask);

            //Gaussian spatial window
            double[,] gaussianSpatialWindow = new double[0, 0];
            if (downweightEdges == true) gaussianSpatialWindow = getGaussianSpatialWindow(blockSize * cellSize, 0.5 * blockSize * cellSize);

            //Pre-compute spatial bin interpolation weighting
            spatialWeights = new double[blocksPerWindow, blockSize * cellSize, blockSize * cellSize, 4];
            spatialBins = new int[blocksPerWindow,blockSize * cellSize, blockSize * cellSize,4];
            int blockCount = 0;
            int difference = blockOverlap == 0 ? 0 : blockStride;
            for (int blockStartY = 0; blockStartY < (windowSize - difference); blockStartY += blockStride)
            {
                for (int blockStartX = 0; blockStartX < (windowSize - difference); blockStartX += blockStride)
                {
                    int blockEndY = blockStartY + blockSize * cellSize;
                    int blockEndX = blockStartX + blockSize * cellSize;
                    for (int y = blockStartY; y < blockEndY; y++)
                    {
                        for (int x = blockStartX; x < blockEndX; x++)
                        {
                            //Get block specific indices
                            int i = x - blockStartX;
                            int j = y - blockStartY;

                            //Calculate spatial bin interpolation weighting
                            double cellX = (j + 0.5f) / cellSize - 0.5f;
                            double cellY = (i + 0.5f) / cellSize - 0.5f;
                            int icellX0 = (int)Math.Floor(cellX);
                            int icellY0 = (int)Math.Floor(cellY);
                            int icellX1 = icellX0 + 1, icellY1 = icellY0 + 1;
                            cellX -= icellX0;
                            cellY -= icellY0;

                            if ((uint)icellX0 < (uint)blockSize && (uint)icellX1 < (uint)blockSize)
                            {
                                if ((uint)icellY0 < (uint)blockSize && (uint)icellY1 < (uint)blockSize)
                                {
                                    spatialBins[blockCount,i,j,0] = icellX0 * blockSize + icellY0;
                                    spatialWeights[blockCount, i, j, 0] = (1.0 - cellX) * (1.0 - cellY);
                                    spatialBins[blockCount, i, j, 1] = icellX1 * blockSize + icellY0;
                                    spatialWeights[blockCount, i, j, 1] = cellX * (1.0 - cellY);
                                    spatialBins[blockCount, i, j, 2] = icellX0 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 2] = (1.0 - cellX) * cellY;
                                    spatialBins[blockCount, i, j, 3] = icellX1 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 3] = cellX * cellY;
                                }
                                else
                                {
                                    if ((uint)icellY0 < (uint)blockSize)
                                    {
                                        icellY1 = icellY0;
                                        cellY = 1.0 - cellY;
                                    }
                                    spatialBins[blockCount, i, j, 0] = icellX0 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 0] = (1.0 - cellX) * cellY;
                                    spatialBins[blockCount, i, j, 1] = icellX1 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 1] = cellX * cellY;
                                    spatialBins[blockCount, i, j, 2] = 0;
                                    spatialWeights[blockCount, i, j, 2] = 0;
                                    spatialBins[blockCount, i, j, 3] = 0;
                                    spatialWeights[blockCount, i, j, 3] = 0;
                                }
                            }
                            else
                            {
                                if ((uint)icellX0 < (uint)blockSize)
                                {
                                    icellX1 = icellX0;
                                    cellX = 1.0 - cellX;
                                }

                                if ((uint)icellY0 < (uint)blockSize && (uint)icellY1 < (uint)blockSize)
                                {
                                    spatialBins[blockCount, i, j, 0] = icellX1 * blockSize + icellY0;
                                    spatialWeights[blockCount, i, j, 0] = cellX * (1.0 - cellY);
                                    spatialBins[blockCount, i, j, 1] = icellX1 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 1] = cellX * cellY;
                                    spatialBins[blockCount, i, j, 2] = 0;
                                    spatialWeights[blockCount, i, j, 2] = 0;
                                    spatialBins[blockCount, i, j, 3] = 0;
                                    spatialWeights[blockCount, i, j, 3] = 0;
                                }
                                else
                                {
                                    if ((uint)icellY0 < (uint)blockSize)
                                    {
                                        icellY1 = icellY0;
                                        cellY = 1.0 - cellY;
                                    }
                                    spatialBins[blockCount, i, j, 0] = icellX1 * blockSize + icellY1;
                                    spatialWeights[blockCount, i, j, 0] = cellX * cellY;
                                    spatialBins[blockCount, i, j, 1] = 0;
                                    spatialWeights[blockCount, i, j, 1] = 0;
                                    spatialBins[blockCount, i, j, 2] = 0;
                                    spatialWeights[blockCount, i, j, 2] = 0;
                                    spatialBins[blockCount, i, j, 3] = 0;
                                    spatialWeights[blockCount, i, j, 3] = 0;
                                }
                            }

                            //Apply gaussian spatial window weighting
                            if (downweightEdges)
                            {
                                double w = gaussianSpatialWindow[i, j];
                                spatialWeights[blockCount, i, j, 0] *= w;
                                spatialWeights[blockCount, i, j, 1] *= w;
                                spatialWeights[blockCount, i, j, 2] *= w;
                                spatialWeights[blockCount, i, j, 3] *= w;
                            }
                        }
                    }
                    blockCount++;
                }
            }
        }

        /// <summary>
        /// Computes a HOG descriptor vector from the given image, with the settings from the constructor.
        /// </summary>
        /// <param name="image">The image to be described. Must be (windowSize x windowSize).</param>
        /// <returns></returns>
        public double[] compute(Image<Bgr, byte> image, Image<Gray, byte> mask = null)
        {
            //Optional: Apply sqrt gamma correction
            if (gammaCorrection) image._GammaCorrect(0.5);

            //Optional: Apply gaussian smoothing
            if (gaussianSmoothing > 0) image._SmoothGaussian(gaussianSmoothing);

            //Calculate image derivatives
            float[, ,] dxImageData;
            float[, ,] dyImageData;
            if (useColour)
            {
                dxImageData = image.Convolution(derivativeMasks[0]).Data;
                dyImageData = image.Convolution(derivativeMasks[1]).Data;
            }
            else
            {
                dxImageData = image.Convert<Gray, byte>().Convolution(derivativeMasks[0]).Data;
                dyImageData = image.Convert<Gray, byte>().Convolution(derivativeMasks[1]).Data;
            }

            //Get mask data
            if (mask == null) mask = new Image<Gray, byte>(windowSize, windowSize, new Gray(255));
            byte[, ,] maskData = mask.Data;

            //Calculate and store gradient magnitude and orientation for each pixel
            double[,] orientations = new double[windowSize, windowSize];
            double[,] magnitudes = new double[windowSize, windowSize];
            int channels = dxImageData.GetLength(2);

            double[] orientationHistogram = new double[orientationRange];

            for (int i = 0; i < windowSize; i++)
            {
                for (int j = 0; j < windowSize; j++)
                {
                    double orientation = 0;
                    double magnitude = 0;
                    double largestMagnitude = 0;
                    if (maskData[i, j, 0] == 255)
                    {
                        for (int channel = 0; channel < channels; channel++)
                        {
                            float dx = dxImageData[i, j, channel];
                            float dy = dyImageData[i, j, channel];
                            magnitude = Math.Sqrt(dx * dx + dy * dy);
                            if (magnitude > largestMagnitude)
                            {
                                largestMagnitude = magnitude;
                                orientation = Math.Atan2(dy, dx) * 180.0 / Math.PI; //[-180,180]
                                //Convert to signed or unsigned orientation
                                if (orientation < 0)
                                {
                                    orientation += orientationRange;//[0,180) or [0,360)
                                }
                            }
                        }
                        magnitudes[i, j] = largestMagnitude;
                        orientations[i, j] = orientation;
                    }
                }
            }

            //Initialise histogram
            double[] descriptor = new double[blocksPerWindow * cellsPerBlock * numberOfBins];

            //Divide image into overlapping blocks
            int blockCount = 0;
            int binStep = orientationRange / numberOfBins;

            if (blocksPerWindow == 1 && cellsPerBlock == 1)
            {
                //Perform bilinear vote interpolation
                for (int y = 0; y < windowSize; y++)
                {
                    for (int x = 0; x < windowSize; x++)
                    {
                        //Calculate orientation bin interpolation weighting
                        double orientation = orientations[x, y];
                        double[] orientationWeights = new double[2];
                        int[] orientationBins = new int[2];
                        double angle = orientations[x, y] / binStep - 0.5;
                        int angleFloor = (int)Math.Floor(angle);
                        angle -= angleFloor;

                        orientationWeights[0] = 1.0 - angle;
                        orientationWeights[1] = angle;

                        if (angleFloor < 0)
                        {
                            angleFloor += numberOfBins;
                        }
                        else if (angleFloor >= numberOfBins)
                        {
                            angleFloor -= numberOfBins;
                        }

                        orientationBins[0] = angleFloor;
                        angleFloor++;
                        angleFloor &= angleFloor < numberOfBins ? -1 : 0;
                        orientationBins[1] = angleFloor;

                        for (int n = 0; n < 2; n++)
                        {
                            descriptor[orientationBins[n]] += (double)(magnitudes[x, y] * orientationWeights[n]);
                        }
                    }
                }
            }
            else
            {
                //Perform trilinear histogram vote interpolation for every cell in each block
                int difference = blockOverlap == 0 ? 0 : blockStride;
                for (int blockStartY = 0; blockStartY < (windowSize - difference); blockStartY += blockStride)
                {
                    for (int blockStartX = 0; blockStartX < (windowSize - difference); blockStartX += blockStride)
                    {
                        int blockEndY = blockStartY + blockSize * cellSize;
                        int blockEndX = blockStartX + blockSize * cellSize;
                        for (int y = blockStartY; y < blockEndY; y++)
                        {
                            for (int x = blockStartX; x < blockEndX; x++)
                            {
                                //Get block specific indices
                                int i = x - blockStartX;
                                int j = y - blockStartY;

                                //Calculate orientation bin interpolation weighting
                                double orientation = orientations[x, y];
                                double[] orientationWeights = new double[2];
                                int[] orientationBins = new int[2];
                                double angle = orientations[x, y] / binStep - 0.5;
                                int angleFloor = (int)Math.Floor(angle);
                                angle -= angleFloor;

                                orientationWeights[0] = 1.0 - angle;
                                orientationWeights[1] = angle;

                                if (angleFloor < 0)
                                {
                                    angleFloor += numberOfBins;
                                }
                                else if (angleFloor >= numberOfBins)
                                {
                                    angleFloor -= numberOfBins;
                                }

                                orientationBins[0] = angleFloor;
                                angleFloor++;
                                angleFloor &= angleFloor < numberOfBins ? -1 : 0;
                                orientationBins[1] = angleFloor;

                                //Perform trilinear interpolation
                                for (int k = 0; k < 4; k++)
                                {
                                    for (int n = 0; n < 2; n++)
                                    {
                                        descriptor[blockCount * cellsPerBlock * numberOfBins + spatialBins[blockCount, i, j, k] * numberOfBins + orientationBins[n]] += (double)(magnitudes[x, y] * spatialWeights[blockCount, i, j, k] * orientationWeights[n]);
                                    }
                                }
                            }
                        }
                        blockCount++;
                    }
                }
            }

            //Block normalisation
            if (blockNormalisation != BlockNormalisation.None)
            {
                for (int block = 0; block < blocksPerWindow; block++)
                {
                    double sum = 0;
                    for (int cell = 0; cell < cellsPerBlock; cell++)
                    {
                        for (int bin = 0; bin < numberOfBins; bin++)
                        {
                            int index = block * cellsPerBlock * numberOfBins + cell * numberOfBins + bin;
                            double val = descriptor[index];
                            if (blockNormalisation == BlockNormalisation.L1Norm)
                            {
                                sum += val;
                            }
                            else // L2Norm
                            {

                                sum += val * val;
                            }
                        }
                    }
                    double scale = 0;
                    if (blockNormalisation == BlockNormalisation.L1Norm)
                    {
                        scale = 1.0 / (sum + numberOfBins * cellsPerBlock * 0.1);
                    }
                    else //L2Norm
                    {
                        scale = 1.0 / (Math.Sqrt(sum) + numberOfBins * cellsPerBlock * 0.1);
                    }
                    for (int cell = 0; cell < cellsPerBlock; cell++)
                    {
                        for (int bin = 0; bin < numberOfBins; bin++)
                        {
                            int index = block * cellsPerBlock * numberOfBins + cell * numberOfBins + bin;
                            descriptor[index] *= (double)scale;
                        }
                    }
                }
            }

            //Dominant rotation alignment
            if (dominantRotationAlignment)
            {
                double[] clonedDescriptor = (double[])descriptor.Clone();
                for (int block = 0; block < blocksPerWindow; block++)
                {
                    for (int cell = 0; cell < cellsPerBlock; cell++)
                    {
                        int cellIndex = block * cellsPerBlock * numberOfBins + cell * numberOfBins;
                        int dominantBin = 0;
                        double dominantValue = 0;
                        double binValue = 0;
                        for (int bin = 0; bin < numberOfBins; bin++)
                        {
                            binValue = descriptor[cellIndex + bin];
                            if (binValue > dominantValue)
                            {
                                dominantValue = binValue;
                                dominantBin = bin;
                            }
                        }
                        for (int bin = 0; bin < numberOfBins; bin++)
                        {
                            descriptor[cellIndex + bin] = clonedDescriptor[cellIndex + ((dominantBin + bin) % numberOfBins)];
                        }
                    }
                }
            }
            return descriptor;
        }

        /// <summary>
        /// Visually represents the HOG descriptor via star diagrams for each cell.
        /// </summary>
        /// <param name="image">The image from which the HOG descriptor was previously generated.</param>
        /// <param name="descriptor">The descriptor returned via the HOG computation.</param>
        /// <param name="scaleFactor">The scale factor determines how large the original image is resized in order to be more visually pleasing.</param>
        /// <returns></returns>
        public Image<Bgr,byte> visualise(Image<Bgr,byte> image, float[] descriptor, int scaleFactor)
        {
            if (!useColour) image = image.Convert<Gray, byte>().Convert<Bgr, byte>();
            Image<Bgr, byte> visualisation = image.Resize(scaleFactor, Emgu.CV.CvEnum.Inter.Cubic);
            double binStep = 0;
            if (signedGradient) binStep = 2 * Math.PI / numberOfBins;
            else binStep = Math.PI / numberOfBins;
            int uniqueCellsPerWindow = (int)Math.Pow(windowSize/cellSize,2);

            //Initialise average and counter variables
            double[,,] gradientMagnitudes = new double[windowSize / cellSize, windowSize / cellSize, numberOfBins];
            int[,] cellUpdateCounter = new int[windowSize / cellSize, windowSize / cellSize];
            for (int i = 0; i < windowSize / cellSize; i++)
            {
                for (int j = 0; j < windowSize / cellSize; j++)
                {
                    cellUpdateCounter[i,j] = 0;
                    for (int k = 0; k < numberOfBins; k++)
                    {
                        gradientMagnitudes[i,j,k] = 0.0;
                    }
                }
            }

            //Average gradient magnitudes for each unique cell
            int descriptorIndex = 0;
            if (blockSize != 1)
            {
                for (int blockX = 0; blockX < (windowSize / cellSize - 1); blockX += blockStride / cellSize < 1 ? 1 : blockStride / cellSize)
                {
                    for (int blockY = 0; blockY < (windowSize / cellSize - 1); blockY += blockStride / cellSize < 1 ? 1 : blockStride / cellSize)
                    {
                        for (int cell = 0; cell < 4; cell++)
                        {
                            int cellX = blockX;
                            int cellY = blockY;
                            if (cell == 1) cellY++;
                            if (cell == 2) cellX++;
                            if (cell == 3)
                            {
                                cellX++;
                                cellY++;
                            }

                            for (int bin = 0; bin < numberOfBins; bin++)
                            {
                                double gradientMagnitude = descriptor[descriptorIndex];
                                descriptorIndex++;

                                gradientMagnitudes[cellX, cellY, bin] += gradientMagnitude;
                            }

                            cellUpdateCounter[cellX, cellY]++;
                        }
                    }
                }

                for (int cellX = 0; cellX < windowSize / cellSize; cellX++)
                {
                    for (int cellY = 0; cellY < windowSize / cellSize; cellY++)
                    {
                        for (int bin = 0; bin < numberOfBins; bin++)
                        {
                            gradientMagnitudes[cellX, cellY, bin] /= (double)cellUpdateCounter[cellX, cellY];
                        }
                    }
                }
            }
            else
            {
                for (int bin = 0; bin < numberOfBins; bin++)
                {
                    double gradientMagnitude = descriptor[descriptorIndex];
                    descriptorIndex++;

                    gradientMagnitudes[0, 0, bin] += gradientMagnitude;
                }
            }

            for (int cellX = 0; cellX < windowSize / cellSize; cellX++)
            {
                for (int cellY = 0; cellY < windowSize / cellSize; cellY++)
                {
                    int drawX = cellX * cellSize;
                    int drawY = cellY * cellSize;

                    double mx = drawX + cellSize / 2.0;
                    double my = drawY + cellSize / 2.0;

                    //visual.Draw(new Rectangle(drawX * scaleFactor, drawY * scaleFactor, scaleFactor * cellSize, scaleFactor * cellSize), new Bgr(0, 0, 255), 1);

                    for (int bin = 0; bin < numberOfBins; bin++)
                    {
                        double currentGradientMagnitude = gradientMagnitudes[cellX, cellY, bin];
                        if (currentGradientMagnitude > 0)
                        {
                            double currentGradientOrientation = bin * binStep + binStep / 2;

                            double xVectorComponent = Math.Cos(currentGradientOrientation + Math.PI / 2);
                            double yVectorComponent = Math.Sin(currentGradientOrientation + Math.PI / 2);

                            double maximumVectorLength = cellSize / 2;
                            double visualScale = 3.0;
                            double xVectorScale = xVectorComponent * currentGradientMagnitude * maximumVectorLength * visualScale;
                            double yVectorScale = yVectorComponent * currentGradientMagnitude * maximumVectorLength * visualScale;
                            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                            if (signedGradient)
                            {
                                x1 = mx;
                                y1 = my;
                                x2 = mx - xVectorScale;
                                y2 = my - yVectorScale;
                            }
                            else
                            {
                                x1 = mx - xVectorScale;
                                y1 = my - yVectorScale;
                                x2 = mx + xVectorScale;
                                y2 = my + yVectorScale;
                            }
                            Point start = new Point((int)Math.Round(x1 * scaleFactor, MidpointRounding.AwayFromZero), (int)Math.Round(y1 * scaleFactor, MidpointRounding.AwayFromZero));
                            Point end = new Point((int)Math.Round(x2 * scaleFactor, MidpointRounding.AwayFromZero), (int)Math.Round(y2 * scaleFactor, MidpointRounding.AwayFromZero));
                            visualisation.Draw(new LineSegment2D(start,end), new Bgr(0, 0, 255), 2);
                        }
                    }
                }
            }
            return visualisation;
        }

        /// <summary>
        /// Returns a 2D Gaussian spatial window of specified size and standard deviation.
        /// </summary>
        /// <param name="size">Size of the window.</param>
        /// <param name="sigma">Standard deviation of the Gaussian distribution.</param>
        /// <returns></returns>
        public static double[,] getGaussianSpatialWindow(int size, double sigma = -1)
        {
            sigma = sigma >= 0 ? sigma : size / 4.0;
            double[,] gaussianWindow = new double[size, size];
            double scale = 1.0 / (sigma * sigma * 2);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    double x = i - (size * 0.5);
                    double y = j - (size * 0.5);
                    //if (size % 2 == 0)
                    //{
                    //    x += 0.5;
                    //    y += 0.5;
                    //}
                    gaussianWindow[i, j] = Math.Exp(-1 * scale * (x * x + y * y));
                }
            }
            return gaussianWindow;
        }

        /// <summary>
        /// Returns one of the designated derivative masks in the form of a ConvolutionKernelF.
        /// </summary>
        /// <param name="type">Derivative mask type. Includes 1D (centred, uncentred and cubic-corrected), 2D (Roberts cross) and 3D (Sobel, Prewitt).</param>
        /// <returns></returns>
        public static ConvolutionKernelF[] getDerivativeMasks(DerivativeMask type)
        {
            float[,] dxMask = new float[0, 0];
            float[,] dyMask = new float[0, 0];

            switch (type)
            {
                case DerivativeMask.CubicCorrected1D:
                    dxMask = new float[1, 5]; dyMask = new float[5, 1];
                    dxMask[0, 0] =  1; dyMask[0, 0] =  1;
                    dxMask[0, 1] = -8; dyMask[1, 0] = -8;
                    dxMask[0, 2] =  0; dyMask[2, 0] =  0;
                    dxMask[0, 3] =  8; dyMask[3, 0] =  8;
                    dxMask[0, 4] = -1; dyMask[4, 0] = -1;
                    break;

                case DerivativeMask.Prewitt3D:
                    dxMask = new float[3, 3]; dyMask = new float[3, 3];
                    dxMask[0, 0] = -1; dyMask[0, 0] = -1;
                    dxMask[0, 1] =  0; dyMask[0, 1] = -1;
                    dxMask[0, 2] =  1; dyMask[0, 2] = -1;
                    dxMask[1, 0] = -1; dyMask[1, 0] =  0;
                    dxMask[1, 1] =  0; dyMask[1, 1] =  0;
                    dxMask[1, 2] =  1; dyMask[1, 2] =  0;
                    dxMask[2, 0] = -1; dyMask[2, 0] =  1;
                    dxMask[2, 1] =  0; dyMask[2, 1] =  1;
                    dxMask[2, 2] =  1; dyMask[2, 2] =  1;
                    break;

                case DerivativeMask.Sobel3D:
                    dxMask = new float[3, 3]; dyMask = new float[3, 3];
                    dxMask[0, 0] = -1; dyMask[0, 0] = -1;
                    dxMask[0, 1] =  0; dyMask[0, 1] = -2;
                    dxMask[0, 2] =  1; dyMask[0, 2] = -1;
                    dxMask[1, 0] = -2; dyMask[1, 0] =  0;
                    dxMask[1, 1] =  0; dyMask[1, 1] =  0;
                    dxMask[1, 2] =  2; dyMask[1, 2] =  0;
                    dxMask[2, 0] = -1; dyMask[2, 0] =  1;
                    dxMask[2, 1] =  0; dyMask[2, 1] =  2;
                    dxMask[2, 2] =  1; dyMask[2, 2] =  1;
                    break;
            }

            return new ConvolutionKernelF[] { new ConvolutionKernelF(dxMask), new ConvolutionKernelF(dyMask) };
        }

        /// <summary>
        /// Static and outdated implementation of HOG descriptor calculator. It does not account for unsigned gradients, colour images, derivative mask selection, or local contrast normalization.
        /// </summary>
        /// <param name="input">Input image must be grayscale.</param>
        /// <param name="binSize">Gradient orientation bin size between [1,360].</param>
        /// <returns></returns>
        public static Dictionary<String, double[]> legacyHOG(Image<Gray, Byte> input, int binSize = 1)
        {
            Image<Gray, float> dx = input.Sobel(1, 0, 3);
            Image<Gray, float> dy = input.Sobel(0, 1, 3);
            float[, ,] dxData = dx.Data;
            float[, ,] dyData = dy.Data;
            double[] intensities = new double[360 / binSize];
            double[] orientations = new double[360 / binSize];
            double orientation = 0;
            double intensity = 0;
            double totalIntensity = 0;

            for (int i = input.Height - 1; i >= 0; i--)
            {
                for (int j = input.Width - 1; j >= 0; j--)
                {
                    // Calculate gradient orientation and intensity at pixel (i,j)
                    orientation = Math.Atan2((double)dyData[i, j, 0], (double)(dxData[i, j, 0])) * 180.0 / Math.PI + 180.0;
                    intensity = Math.Sqrt(Math.Pow((double)dxData[i, j, 0], 2) + Math.Pow((double)dyData[i, j, 0], 2));

                    // Accumulate the total gradient intensity
                    totalIntensity += intensity;

                    // Accumulate orientation-specific gradient intensity
                    for (int k = 0; k < (360 / binSize); k++)
                    {
                        orientations[k] = k * binSize;
                        if (orientation < 0.0 || orientation > 360.0)
                        {
                            //Unacceptable orientation calculated
                        }
                        else if (orientation >= (double)(k * binSize) && orientation < (double)((k + 1) * binSize))
                        {
                            intensities[k] += intensity;
                            break;
                        }
                        else if (orientation == 360.0)
                        {
                            intensities[0] += intensity;
                            break;
                        }
                    }
                }
            }

            //Normalise histogram of oriented gradients
            Dictionary<String, double[]> histogram = new Dictionary<String, double[]>(2)
            {
                { "intensity", new double[360 / binSize] },
                { "orientation", new double[360 / binSize] }
            };

            int maxIndex = Array.IndexOf(intensities, intensities.Max());
            for (int i = 0; i < (360 / binSize); i++)
            {
                //Save orientation
                histogram["orientation"][i] = orientations[i];

                //Perform intensity normalisation and rotation normalisation by circular-shift of orientation
                histogram["intensity"][i] = intensities[(maxIndex + i) % (360 / binSize)] / totalIntensity;
            }
            return histogram;
        }
    }
}
