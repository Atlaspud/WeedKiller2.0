using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    public static class CustomMath
    {
        public static double dotProduct(double[] a, double[] b)
        {
            double output = 0;
            int n = a.Length;
            for (int i = 0; i < n; i++)
            {
                output += a[i] * b[i];
            }
            return output;
        }

        public static double[] appendScalarToFront(double[] input, int scalar)
        {
            int n = input.Length;
            double[] output = new double[n + 1];
            output[0] = scalar;
            for (int i = 1; i < n + 1; i++)
            {
                output[i] = input[i - 1];
            }
            return output;
        }

        public static double[] vectorByMatrix(double[] vector, double[][] matrix)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            if (rows != vector.Length) return null;
            double[] output = new double[cols];

            for (int j = 0; j < cols; j++)
            {
                output[j] = 0;
                for (int i = 0; i < rows; i++)
                {
                    output[j] += vector[i] * matrix[i][j];
                }
            }
            return output;
        }

        public static double[] sigmoid(double[] input)
        {
            int n = input.Length;
            double[] output = new double[n];
            for (int i = 0; i < n; i++)
            {
                output[i] = sigmoid(input[i]);
            }
            return output;
        }

        public static double sigmoid(double input)
        {
            return 1 / (1 + Math.Exp(-1 * input));
        }
    }
}
