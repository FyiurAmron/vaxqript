using System;

namespace vax.vaxqript {
    public class MethodWrapper {
        private Func<object[], object> func;

        public HoldType HoldType { get; private set; }

        public MethodWrapper ( Func<object[], object> func ) : this( func, HoldType.None ) {
        }

        public MethodWrapper ( Func<object[], object> func, HoldType holdType ) {
            this.func = func;
            HoldType = holdType;
        }

        public object invokeWith ( params object[] arguments ) {
            return func( arguments );
        }
    }
}

