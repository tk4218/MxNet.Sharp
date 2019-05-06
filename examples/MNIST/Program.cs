﻿using SiaDNN.Initializers;
using MxNet.NN;
using MxNetLib;
using MxNet.NN.Data;
using MxNet.NN.Layers;
using System;

namespace MNIST
{
    class Program
    {
        static void Main(string[] args)
        {
            MXNet.SetDevice(DeviceType.CPU);

            int inputDim = 28 * 28;
            int labelCount = 10;
            uint batchSize = 200;

            string trainImagePath = "./mnist_data/train-images-idx3-ubyte";
            string trainLabelPath = "./mnist_data/train-labels-idx1-ubyte";
            string valImagePath = "./mnist_data/t10k-images-idx3-ubyte";
            string valLabelPath = "./mnist_data/t10k-labels-idx1-ubyte";

            var (train, val) = DataSetParser.MNIST(trainImagePath, trainLabelPath, valImagePath, valLabelPath, batchSize);
            
            var model = new Module(1, 28, 28);
            //model.Add(new Dense(inputDim, ActivationType.ReLU, new GlorotUniform()));
            //model.Add(new Dense(128, ActivationType.ReLU, new GlorotUniform()));
            //model.Add(new Dense(labelCount));

            model.Add(new Conv2D(20, Tuple.Create<uint, uint>(5, 5), activation: ActivationType.ReLU));
            model.Add(new MaxPooling2D(Tuple.Create<uint, uint>(2, 2), Tuple.Create<uint, uint>(2, 2)));
            model.Add(new Conv2D(20, Tuple.Create<uint, uint>(5, 5), activation: ActivationType.ReLU));
            model.Add(new MaxPooling2D(Tuple.Create<uint, uint>(2, 2), Tuple.Create<uint, uint>(2, 2)));
            model.Add(new Flatten());
            model.Add(new Dropout(0.5f));
            model.Add(new Dense(128, ActivationType.ReLU));
            model.Add(new Dense(10));

            model.Compile(Optimizers.SGD(0.1f), LossType.CategorialCrossEntropy, MetricType.Accuracy);
            model.Fit(train, 10, batchSize, val);
        }
    }
}
