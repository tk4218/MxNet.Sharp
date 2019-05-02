﻿using SiaDNN.Constraints;
using MxNet.DotNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace SiaDNN
{
    public class Variable : Symbol
    {
        public BaseConstraint Constraint { get; set; }

        public Variable()
            :base()
        {
            
        }

        public Variable(string name)
            : base(name)
        {
            
        }
    }
}
