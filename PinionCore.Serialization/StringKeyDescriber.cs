﻿using System;
using PinionCore.Serialization.Dynamic;

namespace PinionCore.Serialization
{
    internal class StringKeyDescriber : IKeyDescriber
    {
        private readonly ITypeFinder _TypeFinder;

        private readonly ITypeDescriber _TypeDescriber;


        public StringKeyDescriber(ITypeFinder type_finder, IDescribersFinder describers_finder)
        {
            _TypeFinder = type_finder;
            _TypeDescriber = describers_finder.Get(typeof(string));
        }

        int IKeyDescriber.GetByteCount(Type type)
        {
            return _TypeDescriber.GetByteCount(type.FullName);
        }

        int IKeyDescriber.ToBuffer(Type type, PinionCore.Memorys.Buffer buffer, int begin)
        {
            return _TypeDescriber.ToBuffer(type.FullName, buffer, begin);
        }

        int IKeyDescriber.ToObject(PinionCore.Memorys.Buffer buffer, int begin, out Type type)
        {
            object nameObject;
            var count = _TypeDescriber.ToObject(buffer, begin, out nameObject);
            type = _TypeFinder.Find(nameObject as string);
            return count;
        }
    }
}
