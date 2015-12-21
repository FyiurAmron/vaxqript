using System;

namespace vax.vaxqript {
    public class UnknownElement : ISyntaxElement {
        string s;

        public UnknownElement ( string s ) {
            this.s = s;
        }

        public override string ToString () {
            return s;
        }
    }
}

