﻿using MxNet.Initializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MxNet.Gluon
{
    public class ParameterDict : IEnumerable
    {
        private string _prefix;

        private ParameterDict _shared;

        private Dictionary<string, Parameter> _params;

        public string Prefix
        {
            get
            {
                return _prefix;
            }
        }

        public ParameterDict Shared
        {
            get
            {
                return _shared;
            }
        }

        public ParameterDict(string prefix = "", ParameterDict shared= null)
        {
            _prefix = prefix;
            _shared = shared;
            _params = new Dictionary<string, Parameter>();
        }

        public Parameter this[string name]
        {
            get
            {
                if(_params.ContainsKey(name))
                    return _params[name];

                return null;
            }
            set
            {
                _params[name] = value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _params.GetEnumerator();
        }

        public string[] Keys()
        {
            return _params.Keys.ToArray();
        }

        public Parameter[] Values()
        {
            return _params.Values.ToArray();
        }

        public Dictionary<string, Parameter> Items()
        {
            return _params;
        }

        public bool Contains(string key) => _params.ContainsKey(key);

        public Parameter GetConstant(string name, NDArray value = null)
        {
            name = Prefix + name;
            var param = GetImpl(name);
            if (param == null)
            {
                if (value == null)
                    throw new Exception($"No constant named '{name}'. Please specify value " +
                                        "if you want to create a new constant.");

                param = new Constant(name, value);
            }
            else if (value != null)
            {
                if(!(param is Constant))
                {
                    throw new Exception($"Parameter '{name}' already exists but it is not a constant.");
                }
            }

            return param;
        }


        private Parameter GetImpl(string name)
        {
            if (_params.ContainsKey(name))
                return _params[name];

            if (_shared != null && _shared.Contains(name))
                _params[name] = _shared[name];

            return null;
        }

        public Parameter Get(string name, OpGradReq grad_req = OpGradReq.Write, Shape shape = null, DType dtype = null,
                             float lr_mult = 1.0f, float wd_mult = 1.0f, Initializer init = null, bool allow_deferred_init = false,
                             bool differentiable = true, StorageStype stype = StorageStype.Default, StorageStype grad_stype = StorageStype.Default)
        {
            name = Prefix + name;
            var param = GetImpl(name);
            if(param == null)
            {
                param = new Parameter(name, grad_req, shape, dtype, lr_mult, wd_mult, init, allow_deferred_init, differentiable, stype, grad_stype);
                _params[name] = param;
            }
            else
            {
                param.Shape = param.Shape ?? shape;
                param.Init = param.Init ?? init;
            }

            return param;
        }

        public void Update(ParameterDict other)
        {
            foreach (var item in other.Items())
            {
                if (!_params.ContainsKey(item.Key))
                {
                    _params[item.Key] = item.Value;
                    continue;
                }

                if(_params[item.Key].GetType() == item.Value.GetType())
                {
                    _params[item.Key] = item.Value;
                }
                else
                {
                    throw new Exception("Cannot update self with other because they have different "  +
                                            $"Parameters with the same name '{item.Key}'");
                }
            }
        }

        public void Initialize(Initializer init= null, Context[] ctx= null, bool verbose= false, bool force_reinit= false)
        {
            init = init ?? new Uniform();
            if (verbose)
                init.SetVerbosity(verbose);

            foreach (var item in _params)
            {
                item.Value.Initialize(null, ctx, init, force_reinit);
            }
        }


        public void ZeroGrad()
        {
            foreach (var item in _params)
            {
                item.Value.ZeroGrad();
            }
        }

        public void ResetCtx(Context ctx)
        {
            foreach (var item in _params)
            {
                item.Value.ZeroGrad();
            }
        }

        public void Save(string filename, string strip_prefix = "")
        {
            NDArrayDict args_dict = new NDArrayDict();
            foreach (var param in _params)
            {
                if(strip_prefix != "" && !param.Key.StartsWith(strip_prefix))
                    throw new Exception($"Prefix '{strip_prefix}' is to be striped before saving, but Parameter's " +
                                        $"name '{param.Key}' does not start with '{strip_prefix}'. " +
                                        "this may be due to your Block shares parameters from other " +
                                        "Blocks or you forgot to use 'with name_scope()' when creating " +
                                        "child blocks. For more info on naming, please see " + 
                                        "http://mxnet.incubator.apache.org/tutorials/basic/naming.html");

                args_dict[param.Key.Remove(0, strip_prefix.Length)] = param.Value.Reduce();
            }

            NDArray.Save(filename, args_dict);
        }

        public void Load(string filename, Context[] ctx= null, bool allow_missing= false,
                        bool ignore_extra= false, string restore_prefix= "", bool cast_dtype= false, string dtype_source= "current")
        {
            if(!string.IsNullOrWhiteSpace(restore_prefix))
            {
                foreach (var name in Keys())
                {
                    if (!name.StartsWith(restore_prefix))
                        throw new Exception($"restore_prefix is '{restore_prefix}' but Parameters name '{name}' does not start with '{restore_prefix}'");

                }
            }

            int lprefix = restore_prefix.Length;
            var loaded_ndarray = NDArray.Load(filename);
            NDArrayDict arg_dict = new NDArrayDict();
            foreach (var item in loaded_ndarray)
            {
                var key = item.Key.StartsWith("arg:") || item.Key.StartsWith("aux:") ? item.Key.Remove(0, 4) : item.Key;
                key = restore_prefix + key;
                arg_dict[key] = item.Value;
            }

            if(!allow_missing)
            {
                foreach (var name in Keys())
                {
                    if (!arg_dict.Contains(name))
                        throw new Exception($"Parameter '{name.Remove(0, lprefix)}' is missing in file '{filename}', which contains parameters: {Utils.BriefPrintList<string>(Keys().ToList())}. " +
                                                "Please make sure source and target networks have the same prefix.");
                }
            }

            foreach (var name in arg_dict.Keys)
            {
                if (!_params.ContainsKey(name))
                {
                    if (ignore_extra)
                        throw new Exception($"Parameter '{name.Remove(0, lprefix)}' loaded from file '{filename}' is not present in ParameterDict, " +
                                                $"choices are: {Utils.BriefPrintList<string>(Keys().ToList())}. Set ignore_extra to True to ignore. " +
                                                "Please make sure source and target networks have the same prefix.");

                    continue;
                }

                this[name].LoadInit(arg_dict[name], ctx, cast_dtype: cast_dtype, dtype_source: dtype_source);
            }
        }
    }
}
