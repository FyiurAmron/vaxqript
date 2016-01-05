using System;

namespace vax.vaxqript {
    public class Undefined : IEvaluable {
        public readonly static Undefined INSTANCE = new Undefined();

        private Undefined () {
        }

        public dynamic eval ( Engine engine ) {
            return this; // doesn't evaluate at all
        }

        public override string ToString () {
            return "undefined";
        }
    }
}

