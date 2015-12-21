using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;

namespace vax.vaxqript {
    public static class Engine {
        /*
        public Engine () {
        }
        */
        public static Dictionary<Identifier, object> varMap = new Dictionary<Identifier,object>() {
            { new Identifier( "true" ), true },
            { new Identifier( "false" ), false },
            //{ new Identifier( "Inf" ), float.PositiveInfinity },
        };

        public static bool IsNumericType ( this object o ) {   
            switch (Type.GetTypeCode( o.GetType() )) {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
            }
        }

    }
}

