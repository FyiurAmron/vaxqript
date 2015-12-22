using System;

namespace vax.vaxqript {
    public class ValueWrapper : IEvaluable {
        public object Value { get; set; }

        public object eval ( Engine engine ) {
            return Value;
        }

        public ValueWrapper ( object value ) {
            Value = value;
        }

        // TODO handle string instances specially (quote them!) OR add a StringWrapper : ValueWrapper extension maybe?

        public override string ToString () {
            return "/* " + Value.GetType().Name + " */ " + Value;
        }
    }
}

