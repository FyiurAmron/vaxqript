using System.Collections.Generic;

namespace vax.vaxqript {
    // note: not IEvaluable!
    public class ValueList : List<object>/*, IScriptOperatorOverload*/ {
        public ValueList () : base() {
        }

        public ValueList ( int capacity ) : base( capacity ) {
        }

        public ValueList ( IEnumerable<object> collection ) : base( collection ) {
        }

        public object last () {
            return this[Count - 1];
        }

        public override string ToString () {
            return "[" + MiscUtils.join( this, "," ) + "]";
        }

        /*
        public static ValueList operator+ ( ValueList valueList, object o ) {
            ValueList ret = new ValueList( valueList );
            ret.Add( o );
            return ret;
        }

        public ValueWrapper processLeft ( string opString, dynamic argument ) {
            return null;
        }

        public ValueWrapper processRight ( string opString, dynamic argument ) {
            if( opString.Equals( "+=" ) ) {
                Add( argument );
                return new ValueWrapper( this );
            } else {
                return null;
            }
        }
        */
    }
}

