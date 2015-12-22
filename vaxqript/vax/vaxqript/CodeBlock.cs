using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeBlock : IEvaluable {
/*, IExecutable*/
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private IExecutable executable;

        public CodeBlock () {
        }

        public void add ( ISyntaxElement syntaxElement ) {
            IExecutable iexe = syntaxElement as IExecutable;
            IEvaluable ieva = syntaxElement as IEvaluable;
            if( iexe != null ) {
                if( executable == null ) {
                    executable = iexe;
                    return;
                } else if( executable.Equals( iexe ) ) {
                    return; // skip redundant ops
                }
                if( ieva == null )
                    throw new NotSupportedException( "non-evaluable executable element '" + executable
                    + "' already present; '" + iexe + "' not compatible" );
                arguments.Add( ieva );
                return;
            }
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
            return " { " + executable + " " + string.Join( " ", arguments ) + " } ";
        }
        /*
        public object exec ( params dynamic[] arguments ) {
            return executable.exec( arguments );
        }
        */

        public object eval () {
            //return ( executable == null ) ? null : executable.exec( prepareArguments() );
            if( executable != null ) {
                return executable.exec( prepareArguments() );
            }
            object ret = null, ret2;
            foreach( IEvaluable ie in arguments ) {
                ret2 = ie.eval();
                if( ret2 != null )
                    ret = ret2;
            }
            return ret;
        }
    }
}

