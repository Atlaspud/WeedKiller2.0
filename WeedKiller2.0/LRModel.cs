using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class LRModel
    {
        //Constants

        //Statics


        //Instances
        double[] mu;
        double[] sigma;
        double[] theta;

        //Model constructors
        public LRModel(string filename)
        {

            string text = File.ReadAllText(filename);
            string[] lines = text.Split('\n');
            int count = 0;
            int rows = 0;
            int cols = 0;
            foreach (string line in lines)
            {
                string[] items = line.Split(',');
                if (items[0] == "mu")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    mu = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            mu[j] = double.Parse(items[j]);
                        }
                    }
                }
                if (items[0] == "sigma")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    sigma = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            sigma[j] = double.Parse(items[j]);
                        }
                    }
                }
                if (items[0] == "theta")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    theta = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            theta[j] = double.Parse(items[j]);
                        }
                    }
                }
                count++;
            }
        }

        public LRModel(double[] mu, double[] sigma, double[] theta)
        {
            this.mu = mu;
            this.sigma = sigma;
            this.theta = theta;
        }

        //Train the model
        public void train()
        {

        }

        //Compute categorical probability output for given input vector
        public double[] compute(double[] input)
        {
            return new double[2];
        }

        /// <summary>
        /// Maps the two input feature scalars to a polynomial feature vector of the given degree.
        /// </summary>
        /// <param name="x">Two-element input vector representing one sample of a two feature descriptor.
        /// 
        /// i.e. the number of predicted target and non-target windows in an image).</param>
        /// <returns></returns>
        public double[] mapFeatures(double[] x, int degree)
        {
            int dimension = 1 + degree * (degree + 3) / 2;
            double[] output = new double[dimension];
            output[0] = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < (i + 2); j++)
                {
                    output[i * (i + 2) + j + 1] = Math.Pow(x[0], (i + 1) - j) * Math.Pow(x[1], j);
                }
            }
            return output;
        }

        /// <summary>
        /// Maps the two input feature vectors to polynomial features (X1, X2, X1^2, X2^2, X1*X2, X1^2*X2, etc) given the polynomial degree.
        /// 
        /// Note that input features must be of equal size.
        /// 
        /// </summary>
        /// <param name="x">First input vector.</param>
        /// <param name="y"> Second input vector.
        /// <param name="degree">The degree of the polynomial.</param>
        /// <returns></returns>
        public double[,] mapFeatures(double[] x1, double[] x2, int degree)
        {
            int length = x1.Length;
            int dimension = 1 + degree * (degree + 3) / 2;
            double[,] output = new double[length,  dimension];
            for (int i = 0; i < length; i++)
            {
                output[i, 0] = 1;
            }
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < (i + 2); j++)
                {
                    for (int k = 0; k < length; k++)
                    {
                        output[k, i * (i + 2) + j + 1] = Math.Pow(x1[k], (i + 1) - j) * Math.Pow(x2[k], j);
                    }
                }
            }
            return output;
        }

        private void calculateMeanAndStandardDeviation(double[,] X)
        {
            int length = X.GetLength(0);
            int dimension = X.GetLength(1);
            double[] mu = new double[dimension];
            double[] sigma = new double[dimension];

            //Initialise empty mean and standard deviation vectors
            for (int i = 0; i < dimension; i++)
            {
                mu[i] = 0;
                sigma[i] = 0;
            }

            //Calculate mean
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    mu[i] += X[j, i];
                }
                mu[i] /= length;
            }

            //Calculate standard deviation
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    double val = X[j, i] - mu[i];
                    sigma[i] += val * val;
                }
                sigma[i] = Math.Sqrt(sigma[i] / (length - 1));
            }
            this.mu = mu;
            this.sigma = sigma;
        }

        /// <summary>
        /// Returns a normalized version of the input matrix X where the mean value of each feature is 0 and the standard deviation is 1.
        /// This is often a good preprocessing step to do when working with learning algorithms.
        /// </summary>
        /// <returns></returns>
        public double[,] standardise(double[,] X)
        {
            int length = X.GetLength(0);
            int dimension = X.GetLength(1);
            double[,] Xnorm = new double[length, dimension];

            //Normalise by subtracting mean and dividing by standard deviation
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < dimension; j++)
                {
                    if (mu[j] == 1 && sigma[j] == 0) Xnorm[i, j] = 1;
                    else Xnorm[i, j] = (X[i, j] - mu[j]) / sigma[j];
                }
            }
            return Xnorm;
        }

        public double[] standardise(double[] x)
        {
            int dimension = x.Length;
            double[] xNorm = new double[dimension];
            xNorm[0] = 1;

            for (int i = 1; i < dimension; i++)
            {
                xNorm[i] = (x[i] - mu[i]) / sigma[i];
            }
            return xNorm;
        }

        public double[] minimiseCost(int lambda, double[,] X, double[] y, double[] initialTheta, int maxIterations, bool useGradient)
        {
            return new double[3];
        }

        public int[] predict(double[,] X)
        {
            int length = X.GetLength(0);
            int dimension = X.GetLength(1);
            int[] prediction = new int[length];
            double[] score = new double[length];
            for (int i = 0; i < length; i++)
            {
                score[i] = 0;
                for (int j = 0; j < dimension; j++)
                {
                    score[i] += theta[j] * X[i, j];
                }
                prediction[i] = score[i] > 0 ? 1 : 0;
            }
            return prediction;
        }

        public Prediction predict(double[] x)
        {
            int dimension = theta.GetLength(0);
            double score = 0;
            for (int i = 0; i < dimension; i++)
            {
                score += theta[i] * x[i];
            }
            bool label = score > 0 ? true : false;
            return new Prediction(label, score);
        }

        /// <summary>
        /// Sigmoid function for scalar input/output.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private double sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-1 * x));
        }

        /// <summary>
        /// Sigmoid function for vector input/output.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private double[] sigmoid(double[] x)
        {
            int length = x.Length;
            double[] y = new double[length];
            for (int i = 0; i < length; i++)
            {
                y[i] = sigmoid(x[i]);
            }
            return y;
        }

        /// <summary>
        /// Sigmoid function for matrix input/output;
        /// </summary>
        /// <param name="X"></param>
        /// <returns></returns>
        private double[,] sigmoid(double[,] X)
        {
            int length = X.GetLength(0);
            int dimension = X.GetLength(1);
            double[,] Y = new double[length, dimension];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < dimension; j++)
                {
                    Y[i, j] = sigmoid(X[i, j]);
                }
            }
            return Y;
        }
    }
}
