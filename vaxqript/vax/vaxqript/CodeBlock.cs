using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeBlock : IEvaluable {
        /*, IExecutable*/
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private IExecutable executable;

        public CodeBlock () {
        }

        public CodeBlock ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements )
                _add( syntaxElement );
        }

        private void _add ( ISyntaxElement syntaxElement ) {
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

        public void add ( ISyntaxElement syntaxElement ) {
            _add( syntaxElement );
        }

        public void addAll ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements )
                _add( syntaxElement );
        }

        public void clear () {
            arguments.Clear();
            executable = null;
        }

        public dynamic[] prepareArguments ( Engine engine ) {
            dynamic[] arr = new dynamic[arguments.Count];
            if( arguments.Count == 0 )
                return arr;
            
            Operator op = executable as Operator;
            int max = arguments.Count;
            if( op != null ) {
                switch (op.HoldType) {
                case HoldType.First:
                    if( op.Associativity == Associativity.LeftToRight ) {
                        arr[0] = arguments[0];
                        for( int i = 1; i < max; i++ ) {
                            arr[i] = arguments[i].eval( engine );
                        }
                    } else {
                        max--;
                        arr[0] = arguments[max];
                        for( int i = 1, j = max - 1; j >= 0; i++, j-- ) {
                            arr[i] = arguments[j].eval( engine );
                        }
                    }
                    return arr;
                case HoldType.AllButFirst:
                    if( op.Associativity == Associativity.LeftToRight ) {
                        arr[0] = arguments[0].eval( engine );
                        for( int i = 1; i < max; i++ ) {
                            arr[i] = arguments[i];
                        }
                    } else {
                        max--;
                        arr[0] = arguments[max].eval( engine );
                        for( int i = 1, j = max - 1; j >= 0; i++, j-- ) {
                            arr[i] = arguments[j];
                        }
                    }
                    return arr;
                case HoldType.All:
                    if( op.Associativity == Associativity.LeftToRight ) {
                        for( int i = 0; i < max; i++ ) {
                            arr[i] = arguments[i];
                        }
                    } else {
                        for( int i = 0, j = max - 1; i < max; i++, j-- ) {
                            arr[i] = arguments[j];
                        }
                    }
                    return arr;
                case HoldType.None:
                    if( op.Associativity == Associativity.LeftToRight ) {
                        for( int i = 0; i < max; i++ ) {
                            arr[i] = arguments[i].eval( engine );
                        }
                    } else {
                        for( int i = 0, j = max - 1; i < max; i++, j-- ) {
                            arr[i] = arguments[j].eval( engine );
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException( "unknown HoldType '" + op.HoldType + "'" );
                }
            }
            // default core behaviour
            for( int i = 0; i < max; i++ ) {
                arr[i] = arguments[i].eval( engine );
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

        public object eval ( Engine engine ) {
            if( executable != null ) {
                return executable.exec( engine, prepareArguments( engine ) );
            }
            object ret = null, ret2;
            foreach( IEvaluable ie in arguments ) {
                ret2 = ie.eval( engine );
                if( ret2 != null )
                    ret = ret2;
            }
            return ret;
        }
    }
}

