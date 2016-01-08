using System;

namespace vax.vaxqript {
    public class ExecutionReturn : IExecutionFlow {
        private object value;

        public ExecutionReturn ( object value ) {
            this.value = value;
        }

        public object getValue() {
            return value;
        }

        public object getLoopValue() {
            return this; // wrapped; propagates until top
        }
    }
}

