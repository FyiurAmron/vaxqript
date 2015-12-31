using System;

namespace vax.vaxqript {
    public class MethodWrapper : IHoldable {
        private Func<object[], object> func;

        public HoldType HoldType { get; private set; }

        public MethodWrapper ( Func<object[], object> func ) : this( func, HoldType.None ) {
        }

        public MethodWrapper ( Func<object[], object> func, HoldType holdType ) {
            this.func = func;
            HoldType = holdType;
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType;
        }

        public object invokeWith ( params object[] arguments ) {
            return func( arguments );
        }
    }
}

