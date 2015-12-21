using System;

namespace vax.vaxqript {
    public class ValueWrapper : IEvaluable {
        public object Value { get; set; }

        public object eval () {
            return Value;
        }

        public ValueWrapper ( object value ) {
            Value = value;
        }

        public override string ToString () {
            return string.Format( "/* " + Value.GetType().Name + " */ " + Value );
        }
    }
}

