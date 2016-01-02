using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class ValueList : List<object> {
        public ValueList () : base() {
        }

        public ValueList ( int capacity ) : base( capacity ) {
        }

        public override string ToString () {
            return MiscUtils.join( ",", this );
        }
    }
}

