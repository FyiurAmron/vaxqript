using System;

namespace vax.vaxqript {
    public class Function : IExecutable {/*, IEvaluable*/
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
            if( ief != null ) {
                ret = ief.getValue();
            }
            engine.popFunction();
            engine.popCall();
            return ret;
        }

        private string tooFewArgumentsString ( int found, int required ) {
            return "function called with too few arguments (" + found + " found, " + required + " required)";
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setFunctionArguments( arguments );
            if( identifiersMapping != null ) {
                int ids = identifiersMapping.Length;
                if( arguments == null || arguments.Length == 0 ) {
                    throw new InvalidOperationException( tooFewArgumentsString( 0, ids ) );
                }
                ValueList vl = arguments[0] as ValueList;
                int args = vl.Count;
                if( args < ids ) {
                    throw new InvalidOperationException( tooFewArgumentsString( args, ids ) );
                }
                for( int i = 0; i < ids; i++ ) {
                    engine.setIdentifierValue( identifiersMapping[i], vl[i] );
                }
            }
            return eval( engine );
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }
    }
}

