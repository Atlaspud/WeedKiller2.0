using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WeedKiller2._0
{
    class SVMModel
    {
        double[] alpha;
        double[] supportVectorLabels;
        double[][] supportVectors;
        double bias;
        double scale;
        double slope;
        double intercept;

        public SVMModel(string filename)
        {
            string text = File.ReadAllText(filename);
            string[] lines = text.Split('\n');
            int count = 0;
            int rows = 0;
            int cols = 0;
            foreach (string line in lines)
            {
                string[] items = line.Split(',');
                if (items[0] == "alpha")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    alpha = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            alpha[j] = double.Parse(items[j]);
                        }
                    }
                }
                if (items[0] == "supportVectors")
                {
                    int rowCount = count;
                    rows = int.Parse(items[1]);
                    cols = int.Parse(items[2]);
                    supportVectors = new double[rows][];
                    for (int i = 0; i < rows; i++)
                    {
                        supportVectors[i] = new double[cols];
                        items = lines[rowCount + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            supportVectors[i][j] = double.Parse(items[j]);
                        }
                        rowCount++;
                    }
                }
                if (items[0] == "supportVectorLabels")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    supportVectorLabels = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            supportVectorLabels[j] = double.Parse(items[j]);
                        }
                    }
                }
                if (items[0] == "bias")
                {
                    bias = double.Parse(lines[count + 1].Split(',')[0]);
                }
                if (items[0] == "scale")
                {
                    scale = double.Parse(lines[count + 1].Split(',')[0]);
                }
                if (items[0] == "slope")
                {
                    slope = double.Parse(lines[count + 1].Split(',')[0]);
                }
                if (items[0] == "intercept")
                {
                    intercept = double.Parse(lines[count + 1].Split(',')[0]);
                }
                count++;
            }
        }

        public SVMModel(double[] alpha, double bias, double scale, double[][] supportVectors, double[] supportVectorLabels, double slope, double intercept)
        {
            this.alpha = alpha;
            this.bias = bias;
            this.scale = scale;
            this.supportVectors = supportVectors;
            this.supportVectorLabels = supportVectorLabels;
            this.slope = slope;
            this.intercept = intercept;
        }

        public unsafe Prediction predict(double[] x)
        {
            //z = Σ (alpha_i * supportVectorLabel_i * G(supportVector_i, x)) + bias;
            //label = 1 if z > 0
            //      = 0 otherwise
            double score = bias;
            for (int i = 0; i < alpha.Length; i++)
            {
                score += alpha[i] * supportVectorLabels[i] * gaussianKernel(x, supportVectors[i]);
            }
            bool isTarget = score > 0 ? true : false;
            double probability = computeProbability(score);
            return new Prediction(isTarget, score, probability);
        }

        private unsafe double gaussianKernel(double[] x1, double[] x2)
        {
            //G(x1, x2) = exp(-||x1 - x2||^2)
            double normDifference = 0;
            for (int i = 0; i < x1.Length; i++)
            {
                double val = (x1[i] - x2[i]) / scale;
                normDifference += val * val;
            }
            return Math.Exp(-1.0 * normDifference);
        }

        private double computeProbability(double score)
        {
            return 1 / (1 + Math.Exp(slope * score + intercept));
        }
    }
}
