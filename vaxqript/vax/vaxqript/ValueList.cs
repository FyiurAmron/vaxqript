using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    // note: not IEvaluable!
    public class ValueList : List<object> {
        public ValueList () : base() {
        }

        public ValueList ( int capacity ) : base( capacity ) {
        }

        public object last() {
            return this[Count - 1];
        }

        public override string ToString () {
            return "[" + MiscUtils.join( ",", this ) + "]";
        }
    }
}

