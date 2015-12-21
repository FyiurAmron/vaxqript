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
                return;
            }
            IEvaluable ieva = syntaxElement as IEvaluable;
            if( ieva == null ) {
                throw new NotSupportedException( "unknown syntax element type '" + syntaxElement.GetType() + "'" );
            }
            arguments.Add( ieva );
        }

        public void addAll ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements )
                add( syntaxElement );
        }

        public void clear () {
            arguments.Clear();
            executable = null;
        }

        public dynamic[] prepareArguments () {
            dynamic[] arr = new dynamic[arguments.Count];
            for( int i = 0; i < arguments.Count; i++ ) {
                arr[i] = arguments[i].eval();
            }
            return arr;
        }

        public override string ToString () {
            return executable + " " + string.Join( " ", arguments );
        }

        public object eval () {
            return ( executable == null ) ? null : executable.exec( prepareArguments() );
        }
    }
}

