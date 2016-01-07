using System;

namespace vax.vaxqript {
    /**
     * for quick lambda syntax with hold semantics
     */
    public class ScriptMethod : IExecutable {
        private HoldType holdType;
        private SyntaxGroup codeBlock;
        // TODO arguments local names

        // TODO add '=>' operator for quick wrapping of CodeBlock into a ScriptMethod

        public ScriptMethod ( HoldType holdType, SyntaxGroup codeBlock ) {
            this.holdType = holdType;
            this.codeBlock = codeBlock;
        }

        public HoldType getHoldType ( Engine engine ) {
            return holdType;
        }

        public void setHoldType ( HoldType holdType ) {
            this.holdType = holdType;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setIdentifierValue( "$args", arguments ); // TEMP, use proper local vars later on
            return codeBlock.eval( engine );
        }
    }
}

