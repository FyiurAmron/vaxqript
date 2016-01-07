using System;
using System.Collections.Generic;
using System.Collections;

namespace vax.vaxqript {
    public class SyntaxGroup : ISyntaxGroup, IEvaluable, IExecutable {
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private Operator op;
        private IExecutable idExec;

        public SyntaxGroup () {
        }

        public SyntaxGroup ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements ) {
                _add( syntaxElement );
            }
        }

        public Operator getOperator () {
            return op;
        }

        public void setOperator ( Operator op ) {
            this.op = op;
        }

        public List<IEvaluable> getArgumentList () {
            return arguments;
        }

        public IList<IEvaluable> getEvaluableList () {
            return arguments;
        }

        private void _add ( ISyntaxElement syntaxElement ) {
            // maybe: add some prefix/infix/postfix sanitization? (note the '.' operator!)
            Operator seOp = syntaxElement as Operator;
            if( seOp != null ) {
                if( op == null ) {
                    op = seOp;
                } else if( !seOp.Equals( op ) ) {
                    throw new InvalidOperationException( "operator '" + op
                    + "' already present, '" + seOp + "' not compatible; explicit parentheses required" );
                }
            } else {
                IEvaluable ieva = syntaxElement as IEvaluable;
                if( ieva == null ) {
                    throw new NotSupportedException( "unsupported syntax element type '"
                    + syntaxElement.GetType() + "' (neither IEvaluable, Operator nor BlockSeparator)" );
                }
                arguments.Add( ieva );
            }
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
            op = null;
            idExec = null;
        }

        protected dynamic[] prepareArguments ( Engine engine, int offset ) {
            int argCount = arguments.Count, realArgCount = argCount - offset;
            dynamic[] arr = new dynamic[realArgCount];
            if( realArgCount == 0 )
                return arr;
            
            HoldType holdType;
            Associativity associativity; // used by Operatator class mostly

            if( op != null ) {
                holdType = op.HoldType;
                associativity = op.Associativity;
            } else {
                holdType = idExec.getHoldType( engine );
                associativity = Associativity.LeftToRight; // shouldn't matter here anyway; if it's needed, implement it in MethodWrapper or interface it
            }
            int i = 0, j;
            switch (associativity) {
            case Associativity.LeftToRight:
                j = offset;
                switch (holdType) {
                case HoldType.None:
                    for( ; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j].eval( engine );
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.First:
                    arr[0] = arguments[j];
                    for( i++, j++; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j].eval( engine );
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.AllButFirst:
                    arr[0] = arguments[j].eval( engine );
                    for( i++, j++; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j];
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.All:
                    for( ; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j];
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                default:
                    throw new InvalidOperationException( "uknown HoldType '" + holdType + "'" );
                }
                break;
            case Associativity.RightToLeft:
                j = argCount - 1;
                switch (holdType) {
                case HoldType.All:
                    for(; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j];
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.AllButFirst:
                    arr[0] = arguments[j].eval( engine );
                    for( i++,j--; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j];
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.First:
                    arr[0] = arguments[j];
                    for( i++,j--; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j].eval( engine );
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                case HoldType.None:
                    for(; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j].eval( engine );
                        /*
                        if( arr[i] is BlockSeparator ) {
                            return arr;
                        }
                        */
                    }
                    break;
                default:
                    throw new InvalidOperationException( "uknown HoldType '" + holdType + "'" );
                }
                break;
            default:
                throw new InvalidOperationException( "uknown Associativity '" + associativity + "'" );
            }
            return arr;
        }

        public bool isEmpty () {
            return op == null && arguments.Count == 0;
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.setFunctionArguments( arguments );
            return eval( engine );
        }

        public dynamic _eval ( Engine engine ) {
            int count = arguments.Count;
            if( op != null ) { // doing this before count check allows nullary (const) operators
                return op.exec( engine, prepareArguments( engine, 0 ) );
            }
            if( count == 0 ) {
                return null;
            } 
            Identifier id = arguments[0] as Identifier;
            object o = null; // actually, the assignment is unneeded, but c# is too stupid to know that
            bool preEval = false;

            if( id != null ) {
                ValueWrapper vw = engine.getIdentifierValue( id );
                if( vw != null ) {
                    if( count == 1 ) {
                        IEvaluable iEva = vw.Value as IEvaluable;
                        if( iEva != null ) {
                            return iEva.eval( engine );
                        }
                    } else {
                        idExec = vw.Value as IExecutable;
                        if( idExec != null ) {
                            return idExec.exec( engine, prepareArguments( engine, 1 ) );
                        }
                    }
                }
            }

            if( !preEval )
                o = arguments[0].eval( engine );
            CompositeIdentifier cid = o as CompositeIdentifier;

            if( cid != null ) {
                if( count == 1 ) {
                    return cid.eval( engine );
                } else {
                    idExec = cid;
                    return idExec.exec( engine, prepareArguments( engine, 1 ) );
                }
            }

            for( int i = 1; i < count; i++ ) {
                if( o is IExecutionFlow ) {
                    return o;
                }
                o = arguments[i].eval( engine );
            }
            return o;
        }

        public dynamic eval ( Engine engine ) {
            engine.pushCallStack( this );
            object ret = _eval( engine );
            engine.popCallStack();
            return ret;
        }


        public override string ToString () {
            return " { " + ( ( op != null ) ? ( op + " " ) : "" ) + string.Join( " ", arguments ) + " } ";
        }
    }
}

