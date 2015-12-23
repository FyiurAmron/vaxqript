using System;
using System.Collections;
using System.Text;

namespace vax.vaxqript {
    public class MiscUtils {
        public MiscUtils () {
        }
        // needed since core lib only supports the generic IEnumerable or array string.Join currently
        public static string join ( string separator, IEnumerable iEnumerable ) {
            IEnumerator ienum = iEnumerable.GetEnumerator();
            bool hasNext = ienum.MoveNext();
            if( !hasNext ) {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            object curr = ienum.Current;
            while( true ) {
                hasNext = ienum.MoveNext();
                sb.Append( curr );
                if( hasNext ) {
                    sb.Append( separator );
                } else {
                    break;
                }
                curr = ienum.Current;
            }
            return sb.ToString();
        }
    }
}

