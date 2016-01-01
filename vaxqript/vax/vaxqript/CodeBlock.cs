using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeBlock : IEvaluable {
        /*, IExecutable*/
        private List<IEvaluable> arguments = new List<IEvaluable>();
        private Operator op;
        private IExecutable idExec;

        public CodeBlock () {
        }

        public CodeBlock ( params ISyntaxElement[] syntaxElements ) {
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

        private void _add ( ISyntaxElement syntaxElement ) {
            Operator seOp = syntaxElement as Operator;
            if( seOp != null ) {
                if( op == null ) {
                    op = seOp;
                } else if( !seOp.Equals( op ) ) {
                    throw new InvalidOperationException( "operator '" + op
                    + "' already present, '" + op + "' not compatible; explicit parentheses required" );
                }
            } else {
                IEvaluable ieva = syntaxElement as IEvaluable;
                if( ieva == null ) {
                    throw new NotSupportedException( "unsupported syntax element type '" + syntaxElement.GetType() + "' (neither IEvaluable nor Operator)" );
                }
                arguments.Add( ieva );
            }
        }
        /*
        private void _add2 ( ISyntaxElement syntaxElement ) {
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
*/

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
                    for(; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j].eval( engine );
                    }
                    break;
                case HoldType.First:
                    arr[0] = arguments[j];
                    for( i++, j++; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j].eval( engine );
                    }
                    break;
                case HoldType.AllButFirst:
                    arr[0] = arguments[j].eval( engine );
                    for( i++, j++; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j];
                    }
                    break;
                case HoldType.All:
                    for(; i < realArgCount; i++, j++ ) {
                        arr[i] = arguments[j];
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
                    for( ; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j];
                    }
                    break;
                case HoldType.AllButFirst:
                    arr[0] = arguments[j].eval( engine );
                    for( i++,j--; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j];
                    }
                    break;
                case HoldType.First:
                    arr[0] = arguments[j];
                    for( i++,j--; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j].eval( engine );
                    }
                    break;
                case HoldType.None:
                    for( ; i < realArgCount; i++, j-- ) {
                        arr[i] = arguments[j].eval( engine );
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

        public override string ToString () {
            return " { " + ( ( op != null ) ? ( op + " " ) : "" ) + string.Join( " ", arguments ) + " } ";
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }

        public object exec ( Engine engine, params dynamic[] arguments ) {
            engine.setIdentifierValue( "$args", arguments ); // TEMP, use proper local vars later on
            return eval( engine );
        }

        public object _eval ( Engine engine ) {
            int count = arguments.Count;
            if( op != null ) { // doing this before count check allows nullary (const) operators
                return op.exec( engine, prepareArguments( engine, 0 ) );
            }
            if( count == 0 ) {
                return null;
            } 
            Identifier id = arguments[0] as Identifier;
            if( id != null ) {
                ValueWrapper vw = engine.getIdentifierValue( id );
                if( vw != null ) {
                    idExec = vw.Value as IExecutable;
                    if( idExec != null ) {
                        return idExec.exec( engine, prepareArguments( engine, 1 ) );
                    }
                }
            }
            ValueList retList = new ValueList( count );
            for( int i = 0; i < count; i++ ) {
                retList.Add( arguments[i].eval( engine ) );
            }
            return retList;
        }

        public object eval ( Engine engine ) {
            engine.increaseStackCount();
            object ret = _eval( engine );
            engine.decreaseStackCount();
            return ret;
        }
    }
}

