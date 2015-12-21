using System;

namespace vax.vaxqript {
    public class ValueWrapper : IEvaluable {
        public object Value { get; set; }

        public  object eval() { return Value; }
    }
}

