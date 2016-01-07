using System;

namespace vax.vaxqript {
    // currently unused
    public class BlockSeparator : IEvaluable {
        public static BlockSeparator Instance { get; private set;} 

        static BlockSeparator() {
            Instance = new BlockSeparator();
        }

        private BlockSeparator () {
        }

        public dynamic eval ( Engine engine ) {
            return null;
        }

        public override string ToString () {
            return ";";
        }
    }
}

