using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeBlock : IEvaluable {
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private IExecutable executable;

        public CodeBlock () {
        }

        public void add ( ISyntaxElement syntaxElement ) {
            IExecutable iexe = syntaxElement as IExecutable;
            if( iexe != null ) {
                if( executable != null )
                    throw new NotSupportedException( "executable element '" + executable + "' already present" );
                executable = iexe;
            }
            IEvaluable ieva = syntaxElement as IEvaluable;
            if( ieva == null ) {
                throw new NotSupportedException( "unknown syntax element type '" + syntaxElement.GetType() + "'" );
            }
            arguments.Add( ieva );
        }

        public dynamic[] prepareArguments() {
            dynamic[] arr = new dynamic[arguments.Count];
            for( int i = 0; i < arguments.Count; i++ ) {
                arr[i] = arguments[i].eval();
            }
            return arr;
        }

        public object eval () {
            return executable.exec( prepareArguments() );
        }
    }
}

