﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.Gluon.NN
{
    public class GlobalMaxPool1D : _Pooling
    {
        public GlobalMaxPool1D(string layout = "NCW", string prefix = null, ParameterDict @params = null)
                        : base(new int[] { 1 }, null, new int[] { 0 }, true, true, "max", layout, null, prefix, @params)
        {
            throw new NotImplementedException();
        }
    }
}