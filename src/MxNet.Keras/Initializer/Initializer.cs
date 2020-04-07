﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.Keras.Initializer
{
    public abstract class Initializer
    {
        public abstract KerasSymbol Call(Shape shap, DType dtype = null);

        public abstract ConfigDict GetConfig();
    }
}
