using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace vax.vaxqript {
    public class Engine {
        private ValueWrapper retVal = new ValueWrapper( null );

        private Dictionary<Identifier, dynamic> globalVarMap = new Dictionary<Identifier,dynamic>();
        private Dictionary<string,Operator> operatorMap = new Dictionary<string,Operator>();
        private Dictionary<string,Identifier> identifierMap = new Dictionary<string,Identifier>();

        public UndefinedVariableBehaviour UndefinedVariableBehaviour { get; set; }

        private Identifier undefinedIdentifier = new Identifier( "undefined" );
        private ValueWrapper undefinedValue = new ValueWrapper( Undefined.INSTANCE );

        private int stackCount = 0;

        public int StackLimit { get; set; }

        public Engine () {
            UndefinedVariableBehaviour = UndefinedVariableBehaviour.ReturnRawNull;
            StackLimit = 4096;
            createDefaultVariables();
            createDefaultOperators();
        }

        public void increaseStackCount () {
            stackCount++;
            if( stackCount > StackLimit ) {
                throw new InvalidOperationException( "stack overflow" );
            }
        }

        public void decreaseStackCount () {
            stackCount--;
            if( stackCount < 0 ) {
                throw new InvalidOperationException( "stack underflow" );
            }
        }

        private T  valueNotFound<T> ( string identifierName, T undefinedValue ) {
            switch (UndefinedVariableBehaviour) {
            case UndefinedVariableBehaviour.ReturnRawNull:
                return default(T);
            case UndefinedVariableBehaviour.ReturnUndefined:
                return undefinedValue;
            case UndefinedVariableBehaviour.ThrowException:
                throw new InvalidOperationException( "identifier '" + identifierName + "' not defined yet" );
            default:
                throw new InvalidOperationException( "unknown/unsupported UndefinedVariableBehaviour '" + UndefinedVariableBehaviour + "'" );
            }
        }

        public Identifier getIdentifier ( string identifierName ) {
            Identifier ret;
            if( identifierMap.TryGetValue( identifierName, out ret ) ) {
                return ret;
            }
            return valueNotFound( identifierName, undefinedIdentifier );
        }

        public ValueWrapper getIdentifierValue ( string identifierName ) {
            return getIdentifierValue( getIdentifier( identifierName ) );
        }

        public ValueWrapper getIdentifierValue ( Identifier identifier ) {
            object ret;
            if( globalVarMap.TryGetValue( identifier, out ret ) ) {
                ValueWrapper vw = ret as ValueWrapper;
                return ( vw != null ) ? vw : new ValueWrapper( ret );
            }
            return valueNotFound( identifier.Name, undefinedValue );
        }

        protected object getIdentifierValueRaw ( Identifier identifier ) {
            object ret;
            if( globalVarMap.TryGetValue( identifier, out ret ) ) {
                return ret;
            }
            return valueNotFound( identifier.Name, Undefined.INSTANCE );
        }

        public Identifier setIdentifierValue ( string identifierName, object value ) {
            Identifier ret;
            if( !identifierMap.TryGetValue( identifierName, out ret ) ) {
                ret = new Identifier( identifierName );
                identifierMap[identifierName] = ret;
            }

            _setIdentifierValue( ret, value );
            return ret;
        }

        public T setIdentifierValue<T> ( Identifier identifier, T value ) {
            identifierMap[identifier.Name] = identifier;
            _setIdentifierValue( identifier, value );
            return value;
        }
        /*
        public void setIdentifierValue ( Identifier identifier, object value ) {
            identifierMap[identifier.Name] = identifier;
            _setIdentifierValue( identifier, value );
        }*/

        private T _setIdentifierValue<T> ( Identifier identifier, T value ) {
            globalVarMap[identifier] = value; // TODO support access modifiers & levels etc
            return value;
        }

        protected void createDefaultVariables () {
            if( UndefinedVariableBehaviour == UndefinedVariableBehaviour.ReturnUndefined ) {
                setIdentifierValue( undefinedIdentifier, Undefined.INSTANCE );
            }

            // value-type (literal) default vars
            setIdentifierValue( "null", null );
            setIdentifierValue( "true", true );
            setIdentifierValue( "false", false );
            setIdentifierValue( "Infinity", float.PositiveInfinity );
            setIdentifierValue( "NaN", float.NaN );

            setIdentifierValue( "$engine", this );
            setIdentifierValue( "$ret", retVal );

            // method-type (delegate) default vars
            //// basic syntax
            setIdentifierValue( "new", new MethodWrapper( (objs ) => {
                ValueList args = ( objs.Length > 1 ) ? objs[1] as ValueList : null; // only permissible objs[1] type now

                Type t = objs[0] as Type;
                if( t != null ) { // TODO: test if this handles structs well/at all
                    if( t.IsPrimitive ) {
                        t = typeof(Nullable<>).MakeGenericType( t );
                    }
                    Type[] types = ( args != null ) ? MiscUtils.toTypes( args ) : MiscUtils.NO_ARGUMENTS;
                    ConstructorInfo ci = t.GetConstructor( types );
                    if( ci == null ) {
                        throw new InvalidOperationException( "constructor " + t + "(" + MiscUtils.join( ",", types ) + ") not found" );
                    }
                    return ci.Invoke( args.ToArray() );
                }
                return null;
                //return ( ( objs[0] as bool? ) ?? false ) ? objs[1] : null;
            } ) );
            setIdentifierValue( "if", new MethodWrapper( (objs ) => { // TODO implement 'else'
                return ( ( objs[0] as bool? ) ?? false ) ? objs[1] : null;
            } ) );
            setIdentifierValue( "while", new MethodWrapper( (objs ) => { // TODO implement 'else'
                IEvaluable //
                condition = objs[0] as IEvaluable,
                body = objs[1] as IEvaluable;
                while( ( condition.eval( this ) as bool? ) ?? false ) {
                    body.eval( this ); // return is ignored here
                }
                return null; // TODO implement 'return' as loop breaker here
            }, HoldType.All ) );
            setIdentifierValue( "for", new MethodWrapper( (objs ) => { // TODO implement 'else'
                IEvaluable //
                init = objs[0] as IEvaluable,
                condition = objs[1] as IEvaluable,
                step = objs[2]as IEvaluable,
                body = objs[3] as IEvaluable;
                for( init.eval( this ); ( condition.eval( this ) as bool? ) ?? false; step.eval( this ) ) {
                    body.eval( this );
                }
                return null; // TODO implement 'return' as loop breaker here
            }, HoldType.All ) );

            //// utility methods
            setIdentifierValue( "print", new MethodWrapper( (objs ) => {
                foreach( object o in objs ) {
                    Console.Write( ( o == null ) ? "null" : o );
                }
                return null;
            } ) );
            setIdentifierValue( "println", new MethodWrapper( (objs ) => {
                foreach( object o in objs ) {
                    Console.WriteLine( ( o == null ) ? "null" : o );
                }
                return null;
            } ) );

            // note: by default, with 'func(arg0,arg1...)' syntax, obj[0] contains *all* arguments passed, wrapped as a CodeBlock;
            // to pass the arguments *directly*, use 'func arg0 arg1'
            // - this is *strictly* required for composite (multi-block) methods (like 'for', 'while' etc) to work at all!
        }

        private Func<dynamic,dynamic> lambda2 ( Func<dynamic,dynamic> func ) {
            return func; // pseudo-casting needed for dynamic dispatch
        }

        private Func<dynamic,dynamic,dynamic> lambda3 ( Func<dynamic,dynamic, dynamic> func ) {
            return func; // pseudo-casting needed for dynamic dispatch
        }

        private object wrappedLambda2 ( Identifier n, Func<dynamic,dynamic> func ) {
            dynamic dyn = getIdentifierValueRaw( n );
            dyn = func( dyn );
            _setIdentifierValue( n, dyn );
            return dyn;
        }

        private object wrappedLambda3 ( Identifier n, dynamic m, Func<dynamic,dynamic, dynamic> func ) {
            dynamic dyn = getIdentifierValueRaw( n );
            dyn = func( dyn, m );
            _setIdentifierValue( n, dyn );
            return dyn;
        }

        private Operator createAssignmentOperator ( string opString, Func<dynamic,dynamic> unary, Func<dynamic,dynamic,dynamic> nary ) {
            return new Operator( opString,
                ( unary != null ) ? lambda2( (n ) => wrappedLambda2( n, unary ) ) : null,
                ( nary != null ) ? lambda3( (n, m ) => wrappedLambda3( n, m, nary ) ) : null,
                HoldType.First );
        }


        protected void createDefaultOperators () {
            Operator[] defaultOperators = {
                new Operator( ".", (n ) => { // TODO implement
                    return null;
                }, (n, m ) => {
                    return null;
                } ),
                new Operator( "+",
                    (n ) => +n,
                    (n, m ) => n + m ),
                new Operator( "-",
                    (n ) => -n,
                    (n, m ) => n - m ),
                new Operator( "'", (n ) => { // Identifier<->string operator; coincidentally doubles as "alternative string quotes"
                    ValueWrapper vw = n as ValueWrapper;
                    if( vw != null ) {
                        n = vw.Value; // note: not recursive!
                    }
                    string s = n as string;
                    if( s != null ) {
                        return getIdentifier( s );
                    }
                    Identifier id = n as Identifier;
                    if( id != null ) {
                        return id.Name;
                    }
                    return n;
                }, null, HoldType.All ),
                new Operator( "`", (n ) => { // hold operator
                    ValueWrapper vw = n as ValueWrapper;
                    if( vw != null ) {
                        n = vw.Value; // note: not recursive!
                    }
                    Identifier id = n as Identifier;
                    return ( id != null ) ? getIdentifierValue( id ) : Wrapper.wrap( n );
                }, null, HoldType.All ),
                new Operator( "?", (n ) => { // typeof operator
                    ValueWrapper vw = n as ValueWrapper;
                    if( vw != null ) {
                        n = vw.Value; // note: not recursive!
                    }
                    return n.GetType();
                }, null, HoldType.None ),
                new Operator( "!", (n ) => !n, null ),
                new Operator( "~", (n ) => ~n, null ),
                createAssignmentOperator( "++", (n ) => ++n, null ),
                createAssignmentOperator( "--", (n ) => --n, null ),
                new Operator( "*", null, (n, m ) => n * m ),
                new Operator( "/", null, (n, m ) => n / m ),
                new Operator( "%", null, (n, m ) => n % m ),
                new Operator( "|", null, (n, m ) => n | m ),
                new Operator( "||", null, (n, m ) => n || m ),
                new Operator( "&", null, (n, m ) => n & m ),
                new Operator( "&&", null, (n, m ) => n && m ),
                new Operator( "^", null, (n, m ) => n ^ m ),
                new Operator( "<<", null, (n, m ) => n << m ),
                new Operator( ">>", null, (n, m ) => n >> m ),
                new Operator( "==", null, (n, m ) => n == m ),
                new Operator( "!=", null, (n, m ) => n != m ),
                new Operator( ">", null, (n, m ) => n > m ),
                new Operator( ">=", null, (n, m ) => n >= m ),
                new Operator( "<", null, (n, m ) => n < m ),
                new Operator( "<=", null, (n, m ) => n <= m ),
                new Operator( "??", null, (n, m ) => n ?? m ),
                // TODO ternary as "?:"
                new Operator( "=", null, (n, m ) => {
                    setIdentifierValue( m, n );
                    return n;
                }, HoldType.AllButFirst, Associativity.RightToLeft ),
                new Operator( ":=", null, (n, m ) => {
                    setIdentifierValue( m, n );
                    return n;
                }, HoldType.All, Associativity.RightToLeft ),
                new Operator( "+=", null, (n, m ) => {
                    // since we have HoldType.First here
                    Identifier id = n as Identifier;
                    if( id != null ) {
                        return wrappedLambda3( id, m, lambda3(
                            (n2, m2 ) => n2 + m2
                        ) );
                    }
                        
                    ValueWrapper wrap = n as ValueWrapper;
                    dynamic o = wrap.Value;
                    if( o is Stack ) {
                        o.Push( m );
                        return n;
                    }
                    if( o is Queue ) {
                        o.Enqueue( m );
                        return n;
                    }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if( o is ICollection ) {
                        o.Add( m );
                        return n;
                    }
                    throw new InvalidOperationException();
                }, HoldType.First ),
                new Operator( "-=", null, (n, m ) => {
                    // since we have HoldType.First here
                    Identifier id = n as Identifier;
                    if( id != null ) {
                        return wrappedLambda3( id, m, lambda3(
                            (n2, m2 ) => n2 - m2
                        ) );
                    }

                    ValueWrapper wrap = n as ValueWrapper;
                    dynamic o = wrap.Value;

                    if( o is Stack ) {
                        o.Pop( m );
                        return n;
                    }
                    if( o is Queue ) {
                        o.Dequeue( m );
                        return n;
                    }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if( o is ICollection ) {
                        o.Remove( m );
                        return n;
                    }
                    throw new InvalidOperationException();
                }, HoldType.First ),
                createAssignmentOperator( "*=", null, (n, m ) => n * m ),
                createAssignmentOperator( "/=", null, (n, m ) => n / m ),
                createAssignmentOperator( "%=", null, (n, m ) => n % m ),
                createAssignmentOperator( "&=", null, (n, m ) => n & m ),
                createAssignmentOperator( "|=", null, (n, m ) => n | m ),
                createAssignmentOperator( "^=", null, (n, m ) => n ^ m ),
                createAssignmentOperator( "<<=", null, (n, m ) => n << m ),
                createAssignmentOperator( ">>=", null, (n, m ) => n >> m ),
            };
            foreach( Operator op in defaultOperators ) {
                addOperator( op );
            }
            Operator indexer = new Operator( "[]", null,
                                   (n, m ) => n[m] );
            /*
            Operator indexerSet = new Operator( "[]=", null,
                (n, m ) => {
                //if( n.GetType() == typeof(Array) ) {
                //return ( (Array) n ).GetValue( m ); // a bit more error-resistant and error-sane than dynamic indexer use
                //}
                return n[m];
            } );
            */
            /*
                new Operator( "[,]", null, (n, m ) => {
                    Console.WriteLine( ""+n.GetType());
                    if( n.GetType() == typeof(Array) ) {
                        return ( (Array) n ).GetValue( m );
                    }
                    return n[m];
                } ),
                */
            addOperator( indexer );
            addOperator( indexer, "]" );
            addOperator( indexer, "[" );
            addOperator( indexer, "][" );
            //addOperator( indexerSet );
        }

        public void addOperator ( Operator op ) {
            operatorMap[op.OperatorString] = op;
        }

        public void addOperator ( Operator op, string alias ) {
            operatorMap[alias] = op;
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

        protected object wrapRetVal ( object ret ) {
            ValueWrapper vw = ret as ValueWrapper;
            retVal.Value = ( vw != null ) ? vw.Value : ret;
            return ret;
        }

        public object eval ( string s ) {
            return eval( new StringLexer( s, this ) ); // retVal set inside
        }

        public object eval ( Lexer lexer ) {
            return eval( lexer.createLinearSyntax() ); // retVal set inside
        }

        public object eval ( LinearSyntax linearSyntax ) {
            return eval( linearSyntax.buildParseTree() ); // retVal set inside
        }

        public object eval ( IEvaluable iEvaluable ) {
            return wrapRetVal( iEvaluable.eval( this ) );
        }

        public object exec ( IExecutable iExecutable, params dynamic[] arguments ) {
            return wrapRetVal( iExecutable.exec( this, arguments ) );
        }

        /**
         * Note: raw values get wrapped automagically; ISyntaxElement-s ain't.
         */
        public dynamic debugApplyStringOperator ( String opString, params dynamic[] arguments ) {
            int max = arguments.Length;
            ISyntaxElement[] ises = new ISyntaxElement[ max + 1];
            ises[0] = operatorValueOf( opString );
            for( int i = 0; i < max; i++ ) {
                dynamic arg = arguments[i];
                ISyntaxElement ise = arg as ISyntaxElement;
                ises[i + 1] = ( ise != null ) ? ise : Wrapper.wrap( arg );
            }
            //Console.WriteLine( string.Join<ISyntaxElement>( ",", ises ) ); // in case of debug
            return new CodeBlock( ises ).eval( this );
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

