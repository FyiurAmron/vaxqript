using System;
using System.Collections;
using System.Text;

namespace vax.vaxqript {
    public static class MiscUtils {
        public static readonly Type[] NO_ARGUMENTS = new Type[0];
        /*
        public MiscUtils () {
        }
        */
        // needed since core lib only supports the generic IEnumerable or array string.Join currently
        public static string join ( string separator, IEnumerable iEnumerable ) {
            return join( separator, iEnumerable, "null" );
        }

        public static string join ( string separator, IEnumerable iEnumerable, string nullString ) {
            IEnumerator ienum = iEnumerable.GetEnumerator();
            bool hasNext = ienum.MoveNext();
            if( !hasNext ) {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            object curr = ienum.Current;
            while( true ) {
                hasNext = ienum.MoveNext();
                sb.Append( ( curr != null ) ? curr : nullString );
                if( hasNext ) {
                    sb.Append( separator );
                } else {
                    break;
                }
                curr = ienum.Current;
            }
            return sb.ToString();
        }

        public static Type[] toTypes ( params object[] objs ) {
            int size = objs.Length;
            Type[] ret = new Type[size];
            for( int i = 0; i < size; i++ ) {
                ret[i] = objs[i].GetType();
            }
            return ret;
        }

        public static Type[] toTypes ( IList objs ) {
            int size = objs.Count;
            Type[] ret = new Type[size];
            int i = 0;
            foreach( object o in objs ) { // iterator instead of indexing here in case IList#get() is not O(1)
                ret[i] = o.GetType();
                i++;
            }
            return ret;
        }
    }
}

