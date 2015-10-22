using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeedKiller2._0
{
    class BinaryClassifier
    {
        //Instance variables
        SVMModel supportVectorMachineModel;
        LRModel logisticRegressionModel;
        ANNModel artificialNeuralNetworkModel;

        public BinaryClassifier()
        {
            //Load default gaussian support vector machine model
            //double[] alpha = CSV.readDoubleArray(Environment.CurrentDirectory + "\\alpha.csv");
            //double[][] supportVectors = CSV.readDoubleJaggedArray(Environment.CurrentDirectory + "\\supportVectors.csv");
            //double[] supportVectorLabels = CSV.readDoubleArray(Environment.CurrentDirectory + "\\supportVectorLabels.csv");
            //double bias = -0.926822427178871;
            //double scale = 0.254248687979702;
            //double slope = -2.545742;
            //double intercept = -0.8773927;
            //supportVectorMachineModel = new SVMModel(alpha, bias, scale, supportVectors, supportVectorLabels, slope, intercept);
            supportVectorMachineModel = new SVMModel(Environment.CurrentDirectory + "\\SVMModel.csv");

            //Load default logistic regression model
            //double[] mu = new double[] { 1, 2.10891089108911, 2.23762376237624 };
            //double[] sigma = new double[] { 0, 5.41869646007879, 4.24610888843110 };
            //double[] theta = new double[] { -0.439200407495956, 5.52706460762027, -0.570342835510078 };
            //logisticRegressionModel = new LRModel(mu, sigma, theta);

            logisticRegressionModel = new LRModel(Environment.CurrentDirectory + "\\LRModel.csv");
            artificialNeuralNetworkModel = new ANNModel(Environment.CurrentDirectory + "\\ANNModel.csv");
        }

        public Prediction predictWindow(double[] descriptor)
        {
            //return supportVectorMachineModel.predict(descriptor);
            return artificialNeuralNetworkModel.predict(descriptor);
        }

        public Prediction predictImage(double[] descriptor)
        {
            descriptor = logisticRegressionModel.mapFeatures(descriptor, 1);
            descriptor = logisticRegressionModel.standardise(descriptor);
            Prediction prediction = logisticRegressionModel.predict(descriptor);
            return prediction;
        }

    }
}
