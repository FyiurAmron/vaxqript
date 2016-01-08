﻿using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public abstract class AbstractSyntaxGroup : ISyntaxGroup {
        /*, IEvaluable, IExecutable*/
        protected List<IEvaluable> arguments = new List<IEvaluable>();
        protected Operator op;
        protected IExecutable idExec;

        public AbstractSyntaxGroup () {
        }

        public AbstractSyntaxGroup ( params ISyntaxElement[] syntaxElements ) {
            foreach( ISyntaxElement syntaxElement in syntaxElements ) {
                _add( syntaxElement );
            }
        }

        // note: essentially a debug method, used in SyntaxTreeBuilder temporarily
        public void setFrom( AbstractSyntaxGroup abstractSyntaxGroup) {
            arguments.AddRange( abstractSyntaxGroup.arguments );
            this.op = abstractSyntaxGroup.op;
            this.idExec = abstractSyntaxGroup.idExec;
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

        public dynamic eval ( Engine engine ) {
            engine.pushCall( this );
            object ret = _eval( engine );
            engine.popCall();
            return ret;
        }

        abstract public dynamic _eval ( Engine engine );

        public override string ToString () {
            return " { " + ( ( op != null ) ? ( op + " " ) : "" ) + string.Join( " ", arguments ) + " } ";
        }
    }
}
