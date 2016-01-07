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

        public Identifier UndefinedIdentifier { get; private set; } /* = new Identifier("undefined");*/
        public Identifier NewIdentifier { get; private set; } /* = new Identifier("new");*/
        public Identifier ExceptionIdentifier { get; private set; } /* = new Identifier("$ex");*/
        public Identifier ArgumentsIdentifier { get; private set; } /* = new Identifier("$args");*/
        public Identifier PromptIdentifier { get; private set; } /* = new Identifier("$prompt");*/
        private ValueWrapper undefinedValue = new ValueWrapper( Undefined.INSTANCE );

        //private Stack<ISyntaxElement> callStack = new Stack<ISyntaxElement>();
        private LinkedList<ISyntaxElement> callStack = new LinkedList<ISyntaxElement>();

        public int StackLimit { get; set; }

        public bool RethrowExceptions { get; set; }

        public Engine () {
            UndefinedIdentifier = new Identifier( "undefined" );
            NewIdentifier = new Identifier( "new" );
            ExceptionIdentifier = new Identifier( "$ex" );
            ArgumentsIdentifier = new Identifier( "$args" );
            PromptIdentifier = new Identifier( "$prompt" );
            //UndefinedVariableBehaviour = UndefinedVariableBehaviour.ReturnRawNull;
            UndefinedVariableBehaviour = UndefinedVariableBehaviour.ThrowException;
            StackLimit = 4096;
            RethrowExceptions = true;


            createDefaultVariables();
            createDefaultOperators();
        }

        public void pushCallStack ( ISyntaxElement caller ) {
            //callStack.Push( caller );
            callStack.AddLast( caller );
            if( callStack.Count > StackLimit ) {
                throw new InvalidOperationException( "stack overflow" );
            }
        }

        public void popCallStack () {
            //callStack.Pop();
            callStack.RemoveLast();
        }

        private T valueNotFound<T> ( string identifierName, T undefinedValue ) {
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

        private void ensureArgCount( string statementName, int requiredCount, object[] args )  {
            if ( args.Length < requiredCount ) {
                throw new InvalidOperationException("'"+statementName+"' statement missing required blocks ("+requiredCount+" needed, "
                        +args.Length+" found)");
            }
        }

        private void ensureArg( string blockName, object existingObj, Identifier assertedId ) {
            if ( !assertedId.Equals( existingObj )) {
                throw new InvalidOperationException("'"+assertedId + " ' expected in '"+blockName+"' block; found '" + existingObj + "' instead");
            }
        }

        /*
        private void ensureBlockSeparator( string blockName, object existingObj) {
            if ( existingObj != BlockSeparator.Instance ) {
                throw new InvalidOperationException("'"+BlockSeparator.Instance
                    + " ' expected in "+blockName+" block; found '" + existingObj + "' instead");
            }
        }
        */

        public void setFunctionArguments( params dynamic[] arguments ) {
            setIdentifierValue( ArgumentsIdentifier, arguments ); // TEMP use proper local var later on
        }

        public string getPrompt() {
            return (string) getIdentifierValueRaw( PromptIdentifier );
        }

        protected void createDefaultVariables () {
            if( UndefinedVariableBehaviour == UndefinedVariableBehaviour.ReturnUndefined ) {
                setIdentifierValueConstant( UndefinedIdentifier, Undefined.INSTANCE );
            }

            setIdentifierValue( PromptIdentifier, "> " );

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

            Identifier //
                whileId = new Identifier( "while" ),
                ifId = new Identifier( "if" ),
                elseId = new Identifier( "else" ),
                catchId = new Identifier( "catch" ),
                finallyId = new Identifier( "finally" );


            // note: below code fails to autoformat properly in Xamarin Studio IDE
            Dictionary<string,object> defaultVarsMap = new Dictionary<string,object> {
                //// common identifiers
                // value-type (literal) default vars
                { "null", null },
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
                //// basic syntax
                // method-type (delegate) default vars
                // maybe todo switch?
                { "breakpoint", new MethodWrapper( (objs) => {
                    return null; // note: place breakpoint here when debugging
                } ) },
                { "undeclare", new MethodWrapper((objs)=>{
                    foreach(object obj in objs) {
                    Identifier id = objs[0] as Identifier;
                    if ( id != null ) {
                        removeIdentifier( id );
                    } else {
                        IEvaluable iEva = objs[0] as IEvaluable;
                        object o = objs[0];
                        if ( iEva != null ) {
                            o = iEva.eval(this);
                            string s = o as string;
                            if ( s != null ) {
                                removeIdentifier( s );
                            } else {
                                throw new InvalidOperationException("'delete' called with invalid parameter '"+o+"'");                                    
                            }
                        }
                    }
                    }
                    return null;
                }, HoldType.All)},
                { "for", new MethodWrapper( (objs ) => {
                    ensureArgCount( "for", 2, objs);
                    ISyntaxGroup cb = objs[0] as ISyntaxGroup;
                    IList<IEvaluable> forList = cb.getEvaluableList();
                    IEvaluable //
                    body = objs[1] as IEvaluable,
                    init = forList[0] as IEvaluable,
                    condition = forList[1] as IEvaluable,
                    step = null; // not guaranteed due to the way '(;;)' is parsed (no block created after last semicolon)

                    SyntaxGroup sg = condition as SyntaxGroup;
                    if ( sg != null && sg.isEmpty() ) {
                        condition = null;
                    }
                    if ( forList.Count > 2 ) {
                        step = forList[2] as IEvaluable;
                    }

                    IExecutionFlow ef;

                    init.eval( this );
                    // minor optimiziations for tight loops follow; note that the loop is actually *expected* to have some body here
                    if ( condition == null ) {
                        if ( step == null ) { // rare case
                            while( true ) {
                                ef = body.eval( this ) as IExecutionFlow;
                                if ( ef != null ) {
                                    return ef.getLoopValue();
                                }
                            }
                        } // else
                        while( true ) {
                            ef = body.eval( this ) as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                            if ( step != null ) {
                                step.eval( this );
                            }
                        }
                    } // else 

                    if ( step == null ) {
                        while( ( condition.eval( this ) as bool? ) ?? false ) {
                            ef = body.eval( this ) as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                        }                        
                    } else {
                        while( ( condition.eval( this ) as bool? ) ?? false ) {
                            ef = body.eval( this ) as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                            step.eval( this );
                        }
                    }
                    return null;
                }, HoldType.All ) },
                // "while" is a bit further in this method
                { "do", new MethodWrapper( (objs ) => {
                    ensureArgCount( "do", 3, objs);
                    ensureArg( "do", objs[1], whileId );

                    IEvaluable //
                    body = objs[0] as IEvaluable,
                    condition = objs[2] as IEvaluable;

                    object ret;
                    IExecutionFlow ef;
                    SyntaxGroup cb = condition as SyntaxGroup;
                    if ( cb != null && cb.isEmpty() ) {
                        while(true) {
                            ret = body.eval( this );
                            ef = ret as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                        }
                    } // else

                        do {
                            ret = body.eval( this );
                            ef = ret as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                        } while ( ( condition.eval( this ) as bool? ) ?? false );
                    return null;
                }, HoldType.All ) },
                { "break", new MethodWrapper(
                    (objs) => new ExecutionBreak( MiscUtils.unwrap(objs) ) )
                },
                { "return", new MethodWrapper(
                    (objs) => new ExecutionReturn( MiscUtils.unwrap(objs) ) )
                },
                { "exit", new MethodWrapper( (objs) => {
                    throw new ScriptExitException( MiscUtils.unwrap(objs) );
                } ) },
                { "throw", new MethodWrapper( (objs) => {
                    throw (Exception) objs[0];
                } ) },
                { "try", new MethodWrapper( (objs) => {
                    ensureArgCount( "try", 1, objs);
                    IEvaluable body = objs[0] as IEvaluable, catcher, finaller;
                    SyntaxGroup exceptionMask;
                    List<IEvaluable> exceptionMaskList;
                    Identifier exceptionIdentifier;
                    Type exceptionMaskType;

                    switch( objs.Length ) {
                    case 1:
                        try {
                            return body.eval(this);
                        } catch ( Exception ex ) {
                            return ex;
                        }
                    case 3:
                        
                        Identifier id = objs[1] as Identifier;
                        if ( catchId.Equals(id)) {
                            catcher = objs[2] as IEvaluable;
                            try {
                                return body.eval(this);
                            } catch ( Exception ex ) {
                                setIdentifierValue(ExceptionIdentifier, ex );
                                return catcher.eval(this);
                            }
                        } else if ( finallyId.Equals(id)) {
                            finaller = objs[2] as IEvaluable;
                            try {
                                return body.eval(this);
                            } finally {
                                finaller.eval(this);
                            }
                        }
                        throw new InvalidOperationException("'catch' or 'finally' expected in 'try' block; found '" + objs[1] + "' instead");
                    case 4:
                        ensureArg( "try", objs[1], catchId );
                        exceptionMask = objs[2] as SyntaxGroup;
                        if ( exceptionMask == null ) {
                            throw new InvalidOperationException("exception mask CodeBlock expected in 'try' block; found '"+objs[2]+"' instead");
                        }
                        exceptionMaskList = exceptionMask.getArgumentList();
                        int index = exceptionMaskList.Count - 1;
                        exceptionIdentifier = exceptionMaskList[index] as Identifier;
                        if ( exceptionIdentifier == null ) {
                            throw new InvalidOperationException("exception identifier expected in 'catch' block; found '"+exceptionMaskList[1]+"' instead");
                        }
                        exceptionMaskList.RemoveAt(index);
                        exceptionMaskType = MiscUtils.getTypeFor( exceptionMask.eval(this) );
                        if ( exceptionMaskType == null ) {
                            throw new InvalidOperationException("unknown exception Type '"+exceptionMaskList+"in 'catch' block");
                        }
                        exceptionMaskList.Add(exceptionIdentifier);
                            
                        catcher = objs[3] as IEvaluable;
                        try {
                            return body.eval(this);
                        } catch ( Exception ex ) {
                            Type t = ex.GetType();
                            if ( t.IsSubclassOf(exceptionMaskType) || t == exceptionMaskType ) {
                                setIdentifierValue(exceptionIdentifier, ex );
                                return catcher.eval(this);
                            }
                            throw ex;
                        }
                    case 5:
                        ensureArg( "try", objs[1], catchId );

                        catcher = objs[2] as IEvaluable;
                        ensureArg( "try", objs[3], finallyId );
                        finaller = objs[4] as IEvaluable;

                        try {
                            return body.eval(this);
                        } catch ( Exception ex ) {
                            setIdentifierValue(ExceptionIdentifier, ex );
                            return catcher.eval(this);
                        } finally {
                            finaller.eval(this);
                        }
                    case 6:
                        ensureArg( "try", objs[1], catchId );
                        exceptionMask = objs[2] as SyntaxGroup;
                        if ( exceptionMask == null ) {
                            throw new InvalidOperationException("exception mask CodeBlock expected in 'try' block; found '"+objs[2]+"' instead");
                        }
                        exceptionMaskList = exceptionMask.getArgumentList();
                        exceptionMaskType = MiscUtils.getTypeFor( exceptionMaskList[0] );
                        if ( exceptionMaskType == null ) {
                            throw new InvalidOperationException("unknown exception Type '"+exceptionMaskList[0]+"in 'catch' block");
                        }
                        exceptionIdentifier = exceptionMaskList[1] as Identifier;
                        if ( exceptionIdentifier == null ) {
                            throw new InvalidOperationException("exception identifier expected in 'catch' block; found '"+exceptionMaskList[1]+"' instead");
                        }

                        catcher = objs[3] as IEvaluable;
                        ensureArg( "try", objs[4], finallyId );
                        finaller = objs[5] as IEvaluable;

                        try {
                            return body.eval(this);
                        } catch ( Exception ex ) {
                            Type t = ex.GetType();
                            if ( t.IsSubclassOf(exceptionMaskType) || t == exceptionMaskType ) {
                                setIdentifierValue(exceptionIdentifier, ex );
                                return catcher.eval(this);
                            }
                            throw ex;
                        } finally {
                            finaller.eval(this);
                        }
                    }
                    throw new InvalidOperationException("mismatched amount of arguments (found "+objs.Length+"; expected 1, 3, 4, 5 or 6) in 'try' block");
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
            setIdentifierValueConstant( ifId, new MethodWrapper( (objs ) => {
                ensureArgCount( "if", 2, objs);
                int len = objs.Length;
                bool hasLoneElse = (len % 4 == 0);
                for( int i = 0; i < len; i += 2 ) {
                    if ( ( ((IEvaluable)objs[i]).eval(this) as bool? ) ?? false ) {
                        return ((IEvaluable)objs[i+1]).eval(this);
                    }
                    i += 2;
                    if ( i >= len )
                        return null;
                    ensureArg( "if", objs[i], elseId );
                    if ( hasLoneElse ) {
                        if ( i + 2 == len ) {
                            return ((IEvaluable)objs[i+1]).eval(this);
                        }
                    } else {
                        ensureArg( "if", objs[i+1], ifId );
                    }
                }
                return null;
            }, HoldType.All ) );
            setIdentifierValueConstant ( whileId, new MethodWrapper( (objs ) => {
                ensureArgCount( "while", 2, objs);
                IEvaluable //
                condition = objs[0] as IEvaluable,
                body = objs[1] as IEvaluable;

                object ret;
                IExecutionFlow ef;

                while( ( condition.eval( this ) as bool? ) ?? false ) {
                    ret = body.eval( this );
                    ef = ret as IExecutionFlow;
                    if ( ef != null ) {
                         return ef.getLoopValue();
                    }
                }
                return null;
            }, HoldType.All ));

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
            SyntaxGroup cb = m as SyntaxGroup;
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
                //// internal script engine operators
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
                    //null
                    (n,m) => { // note: maybe it's not the best idea, but I leave it here for reference & future generations
                        IList list = n as IList;
                        if ( list == null ) {
                            list = new ValueList();
                            list.Add( MiscUtils.getTypeFor(n));
                        }
                        list.Add( MiscUtils.getTypeFor(m));
                        return list;
                    }
                ), // 'typeof' operator
                //new Operator( "=>"),
                new Operator( "@", (n ) => "", (n,m) => m), // suppress return operator
                //new Operator( "*&", () => callStack.Last.Previous.Previous.Value ),
                ////regular unary/nary operators
                new Operator( "+",
                    (n ) => +n,
                    (n, m ) => n + m ),
                new Operator( "-",
                    (n ) => -n,
                    (n, m ) => n - m ),
                new Operator( "!", (n ) => !n, null ),
                new Operator( "~", (n ) => ~n, null ),
                new Operator( "::", (n) => new ValueList(1){n},
                    (n,m)=>{
                        IList list = n as IList ;
                        if ( list == null ) {
                            list = new ValueList(1){n};
                        }
                        list.Add(m);
                        return list;
                    }),
                new Operator( "=!", (n ) => {
                    ValueWrapper vw = getIdentifierValue( n );
                    dynamic val = vw.Value;
                    val = !val;
                    setIdentifierValue( n, val );
                    return val;
                }, null, HoldType.First ),
                new Operator( "=???", null, (n ) => {
                    dynamic val = MiscUtils.getRandomForType(getIdentifierValue(n).Value.GetType());
                    setIdentifierValue( n, val );
                    return val;
                }, null, HoldType.First, Associativity.LeftToRight ),
                new Operator( "???",
                    () => MiscUtils.RNG.NextDouble(),
                    (n) => MiscUtils.RNG.Next(n),
                    (n,m) => MiscUtils.RNG.Next(n,m),
                    HoldType.None, Associativity.LeftToRight
                ),
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
                new Operator( "||", null, (n, m ) => ((IEvaluable)n).eval(this) || ((IEvaluable)m).eval(this), HoldType.All ), // short-circuit op
                new Operator( "&&", null, (n, m ) => ((IEvaluable)n).eval(this) && ((IEvaluable)m).eval(this), HoldType.All ), // short-circuit op
                new Operator( "==", null, (n, m ) => n == m ),
                new Operator( "!=", null, (n, m ) => n != m ),
                new Operator( ">", null, (n, m ) => n > m ),
                new Operator( ">=", null, (n, m ) => n >= m ),
                new Operator( "<", null, (n, m ) => n < m ),
                new Operator( "<=", null, (n, m ) => n <= m ),
                new Operator( "??", null, (n, m ) => n ?? m ),
                // TODO ternary as "?:" ?
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
            Operator indexer = new Operator( "[]", null, (n, m ) => n[m] );
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
            ExecutionReturn er = ret as ExecutionReturn;
            if ( er != null ) {
                ret = er.getValue();
            }
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
            callStack.Clear();
            object ret;
            try {
                ret = iEvaluable.eval( this );
            } catch ( ScriptExitException ex ) {
                ret = ex.ReturnValue;
            } catch ( Exception ex ) {
                ret = ex;
                if( RethrowExceptions ) {
                    throw ex;
                }
            }
            return wrapRetVal( ret );
        }

        public object exec ( IExecutable iExecutable, params dynamic[] arguments ) {
            callStack.Clear();
            object ret;
            try {
                ret = iExecutable.exec( this, arguments );
            } catch ( ScriptExitException ex ) {
                ret = ex.ReturnValue;
            } catch ( Exception ex ) {
                ret = ex;
                if( RethrowExceptions ) {
                    throw ex;
                }
            }
            return wrapRetVal( ret );
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
            return new SyntaxGroup( ises ).eval( this );
        }

        public void loop() {
            Console.Write( getPrompt() );
            for( string line = Console.ReadLine(); line != null && line.Length != 0; line = Console.ReadLine() ) {
                try {
                    Console.WriteLine( MiscUtils.toString( eval( line ) ) );
                } catch (Exception ex) {
                    Console.WriteLine( Test.exceptionToString( ex ) );
                }
                Console.Write( getPrompt() );
            }
        }
    }
}

