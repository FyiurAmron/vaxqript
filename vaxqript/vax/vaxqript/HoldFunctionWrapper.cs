using System;

namespace vax.vaxqript {
    /**
     * for quick function/lambda syntax with hold semantics
     */
    public class HoldFunctionWrapper : IExecutable {
        private HoldType holdType;
        private IExecutable executable;

        public HoldFunctionWrapper ( HoldType holdType, ISyntaxGroup executable ) {
            this.holdType = holdType;
            this.executable = executable;
        }

        public HoldType getHoldType ( Engine engine ) {
            return holdType;
        }

        public void setHoldType ( HoldType holdType ) {
            this.holdType = holdType;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            return executable.exec( engine, arguments );
        }
    }
}

