﻿namespace vax.vaxqript {

using System;
using System.Collections;
using System.Reflection;
using System.Text;

public static class MiscUtils {
    //public static readonly object[] NO_ARGUMENTS = new object[0];
    public static readonly Type[] NO_ARGUMENTS_TYPE = new Type[0];
    public static readonly Random RNG = new();
    private static readonly byte[] EIGHT_BYTES = new byte[8];

    /*
    public MiscUtils () {
    }
    */
    public static int getCurrentTimeMillis() {
        return Environment.TickCount;
    }

    public static object unwrap( params object[] arguments ) {
        switch ( arguments.Length ) {
            case 0:
                return null;
            case 1:
                return arguments[0];
            default:
                return arguments;
        }
    }

    public static object getRandomForType( Type t ) {
        switch ( Type.GetTypeCode( t ) ) {
            case TypeCode.Char:
                return (Char) RNG.Next( Char.MinValue, Char.MaxValue );
            case TypeCode.Byte:
                return (Byte) RNG.Next( Byte.MinValue, Byte.MaxValue );
            case TypeCode.SByte:
                return (SByte) RNG.Next( SByte.MinValue, SByte.MaxValue );
            case TypeCode.UInt16:
                return (UInt16) RNG.Next( UInt16.MinValue, UInt16.MaxValue );
            case TypeCode.UInt32:
                return (UInt32) RNG.Next( 0, Int32.MaxValue );
            case TypeCode.UInt64:
                return (UInt32) RNG.Next( 0, Int32.MaxValue );
            case TypeCode.Int16:
                return (Int16) RNG.Next( Int16.MinValue, Int16.MaxValue );
            case TypeCode.Int32:
                return RNG.Next(); // no actual cast here
            case TypeCode.Int64:
                return (Int64) RNG.Next();
            case TypeCode.Decimal:
                return (Decimal) RNG.NextDouble();
            case TypeCode.Single:
                return (Single) RNG.NextDouble();
            case TypeCode.Double:
                return RNG.NextDouble(); // no actual cast here
            case TypeCode.Boolean:
                return RNG.NextDouble() > 0.5;
            case TypeCode.String:
                RNG.NextBytes( EIGHT_BYTES );
                return Encoding.Default.GetString( EIGHT_BYTES );
            default:
                return false;
        }
    }

    public static Type getTypeFor( object n ) {
        switch ( n ) {
            case CompositeIdentifier ci: {
                Type t = ci.toType();
                if ( t != null ) {
                    return t;
                }

                break;
            }
            case string s: { // get type from name
                Type t = Type.GetType( s );
                if ( t != null ) {
                    return t;
                }

                break;
            }
            case ValueWrapper vw:
                n = vw.Value; // note: not recursive!
                break;
        }

        return n.GetType();
    }

    public static string toString( object o ) {
        return ( o == null ) ? "null" : o.ToString();
    }

    public static string toDebugString( object o ) {
        return "'" + o + "'" + ( ( o == null ) ? "" : " (of type " + o.GetType() + ")" );
    }

    // needed since core lib only supports the generic IEnumerable or array string.Join currently
    public static string join( IEnumerable iEnumerable, string separator = ",", string nullString = "null" ) {
        IEnumerator ienum = iEnumerable.GetEnumerator();
        bool hasNext = ienum.MoveNext();
        if ( !hasNext ) {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        object curr = ienum.Current;
        while ( true ) {
            hasNext = ienum.MoveNext();
            sb.Append( curr ?? nullString );
            if ( hasNext ) {
                sb.Append( separator );
            } else {
                break;
            }

            curr = ienum.Current;
        }

        return sb.ToString();
    }

    public static Type[] toTypes( params object[] objs ) {
        if ( objs == null ) {
            return NO_ARGUMENTS_TYPE;
        }

        int size = objs.Length;
        Type[] ret = new Type[size];
        for ( int i = 0; i < size; i++ ) {
            ret[i] = objs[i].GetType();
        }

        return ret;
    }

    public static Type[] toTypes( IList objs ) {
        if ( objs == null ) {
            return NO_ARGUMENTS_TYPE;
        }

        int size = objs.Count;
        Type[] ret = new Type[size];
        int i = 0;
        foreach ( object o in objs ) { // iterator instead of indexing here in case IList#get() is not O(1)
            ret[i] = o.GetType();
            i++;
        }

        return ret;
    }

    public static bool IsNumericType( Type t ) {
        switch ( Type.GetTypeCode( t ) ) {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Single:
            case TypeCode.Double:
                // case TypeCode.Char: // it's numeric actually...
                return true;
            default:
                return false;
        }
    }

    public static object createNew( Type type, Type[] types, object[] args ) {
        // TODO: test if this handles structs well/at all
        if ( type.IsPrimitive ) {
            type = typeof(Nullable<>).MakeGenericType( type );
        }

        ConstructorInfo ci = type.GetConstructor( types );
        if ( ci == null ) {
            throw new InvalidOperationException( $"constructor {type}({@join( types, "," )}) not found" );
        }

        return ci.Invoke( args );
    }

    public static object createNew( Type type, object[] args ) {
        return createNew( type, ( args != null ) ? toTypes( args ) : NO_ARGUMENTS_TYPE, args );
    }

    public static object createNew( Type type, ValueList args ) {
        return args == null
            ? createNew( type, NO_ARGUMENTS_TYPE, null )
            : createNew( type, toTypes( args ), args.ToArray() );
    }
}

}
