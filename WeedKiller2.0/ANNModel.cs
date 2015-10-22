using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class ANNModel
    {
        //Settings
        int nodes;
        int labels;
        double[][] theta1;
        double[] theta2;


        /// <summary>
        /// ANNModel constructor from the given filename.
        /// </summary>
        /// <param name="filename"></param>
        public ANNModel(string filename)
        {
            string text = File.ReadAllText(filename);
            string[] lines = text.Split('\n');
            int count = 0;
            int rows = 0;
            int cols = 0;
            foreach (string line in lines)
            {
                string[] items = line.Split(',');
                if (items[0] == "nodes")
                {
                    nodes = int.Parse(lines[count + 1].Split(',')[0]);
                }
                if (items[0] == "labels")
                {
                    labels = int.Parse(lines[count + 1].Split(',')[0]);
                }
                if (items[0] == "theta1")
                {
                    int rowCount = count;
                    rows = int.Parse(items[1]);
                    cols = int.Parse(items[2]);
                    theta1 = new double[rows][];
                    for (int i = 0; i < rows; i++)
                    {
                        theta1[i] = new double[cols];
                        items = lines[rowCount + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            theta1[i][j] = double.Parse(items[j]);
                        }
                        rowCount++;
                    }
                }
                if (items[0] == "theta2")
                {
                    rows = int.Parse(items[1]); //=1
                    cols = int.Parse(items[2]);
                    theta2 = new double[cols];
                    for (int i = 0; i < rows; i++)
                    {
                        items = lines[count + 1].Split(',');
                        for (int j = 0; j < cols; j++)
                        {
                            theta2[j] = double.Parse(items[j]);
                        }
                    }
                }
                count++;
            }
        }

        ///// <summary>
        ///// ANNModel constructor from the given settings.
        ///// </summary>
        //public ANNModel()
        //{
        //
        //}

        /// <summary>
        /// Predict the binary classification result for the given feature vector.
        /// </summary>
        /// <returns></returns>
        public Prediction predict(double[] x)
        {
            x = CustomMath.appendScalarToFront(x,1);
            double[] h = CustomMath.vectorByMatrix(x, theta1);
            h = CustomMath.sigmoid(h);
            h = CustomMath.appendScalarToFront(h,1);
            double score = CustomMath.dotProduct(h, theta2);
            double probability = CustomMath.sigmoid(score);
            bool label = probability >= 0.5 ? true : false;
            return new Prediction(label, score, probability);
        }
    }
}
