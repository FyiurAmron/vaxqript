using System;

namespace vax.vaxqript {
    public class Function : IExecutable/*, IEvaluable*/ {
        private IEvaluable evaluable;
        private Identifier[] identifiersMapping;

        public Function ( IEvaluable syntaxGroup ) {
            this.evaluable = syntaxGroup;
        }

        public Function ( IEvaluable syntaxGroup, params Identifier[] identifiers ) {
            this.evaluable = syntaxGroup;
            this.identifiersMapping = identifiers;
        }

        private dynamic _eval ( Engine engine ) {
            return evaluable.eval( engine );
        }

        private dynamic eval ( Engine engine ) {
            engine.pushCall( this );
            engine.pushFunction( this );
            object ret = _eval( engine );
            IExecutionFlow ief = ret as IExecutionFlow;
            if ( ief != null ) {
                ret = ief.getValue();
            }
            engine.popFunction();
            engine.popCall();
            return ret;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setFunctionArguments( arguments );
            if( identifiersMapping != null ) {
                int args = arguments.Length, ids = identifiersMapping.Length;
                if ( args < ids ) {
                    throw new InvalidOperationException("function called with too few arguments ("
                        +args+" found, "+ids+" required)");
                }
                for(int i = 0; i < ids; i++ ) {
                    engine.setIdentifierValue( identifiersMapping[i], arguments[i] );
                }
            }
            return eval( engine );
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }
    }
}

