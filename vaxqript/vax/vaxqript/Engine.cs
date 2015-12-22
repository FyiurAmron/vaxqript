using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;
using System.Collections;

namespace vax.vaxqript {
    public class Engine {
        public Dictionary<Identifier, object> varMap = new Dictionary<Identifier,object>() {
            { new Identifier( "true" ), true },
            { new Identifier( "false" ), false },
            //{ new Identifier( "Inf" ), float.PositiveInfinity },
            //etc
        };

        Dictionary<string,Operator> operatorMap = new Dictionary<string,Operator>();

        public Engine () {
            createDefaultOperators();
        }

        protected void createDefaultOperators () {
            Operator[] defaultOperators = {
                new Operator( ".", (n ) => {
                    return null;
                }, (n, m ) => {
                    return null;
                } ),
                new Operator( "+", (n ) => {
                    return +n;
                }, (n, m ) => {
                    return n + m;
                } ),
                new Operator( "-", (n ) => {
                    return -n;
                }, (n, m ) => {
                    return n - m;
                } ),
                new Operator( "`", (n ) => {
                    return varMap[Identifier.valueOf( n )];
                }, null ),
                new Operator( "!", (n ) => {
                    return !n;
                }, null ),
                new Operator( "~", (n ) => {
                    return ~n;
                }, null ),
                new Operator( "++", (n ) => {
                    return ++varMap[n];
                }, null, HoldType.HoldFirst ),
                new Operator( "--", (n ) => {
                    return --varMap[n];
                }, null, HoldType.HoldFirst ),
                new Operator( "*", null, (n, m ) => {
                    return n * m;
                } ),
                new Operator( "/", null, (n, m ) => {
                    return n / m;
                } ),
                new Operator( "%", null, (n, m ) => {
                    return n % m;
                } ),
                new Operator( "|", null, (n, m ) => {
                    return n | m;
                } ),
                new Operator( "||", null, (n, m ) => {
                    return n || m;
                } ),
                new Operator( "&", null, (n, m ) => {
                    return n & m;
                } ),
                new Operator( "&&", null, (n, m ) => {
                    return n && m;
                } ),
                new Operator( "^", null, (n, m ) => {
                    return n ^ m;
                } ),
                new Operator( "<<", null, (n, m ) => {
                    return n << m;
                } ),
                new Operator( ">>", null, (n, m ) => {
                    return n >> m;
                } ),
                new Operator( "==", null, (n, m ) => {
                    return n == m;
                } ),
                new Operator( "!=", null, (n, m ) => {
                    return n != m;
                } ),
                new Operator( ">", null, (n, m ) => {
                    return n > m;
                } ),
                new Operator( ">=", null, (n, m ) => {
                    return n >= m;
                } ),
                new Operator( "<", null, (n, m ) => {
                    return n < m;
                } ),
                new Operator( "<=", null, (n, m ) => {
                    return n <= m;
                } ),
                new Operator( "??", null, (n, m ) => {
                    return n ?? m;
                } ),
                // TODO ternary as "?:"
                new Operator( "=", null, (n, m ) => {
                    varMap[n] = m;
                    return m;
                }, HoldType.HoldFirst ),
                new Operator( "+=", null, (n, m ) => {
                    if( n is Stack ) {
                        n.Push( m );
                        return n;
                    }
                    if( n is Queue ) {
                        n.Enqueue( m );
                        return n;
                    }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if( n is ICollection ) {
                        n.Add( m );
                        return n;
                    }
                    varMap[n] += m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "-=", null, (n, m ) => {
                    if( n is Stack ) {
                        n.Pop( m );
                        return n;
                    }
                    if( n is Queue ) {
                        n.Dequeue( m );
                        return n;
                    }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if( n is ICollection ) {
                        n.Remove( m );
                        return n;
                    }        
                    varMap[n] -= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "*=", null, (n, m ) => {
                    varMap[n] *= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "/=", null, (n, m ) => {
                    varMap[n] /= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "%=", null, (n, m ) => {
                    varMap[n] %= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "&=", null, (n, m ) => {
                    varMap[n] &= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "|=", null, (n, m ) => {
                    varMap[n] |= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "^=", null, (n, m ) => {
                    varMap[n] ^= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( "<<=", null, (n, m ) => {
                    varMap[n] <<= m;
                    return n;
                }, HoldType.HoldFirst ),
                new Operator( ">>=", null, (n, m ) => {
                    varMap[n] >>= m;
                    return n;
                }, HoldType.HoldFirst ),
            };
            foreach( Operator op in defaultOperators ) {
                addOperator( op );
            }
        }

        public void addOperator ( Operator op ) {
            operatorMap[op.OperatorString] = op;
        }

        public Operator operatorValueOf ( string opString ) {
            Operator op = null;
            if( !operatorMap.TryGetValue( opString, out op ) )
                throw new InvalidOperationException( "no matching operator in this engine for '" + opString + "'" );
            return op;
        }

        public Func<dynamic, dynamic> getUnaryOperator ( string opString ) {
            return operatorValueOf( opString ).UnaryLambda;
        }

        public Func<dynamic, dynamic, dynamic> getNaryOperator ( string opString ) {
            return operatorValueOf( opString ).NaryLambda;
            /*
            Func<dynamic, dynamic, dynamic> ret;
            naryOperatorDictionary.TryGetValue( opString, out ret );
            return ret;
            */
        }

        /*
        // flow operators
        OP_ParenthesisLeft( "(", -1, -1, type.OT_Prefix, true ),
        OP_ParenthesisRight( ")", -1, -1, type.OT_Postfix, true ),
        OP_BracketLeft( "[", -1, 1, type.OT_Prefix, true ),
        OP_BracketRight( "]", -1, 1, type.OT_Postfix, true ),
        OP_BraceLeft( "{", -1, -1, type.OT_Prefix, true ),
        OP_BraceRight( "}", -1, -1, type.OT_Postfix, true ),
        OP_Separator( ",", -1, 2, type.OT_Infix, true ),
        OP_Terminator( ";", -1, 1, type.OT_Postfix, true ),
        OP_Internal( "#", -1, 1, type.OT_Prefix, true ),
        // secondary flow operators
        OP_ForeachForward( ">>", -1, 2, type.OT_Infix ),
        OP_ForeachBackward( "<<", -1, 2, type.OT_Infix ), // last operator in enum
        OP_Ellipsis( "...", -1, 0, type.OT_Prefix ), // vararg def
        OP_New( "@", -1, 1, type.OT_Prefix ), // strange, but simple
        // regular operators
        OP_Select( ".", -2, 2, type.OT_Infix ), // single dot (selection operator) should create an identifier if followed by alpha char
        OP_Increment( "++", -1, 1, type.OT_PrefixPostfix ), // L-VAL
        OP_Decrement( "--", -1, 1, type.OT_PrefixPostfix ), // L-VAL
        OP_BoolNot( "!", 0, 1, type.OT_Prefix ),
        OP_Range( "..", 0, 2, type.OT_Infix ), // array range selector
        OP_InstanceOf( "??", 0, 2, type.OT_Infix ), // again, strange, but simple
        OP_Multiply( "*", 1, 2, type.OT_Infix ),
        OP_Divide( "/", 1, 2, type.OT_Infix ),
        OP_Remainder( "%", 1, 2, type.OT_Infix ),
        OP_Plus( "+", 2, 2, type.OT_Infix ),
        OP_Minus( "-", 2, 2, type.OT_Infix ),
        OP_MoreThan( ">", 3, 2, type.OT_Infix ),
        OP_LessThan( "<", 3, 2, type.OT_Infix ),
        OP_MoreOrEq( ">=", 3, 2, type.OT_Infix ),
        OP_LessOrEq( "<=", 3, 2, type.OT_Infix ),
        OP_IsEqual( "==", 4, 2, type.OT_Infix ),
        OP_IsNotEqual( "!=", 4, 2, type.OT_Infix ),
        OP_BoolAnd( "&&", 5, 2, type.OT_Infix ),
        OP_BoolAndLong( "&", 5, 2, type.OT_Infix ),
        OP_BoolOr( "||", 6, 2, type.OT_Infix ),
        OP_BoolOrLong( "|", 6, 2, type.OT_Infix ),
        OP_Equals( "=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisPlus( "+=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisMinus( "-=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisMultiply( "*=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisDivide( "/=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisRemainder( "%=", 7, 2, type.OT_Infix ), // L-VAL
        //OP_TernaryStart( "?", 7, type.OT_Ternary ),
        //OP_TernaryEnd( ":", 7, type.OT_Ternary ), // shared with case selector in "case X:"; op_prec doesn't matter in that case
        ;
        */
        public object eval ( string s ) {
            return eval( new StringLexer( s, this ) );
        }

        public object eval ( Lexer lexer ) {
            return eval( lexer.createLinearSyntax() );
        }

        public object eval ( LinearSyntax linearSyntax ) {
            return linearSyntax.buildParseTree().eval( this );
        }

        public object eval ( IEvaluable iEvaluable ) {
            return iEvaluable.eval( this );
        }

        public object exec ( IExecutable iExecutable, params dynamic[] arguments ) {
            return iExecutable.exec( this, arguments );
        }

        public dynamic debugApplyGenericOperator ( String opString, params dynamic[] arguments ) {
            Operator op = operatorValueOf( opString );
            switch (arguments.Length) {
            case 0:
                return applyNullaryOperator( op );
            case 1:
                return applyUnaryOperator( op, arguments[0] );
            default:
                return applyNaryOperator( op, arguments );
            }
        }

        public dynamic applyGenericOperator ( Operator op, params dynamic[] arguments ) {
            switch (arguments.Length) {
            case 0:
                return applyNullaryOperator( op );
            case 1:
                return applyUnaryOperator( op, arguments[0] );
            default:
                return applyNaryOperator( op, arguments );
            }
        }

        public dynamic applyNullaryOperator ( Operator op ) {
            throw new NotSupportedException( "not supported nullary '" + op + "'" );
        }

        public dynamic applyUnaryOperator ( Operator op, dynamic argument ) {
            IScriptOperatorOverload isoo = argument as IScriptOperatorOverload;
            if( isoo != null ) {
                ValueWrapper ret = isoo.processLeft( op.OperatorString, argument );
                if( ret != null )
                    return ret.Value;
            }
            return op.UnaryLambda( argument );
        }

        public dynamic applyNaryOperator ( Operator op, params dynamic[] arguments ) {
            dynamic result = arguments[0];
            //var operatorLambda = getNaryOperator( opString );
            var operatorLambda = op.NaryLambda;
            int i = 1;
            for(; i < arguments.Length; i++ ) {
                IScriptOperatorOverload isoo = result as IScriptOperatorOverload;
                if( isoo != null ) {
                    ValueWrapper ret = isoo.processLeft( op.OperatorString, arguments[i] );
                    if( ret != null ) {
                        result = ret.Value;
                        continue;
                    }
                }
                isoo = arguments[i] as IScriptOperatorOverload;
                if( isoo != null ) {
                    ValueWrapper ret = isoo.processRight( op.OperatorString, arguments[i] );
                    if( ret != null ) {
                        result = ret.Value;
                        continue;
                    }
                }
                result = operatorLambda( result, arguments[i] );
            }
            return result;
        }

        public static bool IsNumericType ( object o ) {   
            switch (Type.GetTypeCode( o.GetType() )) {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
            }
        }

    }
}

