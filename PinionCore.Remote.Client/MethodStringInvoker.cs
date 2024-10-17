using System;
using System.Linq;
using System.Reflection;

namespace PinionCore.Remote.Client
{
    public class MethodStringInvoker
    {
         private readonly TypeConverterSet _TypeConverterSet ;
        public readonly object Target;
        public readonly MethodInfo Method;

        public MethodStringInvoker(object instance, MethodInfo method, TypeConverterSet set)
        {
            this.Target = instance;
            _TypeConverterSet = set;
            this.Method = method;
        }

        public object Invoke(params string[] in_args)
        {

            ParameterInfo[] argInfos = Method.GetParameters();

            if (in_args.Length != argInfos.Length)
                throw new Exception($"Method parameter is {argInfos.Length}, input parameter is {in_args.Length}");

            System.Collections.Generic.List<object> argInstances = new System.Collections.Generic.List<object>();
            for (int i = 0; i < argInfos.Length; ++i)
            {
                ParameterInfo argInfo = argInfos[i];
                string inArg = in_args[i];
                object val;

                try
                {
                    _Conversion(inArg, out val, argInfo.ParameterType);
                    argInstances.Add(val);
                }
                catch (SystemException se)
                {
                    throw new Exception($"Type mismatch , arg is {inArg}");
                }


            }
            return Method.Invoke(Target, argInstances.ToArray());
        }

        private void _Conversion(string inArg, out object val, Type parameterType)
        {
            if (_TypeConverterSet.Convert(inArg, out val, parameterType))
                return;

            PinionCore.Utility.Command.TryConversion(inArg, out val, parameterType);
        }
        


    }
}