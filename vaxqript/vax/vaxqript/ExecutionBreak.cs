using System;

namespace vax.vaxqript {
    public class ExecutionBreak : IExecutionFlow {
        private object value;

        public ExecutionBreak ( object value ) {
            this.value = value;
        }

        public object getValue() {
            return value;
        }

        public object getLoopValue() {
            return value; // unwrapped; stops at single loop level
        }
    }
}

