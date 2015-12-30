using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeBlock : IEvaluable {
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private IExecutable executable;

        public CodeBlock () {
        }

        public CodeBlock ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements ) {
                _add( syntaxElement );
            }
        }

        public IExecutable getExecutable () {
            return executable;
        }

        public void setExecutable ( IExecutable executable ) {
            this.executable = executable;
        }

        public List<IEvaluable> getArgumentList () {
            return arguments;
        }

        private void _add ( ISyntaxElement syntaxElement ) {
            IExecutable iexe = syntaxElement as IExecutable;
            IEvaluable ieva = syntaxElement as IEvaluable;
            if( iexe != null ) {
                Identifier id = syntaxElement as Identifier;
                if( id != null ) {
                    if( arguments.Count == 0 && executable == null ) {
                        executable = id;
                    } else {
                        arguments.Add( id );
                    }
                    return;
                }
                // if we're here, syntaxElement is a non-Identifier IExecutable (usually an Operator)
                if( executable == null ) {
                    executable = iexe;
                    return;
                } else if( executable.Equals( iexe ) ) {
                    return; // skip redundant ops etc
                } else {
                    id = executable as Identifier;
                    if( id != null ) { // op is more important, demote the Identifier to regular argument
                        arguments.Insert( 0, id ); // since it *had* to be the first added ISyntaxElement
                        executable = iexe;
                        return;
                    }
                }
                if( ieva == null ) {
                    throw new NotSupportedException( "non-evaluable executable element '" + executable
                    + "' already present; '" + iexe + "' not compatible" );
                }
                arguments.Add( ieva );
                return;
            }
            if( ieva == null ) {
                throw new NotSupportedException( "unsupported syntax element type '" + syntaxElement.GetType() + "'" );
            }
            arguments.Add( ieva );
        }

        private void _add ( object obj ) {
            ISyntaxElement ise = obj as ISyntaxElement;
            _add( ( ise != null ) ? ise : Wrapper.wrap( obj ) );
        }

        public void add ( object obj ) {
            _add( obj );
        }

        public void add ( ISyntaxElement syntaxElement ) {
            _add( syntaxElement );
        }


        public void addAll ( params object[] objs ) {
            foreach( object obj in objs ) {
                _add( obj );
            }
        }

        public void addAll ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements ) {
                _add( syntaxElement );
            }
        }

        public void clear () {
            arguments.Clear();
            executable = null;
        }

        protected dynamic[] prepareArguments ( Engine engine ) {
            dynamic[] arr = new dynamic[arguments.Count];
            if( arguments.Count == 0 )
                return arr;
            
            Operator op = executable as Operator;
            int max = arguments.Count;
            HoldType holdType;
            Associativity associativity;
            if( op != null ) {
                holdType = op.HoldType;
                associativity = op.Associativity;
            } else {
                Identifier id = executable as Identifier;
                if( id != null ) { // if it's already added here, it ought to be a MethodWrapper's Identifier
                    holdType = ( (MethodWrapper) engine.getIdentifierValue( id ).Value ).HoldType;
                    associativity = Associativity.LeftToRight; // shouldn't matter here anyway; if it's needed, implement it in MethodWrapper
                } else {
                    throw new InvalidOperationException( "unknown/unsupported IExecutable type '" + executable.GetType().Name + "'" );
                    /*
                                    holdType = HoldType.None;
                    associativity = Associativity.LeftToRight;
                    */
                }
            }
            switch (holdType) {
            case HoldType.First:
                if( associativity == Associativity.LeftToRight ) {
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
                if( associativity == Associativity.LeftToRight ) {
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
                if( associativity == Associativity.LeftToRight ) {
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
                if( associativity == Associativity.LeftToRight ) {
                    for( int i = 0; i < max; i++ ) {
                        arr[i] = arguments[i].eval( engine );
                    }
                } else {
                    for( int i = 0, j = max - 1; i < max; i++, j-- ) {
                        arr[i] = arguments[j].eval( engine );
                    }
                }
                return arr;
            default:
                throw new InvalidOperationException( "unknown HoldType '" + op.HoldType + "'" );
            }
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
            engine.increaseStackCount();
            object ret, ret2;
            if( executable != null ) {
                ret = executable.exec( engine, prepareArguments( engine ) );
            } else {
                ret = null;
                foreach( IEvaluable ie in arguments ) {
                    ret2 = ie.eval( engine );
                    if( ret2 != null ) {
                        ret = ret2;
                    }
                }
            }
            engine.decreaseStackCount();
            return ret;
        }
    }
}

