using System;

namespace vax.vaxqript {
    /**
     * for quick lambda syntax with hold semantics
     */
    public class ScriptMethod : IExecutable {
        private HoldType holdType;
        private ISyntaxGroup syntaxGroup;
        // TODO arguments local names

        // TODO add '=>' operator for quick wrapping of CodeBlock into a ScriptMethod

        public ScriptMethod ( HoldType holdType, ISyntaxGroup codeBlock ) {
            this.holdType = holdType;
            this.syntaxGroup = codeBlock;
        }

        public HoldType getHoldType ( Engine engine ) {
            return holdType;
        }

        public void setHoldType ( HoldType holdType ) {
            this.holdType = holdType;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setIdentifierValue( "$args", arguments ); // TEMP, use proper local vars later on
            return syntaxGroup.eval( engine );
        }
    }
}

