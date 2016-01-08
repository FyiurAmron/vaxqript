using System;
using System.Collections.Generic;
using System.Collections;

namespace vax.vaxqript {
    public class SeparatorSyntaxGroup : ISyntaxGroup {
        private List<IEvaluable> groups = new List<IEvaluable>();

        public SeparatorSyntaxGroup () {
        }

        public void add ( ISyntaxElement syntaxElement ) {
            IEvaluable iEva = syntaxElement as IEvaluable;
            if( iEva == null ) {
                throw new NotSupportedException( "unsupported syntax element type '"
                    + syntaxElement.GetType() + "' (not an IEvaluable)" );
            }
            groups.Add( iEva );
        }

        public IList<IEvaluable> getEvaluableList () {
            return groups;
        }

        public bool isEmpty() {
            return groups.Count == 0;
        }

        public dynamic _eval ( Engine engine ) {
            int count = groups.Count;
            if ( count == 0 ) {
                return null;
            }
            object o = groups[0].eval( engine );
            for( int i = 1; i < count; i++ ) {
                if( o is IExecutionFlow ) {
                    return o;
                }
                o = groups[i].eval( engine );
            }
            return o;
        }

        public dynamic eval ( Engine engine ) {
            engine.pushCall( this );
            object ret = _eval( engine );
            engine.popCall();
            return ret;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setFunctionArguments( arguments );
            return eval( engine );
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }

        public override string ToString () {
            return " ( " + string.Join( ";", groups) + " ) ";
        }
    }
}

