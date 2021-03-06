﻿using System;
using System.Numerics;

namespace Cassandra
{
    internal partial class TypeInterpreter
    {
        public static object ConvertFromVarint(IColumnInfo type_info, byte[] value)
        {
            Array.Reverse(value);
            return new BigInteger(value);
        }

        public static Type GetTypeFromVarint(IColumnInfo type_info)
        {
            return typeof(BigInteger);
        }

        public static byte[] InvConvertFromVarint(IColumnInfo type_info, object value)
        {
            CheckArgument<BigInteger>(value);
            var ret  = ((BigInteger)value).ToByteArray();
            
            Array.Reverse(ret);
            return ret;
        }
    }
}
