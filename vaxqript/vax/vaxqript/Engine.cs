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
        private HashSet<Identifier> constantIdentifierSet = new HashSet<Identifier>();

        public UndefinedVariableBehaviour UndefinedVariableBehaviour { get; set; }

        public Identifier UndefinedIdentifier { get; private set; } /* = new Identifier("new");*/
        public Identifier NewIdentifier { get; private set; } /* = new Identifier("new");*/
        private ValueWrapper undefinedValue = new ValueWrapper( Undefined.INSTANCE );

        private int stackCount = 0;

        public int StackLimit { get; set; }

        public Engine () {
            UndefinedIdentifier = new Identifier( "undefined" );
            NewIdentifier = new Identifier( "new" );
            //UndefinedVariableBehaviour = UndefinedVariableBehaviour.ReturnRawNull;
            UndefinedVariableBehaviour = UndefinedVariableBehaviour.ThrowException;
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

        public void removeIdentifier( string identifierName ) {
            Identifier ret;
            if( identifierMap.TryGetValue( identifierName, out ret ) ) {
                removeIdentifier( ret );
            }
        }

        public void removeIdentifier( Identifier identifier ) {
            identifierMap.Remove( identifier.Name );
            globalVarMap.Remove( identifier );
        }

        public bool tryGetIdentifierValue( Identifier identifier, out object ret )  {
            return globalVarMap.TryGetValue( identifier, out ret );
        }

        public Identifier getIdentifier ( string identifierName ) {
            Identifier ret;
            if( identifierMap.TryGetValue( identifierName, out ret ) ) {
                return ret;
            }
            return valueNotFound( identifierName, UndefinedIdentifier );
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

        public Identifier setIdentifierValueConstant ( string identifierName, object value ) {
            Identifier id = setIdentifierValue( identifierName, value );
            constantIdentifierSet.Add( id );
            return id;
        }

        public T setIdentifierValue<T> ( Identifier identifier, T value ) {
            identifierMap[identifier.Name] = identifier;
            _setIdentifierValue( identifier, value );
            return value;
        }

        public T setIdentifierValueConstant<T> ( Identifier identifier, T value ) {
            T ret = setIdentifierValue( identifier, value );
            constantIdentifierSet.Add( identifier );
            return ret;
        }

        private T _setIdentifierValue<T> ( Identifier identifier, T value ) {
            if( constantIdentifierSet.Contains( identifier ) ) {
                throw new InvalidOperationException( "identifier '" + identifier
                    + "' is declared as constant; remove it first before assigning other value to it" );
            }
            globalVarMap[identifier] = value; // TODO support access modifiers & levels etc
            return value;
        }

        public string globalVarsToString() {
            return "ENGINE VARS:\n" + MiscUtils.join( "\n", globalVarMap );
        }

        protected void createDefaultVariables () {
            if( UndefinedVariableBehaviour == UndefinedVariableBehaviour.ReturnUndefined ) {
                setIdentifierValueConstant( UndefinedIdentifier, Undefined.INSTANCE );
            }

            setIdentifierValueConstant( NewIdentifier, new MethodWrapper( (objs ) => {
                if ( objs.Length == 0 )
                    throw new InvalidOperationException("'new' command missing required argument (type)");
                object typeObj = objs[0];
                ValueList args = ( objs.Length > 1 ) ? objs[1] as ValueList : null; // only permissible objs[1] type now
                Type t = typeObj as Type;
                if ( t == null ) {
                    CompositeIdentifier compi = typeObj as CompositeIdentifier;
                    if ( compi == null ) {
                        throw new InvalidOperationException("object '"+MiscUtils.toDebugString(typeObj)
                            +"' used as a Type");
                    }
                    t = compi.toType();
                }
                return MiscUtils.createNew(t, args);
            } ));

            // note: below code fails to autoformat properly in Xamarin Studio IDE
            Dictionary<string,object> defaultVarsMap = new Dictionary<string,object> {
                //// common identifiers
                // value-type (literal) default vars
                { "null",null },
                { "true", true },
                { "false", false },
                { "Infinity", float.PositiveInfinity },
                { "NaN", float.NaN },

                //// common type identifiers (note: no unsigned types/signed byte since they are quite uncommon/unportable [Java])
                { "bool", typeof(bool) },
                { "byte", typeof(byte) },
                { "short", typeof(short) },
                { "int", typeof(int) },
                { "long", typeof(long) },
                { "float", typeof(float) },
                { "double", typeof(double) },

                { "char", typeof(char) },
                { "object", typeof(object) },
                { "string", typeof(string) },

                //// script identifiers
                { "$engine", this },
                { "$ret", retVal },
                 //$args[]
                        //// basic syntax
                        // method-type (delegate) default vars

                    //{ "vars", new MethodWrapper( (objs)=>globalVarsToString() )  }, // DEBUG ONLY!
            { "if", new MethodWrapper( (objs ) => { // TODO implement 'else'
                    if ( objs.Length < 2 )
                        throw new InvalidOperationException("'if' conditional missing required blocks (2 needed, "
                            +objs.Length+" found)");
                return ( ( objs[0] as bool? ) ?? false ) ? objs[1] : null;
                                    } ) },
            { "while", new MethodWrapper( (objs ) => {
                    if ( objs.Length < 2 )
                        throw new InvalidOperationException("'while' loop missing required blocks (2 needed, "
                            +objs.Length+" found)");
                    
                    IEvaluable //
                condition = objs[0] as IEvaluable,
                body = objs[1] as IEvaluable;
                while( ( condition.eval( this ) as bool? ) ?? false ) {
                    body.eval( this ); // return is ignored here
                }
                return null; // TODO implement 'return' as loop breaker here
                                        }, HoldType.All ) },
            { "for", new MethodWrapper( (objs ) => { // TODO implement 'else'
                    if ( objs.Length < 4 )
                        throw new InvalidOperationException("'for' loop missing required blocks (4 needed, "
                            +objs.Length+" found)");
                IEvaluable //
                init = objs[0] as IEvaluable,
                condition = objs[1] as IEvaluable,
                step = objs[2]as IEvaluable,
                body = objs[3] as IEvaluable;
                for( init.eval( this ); ( condition.eval( this ) as bool? ) ?? false; step.eval( this ) ) {
                    body.eval( this );
                }
                return null; // TODO implement 'return' as loop breaker here
                                            }, HoldType.All ) },

            //// utility methods
            { "print", new MethodWrapper( (objs ) => {
                foreach( object o in objs ) {
                    Console.Write( ( o == null ) ? "null" : o );
                }
                return null;
                                                } ) },
            { "println", new MethodWrapper( (objs ) => {
                foreach( object o in objs ) {
                    Console.WriteLine( ( o == null ) ? "null" : o );
                }
                return null;
                                                } ) },
                                                    };
            // note: by default, with 'func(arg0,arg1...)' syntax, obj[0] contains *all* arguments passed, wrapped as a CodeBlock;
            // to pass the arguments *directly*, use 'func arg0 arg1'
            // - this is *strictly* required for composite (multi-block) methods (like 'for', 'while' etc) to work at all!

                                                    foreach( KeyValuePair<string, object> entry in defaultVarsMap ) {
                setIdentifierValueConstant(entry.Key, entry.Value);
                                                    }
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

        private string dotOpExceptionMessage( object n, object m ) {
            return "Identifier-Identifier, CompositeIdentifier-Identifier or object-string pair expected; found "
            + MiscUtils.toDebugString( n ) + " and " + MiscUtils.toDebugString( m ) + " instead";
        }

        private object execOnBlock(IExecutable iexecutable, object n, object m) {
            CodeBlock cb = m as CodeBlock;
            if ( cb == null ) {
                throw new InvalidOperationException(dotOpExceptionMessage(n,m));
            }
            if ( cb.isEmpty() ) {
                return iexecutable.exec(this,null);
            }
            object cbEval = cb.eval(this);
            ValueList vl = cbEval as ValueList;
            return ( vl != null ) ? iexecutable.exec(this,vl.ToArray()) : iexecutable.exec(this,cbEval);
        }

        private object objectMethodHandler( object n, object m, Identifier idM ) {
            IEvaluable ievaN = n as IEvaluable;
            if ( ievaN == null ) {
                ObjectMethod om = n as ObjectMethod;
                if ( om == null ) {
                    throw new InvalidOperationException(dotOpExceptionMessage(n,m));                                                                                   
                }
                return execOnBlock(om, n, m);
            }
            object oN = ievaN.eval(this);
            Type t = oN as Type;
            if ( t == null ) {
                t = oN.GetType();
            }

            string s;
            object oM;
            if( idM != null ) {
                s = idM.Name;
                oM = null;
            } else {
                IEvaluable ievaM = m as IEvaluable;
                if( ievaM == null ) {
                    throw new InvalidOperationException( dotOpExceptionMessage( n, m ) );                                           
                }
                oM = ievaM.eval( this );
                s = oM as string;
            }
            MethodInfo mi;
            if ( s == null ) {
                mi = oM as MethodInfo;
                if ( mi == null ) {
                    throw new InvalidOperationException(dotOpExceptionMessage(n,m));                                           
                }
            } else {
                mi = t.GetMethod( s );
                if ( mi == null ) {
                    throw new InvalidOperationException("method '"+s+"' not found in type '"+t+"'" );
                }
            }
            return new ObjectMethod( oN, mi );
        }

        protected void createDefaultOperators () {
            Operator[] defaultOperators = {
                // internal script engine operators
                new Operator( ".", null, // method operator
                    (n,m)=>{
                        CompositeIdentifier ci = n as CompositeIdentifier;
                        Identifier idM = m as Identifier;
                        if ( ci == null ) {
                            if ( idM == null ) {
                                return objectMethodHandler(n,m,idM);
                            } // else m is a valid Identifier

                            Identifier idN = n as Identifier;
                            if ( idN == null ) {
                                return objectMethodHandler(n,m,idM);
                            }
                            return new CompositeIdentifier(){ idN, idM };
                        } 
                        if ( idM == null ) {
                            return execOnBlock( ci, n, m );
                        }
                        ci.Add(idM);                                
                        return ci;
                    }, HoldType.All ),
                /*
                new Operator( ".", (n ) => { // method operator
                    return null;
                }, (n, m ) => {
                    Type t = n as Type;
                    if ( t == null ) {
                        t = n.GetType();
                    } else { // n is a Type, so usually it's a static method; TODO implement calling Type# methods
                        n = null;
                    }
                    return new ObjectMethod(n, t.GetMethod( m ) );
                } ),
                */
                /*
                new Operator( "@", (n ) => { // method operator
                    return ((ObjectMethod)n).invoke( null ); // 'null' instead of 'object[0]' for some obscure, ericlipperty reason
                }, (n, m ) => {
                    return ((ObjectMethod)n).invoke( m );
                } ),
                */
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
                new Operator( "?",
                    (n ) => MiscUtils.getTypeFor(n),
                    null
                    /*
                    (n,m) => { // note: maybe it's not the best idea, but I leave it here for reference & future generations
                        IList list = n as IList;
                        if ( list == null ) {
                            list = new ValueList();
                            list.Add( MiscUtils.getTypeFor(n));
                        }
                        list.Add( MiscUtils.getTypeFor(m));
                        return list;
                    }
                */
                ), // 'typeof' operator
                //regular unary/nary operators
                new Operator( "+",
                    (n ) => +n,
                    (n, m ) => n + m ),
                new Operator( "-",
                    (n ) => -n,
                    (n, m ) => n - m ),
                new Operator( "!", (n ) => !n, null ),
                new Operator( "~", (n ) => ~n, null ),
                createAssignmentOperator( "++", (n ) => ++n, null ),
                createAssignmentOperator( "--", (n ) => --n, null ),
                new Operator( "*", null, (n, m ) => n * m ),
                new Operator( "/", null, (n, m ) => n / m ),
                new Operator( "%", null, (n, m ) => n % m ),
                new Operator( "<<", null, (n, m ) => n << m ),
                new Operator( ">>", null, (n, m ) => n >> m ),
                new Operator( "^", null, (n, m ) => n ^ m ),
                new Operator( "|", null, (n, m ) => n | m ),
                new Operator( "&", null, (n, m ) => n & m ),
                new Operator( "||", null, (n, m ) => n || m ),
                new Operator( "&&", null, (n, m ) => n && m ),
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
                    //if ( o is LinkedList<> ) { o.RemoveLast(); return n; }
                    if( o is ICollection ) {
                        o.Remove( m );
                        return n;
                    }
                    throw new InvalidOperationException();
                }, HoldType.First ),
                createAssignmentOperator( "*=", null, (n, m ) => n * m ),
                createAssignmentOperator( "/=", null, (n, m ) => n / m ),
                createAssignmentOperator( "%=", null, (n, m ) => n % m ),
                createAssignmentOperator( "<<=", null, (n, m ) => n << m ),
                createAssignmentOperator( ">>=", null, (n, m ) => n >> m ),
                createAssignmentOperator( "^=", null, (n, m ) => n ^ m ),
                createAssignmentOperator( "&=", null, (n, m ) => n & m ),
                createAssignmentOperator( "|=", null, (n, m ) => n | m ),
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
        }

        /* // some ideas from old engine:
        OP_ForeachForward( ">>", -1, 2, type.OT_Infix ),
        OP_ForeachBackward( "<<", -1, 2, type.OT_Infix ), // last operator in enum
        OP_Ellipsis( "...", -1, 0, type.OT_Prefix ), // vararg def
        OP_InstanceOf( "??", 0, 2, type.OT_Infix ),
        OP_TernaryStart( "?", 7, type.OT_Ternary ),
        OP_TernaryEnd( ":", 7, type.OT_Ternary ),
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
    }
}

