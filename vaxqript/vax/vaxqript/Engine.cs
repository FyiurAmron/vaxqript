﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace vax.vaxqript {

public class Engine {
    private readonly ValueWrapper retVal = new(null);

    private readonly Dictionary<Identifier, dynamic> globalVarMap = new();
    private readonly Dictionary<string, Operator> operatorMap = new();
    public Dictionary<string, Identifier> identifierMap = new();
    public HashSet<Identifier> constantIdentifierSet = new();

    public UndefinedVariableBehaviour UndefinedVariableBehaviour { get; set; }

    public Identifier UndefinedIdentifier { get; private set; } /* = new Identifier("undefined");*/
    public Identifier NewIdentifier { get; private set; } /* = new Identifier("new");*/
    public Identifier ExceptionIdentifier { get; private set; } /* = new Identifier("$ex");*/
    public Identifier ArgumentsIdentifier { get; private set; } /* = new Identifier("$args");*/
    public Identifier Arguments0Identifier { get; private set; } /* = new Identifier("$args0");*/
    public Identifier PromptIdentifier { get; private set; } /* = new Identifier("$prompt");*/
    private readonly ValueWrapper undefinedValue = new(Undefined.INSTANCE);

    //private Stack<ISyntaxElement> callStack = new Stack<ISyntaxElement>();
    private readonly LinkedList<ISyntaxElement> callStack = new(); // since it's easier to manage in c#
    private readonly LinkedList<Function> functionStack = new();

    public int StackLimit { get; set; }

    public bool RethrowExceptions { get; set; }

    public Engine() {
        UndefinedIdentifier = new Identifier( "undefined" );
        NewIdentifier = new Identifier( "new" );
        ExceptionIdentifier = new Identifier( "$ex" );
        ArgumentsIdentifier = new Identifier( "$args" );
        Arguments0Identifier = new Identifier( "$args0" );
        PromptIdentifier = new Identifier( "$prompt" );
        //UndefinedVariableBehaviour = UndefinedVariableBehaviour.ReturnRawNull;
        UndefinedVariableBehaviour = UndefinedVariableBehaviour.ThrowException;
        StackLimit = 4096;
        RethrowExceptions = true;

        createDefaultVariables();
        createDefaultOperators();
    }

    public void pushCall( ISyntaxElement caller ) {
        callStack.AddLast( caller );
        if ( callStack.Count > StackLimit ) {
            throw new InvalidOperationException( "stack overflow" );
        }
    }

    public void popCall() {
        callStack.RemoveLast();
    }

    public void pushFunction( Function function ) {
        functionStack.AddLast( function );
        if ( functionStack.Count > StackLimit ) {
            throw new InvalidOperationException( "stack overflow" );
        }
    }

    public void popFunction() {
        functionStack.RemoveLast();
        // TODO remove all scoped variables here
    }

    private T valueNotFound<T>( string identifierName, T undefinedValue ) {
        switch ( UndefinedVariableBehaviour ) {
            case UndefinedVariableBehaviour.ReturnRawNull:
                return default(T);
            case UndefinedVariableBehaviour.ReturnUndefined:
                return undefinedValue;
            case UndefinedVariableBehaviour.ThrowException:
                throw new InvalidOperationException( "identifier '" + identifierName + "' not defined yet" );
            default:
                throw new InvalidOperationException( "unknown/unsupported UndefinedVariableBehaviour '" +
                                                     UndefinedVariableBehaviour + "'" );
        }
    }

    public void removeIdentifier( string identifierName ) {
        if ( identifierMap.TryGetValue( identifierName, out Identifier ret ) ) {
            removeIdentifier( ret );
        }
    }

    public void removeIdentifier( Identifier identifier ) {
        identifierMap.Remove( identifier.Name );
        globalVarMap.Remove( identifier );
    }

    public bool tryGetIdentifierValue( Identifier identifier, out object ret ) {
        return globalVarMap.TryGetValue( identifier, out ret );
    }

    public Identifier getIdentifier( string identifierName ) {
        return identifierMap.TryGetValue( identifierName, out Identifier ret )
            ? ret
            : valueNotFound( identifierName, UndefinedIdentifier );
    }

    public ValueWrapper getIdentifierValue( string identifierName ) {
        return getIdentifierValue( getIdentifier( identifierName ) );
    }

    public ValueWrapper getIdentifierValue( Identifier identifier ) {
        if ( globalVarMap.TryGetValue( identifier, out object ret ) ) {
            return ( ret is ValueWrapper vw )
                ? vw
                : new ValueWrapper( ret );
        }

        return valueNotFound( identifier.Name, undefinedValue );
    }

    protected object getIdentifierValueRaw( Identifier identifier ) {
        return globalVarMap.TryGetValue( identifier, out object ret )
            ? ret
            : valueNotFound( identifier.Name, Undefined.INSTANCE );
    }

    public Identifier setIdentifierValue( string identifierName, object value ) {
        if ( !identifierMap.TryGetValue( identifierName, out Identifier ret ) ) {
            ret = new Identifier( identifierName );
            identifierMap[identifierName] = ret;
        }

        _setIdentifierValue( ret, value );
        return ret;
    }

    public Identifier setIdentifierValueConstant( string identifierName, object value ) {
        Identifier id = setIdentifierValue( identifierName, value );
        constantIdentifierSet.Add( id );
        return id;
    }

    public T setIdentifierValue<T>( Identifier identifier, T value ) {
        identifierMap[identifier.Name] = identifier;
        _setIdentifierValue( identifier, value );
        return value;
    }

    public T setIdentifierValueConstant<T>( Identifier identifier, T value ) {
        T ret = setIdentifierValue( identifier, value );
        constantIdentifierSet.Add( identifier );
        return ret;
    }

    private T _setIdentifierValue<T>( Identifier identifier, T value ) {
        if ( constantIdentifierSet.Contains( identifier ) ) {
            throw new InvalidOperationException( "identifier '" + identifier
                                                 + "' is declared as constant; remove it first before assigning other value to it" );
        }

        globalVarMap[identifier] = value; // TODO support access modifiers & levels etc
        return value;
    }

    public string globalVarsToString() {
        return "ENGINE VARS:\n" + MiscUtils.join( globalVarMap, "\n" );
    }

    private static void ensureArgCount( string statementName, int minCount, int maxCount, object[] args ) {
        ensureArgCount( statementName, minCount, maxCount, args.Length );
    }

    private static void ensureArgCount( string statementName, int minCount, int maxCount, ICollection collection ) {
        ensureArgCount( statementName, minCount, maxCount, collection.Count );
    }

    private static void ensureArgCount<T>( string statementName, int minCount, int maxCount,
                                           ICollection<T> collection ) {
        ensureArgCount( statementName, minCount, maxCount, collection.Count );
    }

    private static void ensureArgCount( string statementName, int minCount, int maxCount, int actualLength ) {
        if ( actualLength < minCount ) {
            throw new InvalidOperationException( "'" + statementName + "' statement missing required blocks (" +
                                                 minCount + " needed, "
                                                 + actualLength + " found)" );
        }

        if ( actualLength > maxCount ) {
            throw new InvalidOperationException( "'" + statementName + "' statement having too many blocks (" +
                                                 maxCount + " needed, "
                                                 + actualLength + " found)" );
        }
    }

    private static void ensureArg( string blockName, object existingObj, Identifier assertedId ) {
        if ( !assertedId.Equals( existingObj ) ) {
            throw new InvalidOperationException( "'" + assertedId + " ' expected in '" + blockName +
                                                 "' block; found '" + existingObj + "' instead" );
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
        if ( arguments is not { Length: > 0 } ) {
            return;
        }

        object args0 = arguments[0];
        if ( args0 != null ) {
            setIdentifierValue( Arguments0Identifier, args0 );
        }
    }

    public string getPrompt() {
        return (string) getIdentifierValueRaw( PromptIdentifier );
    }

    private bool evalCondition( IEvaluable condition ) {
        //return ( MiscUtils.unwrap( condition.eval( this ) ) as bool? ) ?? false;
        object o = condition.eval( this );
        bool? b = ( ( o is ValueList vl )
            ? vl.last()
            : MiscUtils.unwrap( condition.eval( this ) ) ) as bool?;
        if ( b == null ) {
            throw new InvalidOperationException( "condition '" + condition + "' didn't eval to bool type" );
        }

        return b.Value;
    }

    private static Function createFunction( object n, object m ) {
        if ( !( n is ISyntaxGroup isg ) ) {
            if ( n is Identifier id ) {
                return new Function( (IEvaluable) m, id );
            }

            throw new InvalidOperationException(
                "'function' requires its first argument to be either the function body (nullary function, only parameter)"
                + "or Identifier/argument list (n-ary)" );
        }

        IList<IEvaluable> list = isg.getEvaluableList();
        // int count = list.Count;
        Identifier[] ids = new Identifier[list.Count];
        int i = 0;
        foreach ( IEvaluable iEva in list ) {
            Identifier id = iEva as Identifier;
            ids[i] = id
                ?? throw new InvalidOperationException(
                    "'function' requires its argument list to be populated with Identifier instances only" );
            i++;
        }

        return new Function( (IEvaluable) m, ids );
    }

    protected void createDefaultVariables() {
        // TODO maybe create a 'sys' var set (similar to that in SC) so that they are both const and 'invisible'?

        if ( UndefinedVariableBehaviour == UndefinedVariableBehaviour.ReturnUndefined ) {
            setIdentifierValueConstant( UndefinedIdentifier, Undefined.INSTANCE );
        }

        setIdentifierValue( PromptIdentifier, "> " );

        setIdentifierValueConstant( NewIdentifier, new MethodWrapper( ( objs ) => {
            if ( objs.Length == 0 )
                throw new InvalidOperationException( "'new' command missing required argument (type)" );
            object typeObj = objs[0];
            ValueList args = ( objs.Length > 1 ) ? objs[1] as ValueList : null; // only permissible objs[1] type now
            Type t = typeObj as Type;
            if ( t == null ) {
                if ( !( typeObj is CompositeIdentifier compi ) ) {
                    throw new InvalidOperationException(
                        $"object '{MiscUtils.toDebugString( typeObj )}' used as a Type" );
                }

                t = compi.toType();
            }

            return MiscUtils.createNew( t, args );
        } ) );

        Identifier //
            whileId = new("while"),
            ifId = new("if"),
            elseId = new("else"),
            catchId = new("catch"),
            finallyId = new("finally");

        // note: below code fails to autoformat properly in Xamarin Studio IDE
        Dictionary<string, object> defaultVarsMap = new() {
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
            {
                "breakpoint", new MethodWrapper( ( objs ) => {
                    return null; // note: place breakpoint here when debugging
                } )
            }, {
                "undeclare", new MethodWrapper( ( objs ) => { // TODO find a better name for this method
                    foreach ( object obj in objs ) {
                        if ( objs[0] is Identifier id ) {
                            removeIdentifier( id );
                        } else {
                            IEvaluable iEva = objs[0] as IEvaluable;
                            object o = objs[0];
                            if ( iEva != null ) {
                                o = iEva.eval( this );
                                if ( o is string s ) {
                                    removeIdentifier( s );
                                } else {
                                    throw new InvalidOperationException(
                                        "'delete' called with invalid parameter '" + o + "'" );
                                }
                            }
                        }
                    }

                    return null;
                }, HoldType.All )
            }, {
                "for", new MethodWrapper( ( objs ) => { // TODO foreach syntax
                    ensureArgCount( "for", 2, 2, objs );
                    ISyntaxGroup cb = objs[0] as ISyntaxGroup;
                    IList<IEvaluable> forList = cb.getEvaluableList();
                    ensureArgCount( "for", 2, 3, forList );
                    IEvaluable //
                        body = objs[1] as IEvaluable,
                        init = forList[0],
                        condition = forList[1],
                        step = null; // not guaranteed due to the way '(;;)' is parsed (if ignoreLastSeparator, no block is created after last semicolon)

                    ISyntaxGroup sg = condition as ISyntaxGroup;
                    if ( sg != null && sg.isEmpty() ) {
                        condition = null;
                    }

                    sg = step as ISyntaxGroup;
                    if ( forList.Count == 3 || ( sg != null && sg.isEmpty() ) ) {
                        step = forList[2];
                    }

                    IExecutionFlow ef;

                    init.eval( this );
                    // minor optimizations for tight loops follow; note that the loop is actually *expected* to have some body here
                    if ( condition == null ) {
                        if ( step == null ) { // rare case
                            while ( true ) {
                                ef = body.eval( this ) as IExecutionFlow;
                                if ( ef != null ) {
                                    return ef.getLoopValue();
                                }
                            }
                        } // else

                        while ( true ) {
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
                        while ( evalCondition( condition ) ) {
                            ef = body.eval( this ) as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }
                        }
                    } else {
                        while ( evalCondition( condition ) ) {
                            ef = body.eval( this ) as IExecutionFlow;
                            if ( ef != null ) {
                                return ef.getLoopValue();
                            }

                            step.eval( this );
                        }
                    }

                    return null;
                }, HoldType.All )
            },
            // "while" is a bit further in this method
            {
                "do", new MethodWrapper( ( objs ) => {
                    ensureArgCount( "do", 3, 3, objs );
                    ensureArg( "do", objs[1], whileId );

                    IEvaluable //
                        body = objs[0] as IEvaluable,
                        condition = objs[2] as IEvaluable;

                    object ret;
                    IExecutionFlow ef;
                    if ( condition is ISyntaxGroup cb && cb.isEmpty() ) {
                        while ( true ) {
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
                    } while ( evalCondition( condition ) );

                    return null;
                }, HoldType.All )
            }, {
                "break", new MethodWrapper(
                    ( objs ) => new ExecutionBreak( MiscUtils.unwrap( objs ) ) )
            }, {
                "return", new MethodWrapper(
                    ( objs ) => new ExecutionReturn( MiscUtils.unwrap( objs ) ) )
            },
            { "exit", new MethodWrapper( ( objs ) => { throw new ScriptExitException( MiscUtils.unwrap( objs ) ); } ) },
            { "throw", new MethodWrapper( ( objs ) => { throw (Exception) objs[0]; } ) }, {
                "try", new MethodWrapper( ( objs ) => {
                    ensureArgCount( "try", 1, 6, objs );
                    IEvaluable body = objs[0] as IEvaluable, catcher, finaller;
                    ISyntaxGroup exceptionMask;
                    IList<IEvaluable> exceptionMaskList;
                    Identifier exceptionIdentifier;
                    Type exceptionMaskType;

                    switch ( objs.Length ) {
                        case 1:
                            try {
                                return body.eval( this );
                            } catch ( Exception ex ) {
                                return ex;
                            }
                        case 3:

                            Identifier id = objs[1] as Identifier;
                            if ( catchId.Equals( id ) ) {
                                catcher = objs[2] as IEvaluable;
                                try {
                                    return body.eval( this );
                                } catch ( Exception ex ) {
                                    setIdentifierValue( ExceptionIdentifier, ex );
                                    return catcher.eval( this );
                                }
                            } else if ( finallyId.Equals( id ) ) {
                                finaller = objs[2] as IEvaluable;
                                try {
                                    return body.eval( this );
                                } finally {
                                    finaller.eval( this );
                                }
                            }

                            throw new InvalidOperationException(
                                "'catch' or 'finally' expected in 'try' block; found '" + objs[1] + "' instead" );
                        case 4:
                            ensureArg( "try", objs[1], catchId );
                            exceptionMask = objs[2] as ISyntaxGroup;
                            if ( exceptionMask == null ) {
                                throw new InvalidOperationException(
                                    "exception mask ISyntaxGroup expected in 'try' block; found '" + objs[2] +
                                    "' instead" );
                            }

                            exceptionMaskList = exceptionMask.getEvaluableList();
                            int index = exceptionMaskList.Count - 1;
                            exceptionIdentifier = exceptionMaskList[index] as Identifier;
                            if ( exceptionIdentifier == null ) {
                                throw new InvalidOperationException(
                                    "exception identifier expected in 'catch' block; found '" + exceptionMaskList[1] +
                                    "' instead" );
                            }

                            exceptionMaskList.RemoveAt( index );
                            exceptionMaskType = MiscUtils.getTypeFor( exceptionMask.eval( this ) );
                            if ( exceptionMaskType == null ) {
                                throw new InvalidOperationException(
                                    "unknown exception Type '" + exceptionMaskList + "in 'catch' block" );
                            }

                            exceptionMaskList.Add( exceptionIdentifier );

                            catcher = objs[3] as IEvaluable;
                            try {
                                return body.eval( this );
                            } catch ( Exception ex ) {
                                Type t = ex.GetType();
                                if ( !t.IsSubclassOf( exceptionMaskType ) && t != exceptionMaskType ) {
                                    throw ex;
                                }

                                setIdentifierValue( exceptionIdentifier, ex );
                                return catcher.eval( this );
                            }
                        case 5:
                            ensureArg( "try", objs[1], catchId );

                            catcher = objs[2] as IEvaluable;
                            ensureArg( "try", objs[3], finallyId );
                            finaller = objs[4] as IEvaluable;

                            try {
                                return body.eval( this );
                            } catch ( Exception ex ) {
                                setIdentifierValue( ExceptionIdentifier, ex );
                                return catcher.eval( this );
                            } finally {
                                finaller.eval( this );
                            }
                        case 6:
                            ensureArg( "try", objs[1], catchId );
                            exceptionMask = objs[2] as ISyntaxGroup;
                            if ( exceptionMask == null ) {
                                throw new InvalidOperationException(
                                    "exception mask ISyntaxGroup expected in 'try' block; found '" + objs[2] +
                                    "' instead" );
                            }

                            exceptionMaskList = exceptionMask.getEvaluableList();
                            exceptionMaskType = MiscUtils.getTypeFor( exceptionMaskList[0] );
                            if ( exceptionMaskType == null ) {
                                throw new InvalidOperationException(
                                    "unknown exception Type '" + exceptionMaskList[0] + "in 'catch' block" );
                            }

                            exceptionIdentifier = exceptionMaskList[1] as Identifier;
                            if ( exceptionIdentifier == null ) {
                                throw new InvalidOperationException(
                                    "exception identifier expected in 'catch' block; found '" + exceptionMaskList[1] +
                                    "' instead" );
                            }

                            catcher = objs[3] as IEvaluable;
                            ensureArg( "try", objs[4], finallyId );
                            finaller = objs[5] as IEvaluable;

                            try {
                                return body.eval( this );
                            } catch ( Exception ex ) {
                                Type t = ex.GetType();
                                if ( !t.IsSubclassOf( exceptionMaskType ) && t != exceptionMaskType ) {
                                    throw ex;
                                }

                                setIdentifierValue( exceptionIdentifier, ex );
                                return catcher.eval( this );
                            } finally {
                                finaller.eval( this );
                            }
                    }

                    throw new InvalidOperationException(
                        $"mismatched amount of arguments (found {objs.Length}; expected 1, 3, 4, 5 or 6) in 'try' block" );
                }, HoldType.All )
            }, {
                "function", new MethodWrapper( ( objs ) => { // TODO lambda operator '=>'
                    return objs.Length switch {
                        1 => new Function(
                            (IEvaluable) objs[0] ), // it has to be IEvaluable since we have Hold on it (HoldType.All)
                        2 => createFunction( objs[0], objs[1] ),
                        _ => throw new InvalidOperationException(
                            $"wrong number of parameters for 'function' (expected 1 or 2, found {objs.Length}' instead)" )
                    };
                }, HoldType.All )
            },
            //// utility methods
            {
                "print", new MethodWrapper( ( objs ) => {
                    foreach ( object o in objs ) {
                        if ( o is ArgumentGroup ag && ag.getOperator() == null ) {
                            foreach ( IEvaluable ie in ag.getEvaluableList() ) {
                                Console.Write( MiscUtils.toString( ie.eval( this ) ) );
                            }
                        } else {
                            Console.Write( MiscUtils.toString( ( (IEvaluable) o ).eval( this ) ) );
                        }
                    }

                    return ""; // easy way to produce no additional visual output
                }, HoldType.All )
            }, {
                "println", new MethodWrapper( ( objs ) => {
                    foreach ( object o in objs ) {
                        if ( o is ArgumentGroup ag && ag.getOperator() == null ) {
                            foreach ( IEvaluable ie in ag.getEvaluableList() ) {
                                Console.WriteLine( MiscUtils.toString( ie.eval( this ) ) );
                            }
                        } else {
                            Console.WriteLine( MiscUtils.toString( ( (IEvaluable) o ).eval( this ) ) );
                        }
                    }

                    return ""; // easy way to produce no additional visual output
                }, HoldType.All )
            },
        };
        // note: by default, with 'func(arg0,arg1...)' syntax, obj[0] contains *all* arguments passed, wrapped as a ValueList
        // to pass the arguments *directly*, use 'func arg0 arg1'
        // - this is *strictly* required for composite (multi-block) methods (like 'for', 'while' etc) to work at all!
        setIdentifierValueConstant( ifId, new MethodWrapper( ( objs ) => {
            ensureArgCount( "if", 2, StackLimit, objs );
            int len = objs.Length;
            bool hasLoneElse = ( len % 4 == 0 );
            for ( int i = 0; i < len; i += 2 ) {
                if ( evalCondition( (IEvaluable) objs[i] ) ) {
                    return ( (IEvaluable) objs[i + 1] ).eval( this );
                }

                i += 2;
                if ( i >= len ) {
                    return null;
                }

                ensureArg( "if", objs[i], elseId );
                if ( hasLoneElse ) {
                    if ( i + 2 == len ) {
                        return ( (IEvaluable) objs[i + 1] ).eval( this );
                    }
                } else {
                    ensureArg( "if", objs[i + 1], ifId );
                }
            }

            return null;
        }, HoldType.All ) );
        setIdentifierValueConstant( whileId, new MethodWrapper( ( objs ) => {
            ensureArgCount( "while", 2, 2, objs );
            IEvaluable //
                condition = objs[0] as IEvaluable,
                body = objs[1] as IEvaluable;

            while ( evalCondition( condition ) ) {
                object ret = body.eval( this );
                if ( ret is IExecutionFlow ef ) {
                    return ef.getLoopValue();
                }
            }

            return null;
        }, HoldType.All ) );

        foreach ( KeyValuePair<string, object> entry in defaultVarsMap ) {
            setIdentifierValueConstant( entry.Key, entry.Value );
        }
    }

    private Func<dynamic, dynamic> lambda2( Func<dynamic, dynamic> func ) {
        return func; // pseudo-casting needed for dynamic dispatch
    }

    private Func<dynamic, dynamic, dynamic> lambda3( Func<dynamic, dynamic, dynamic> func ) {
        return func; // pseudo-casting needed for dynamic dispatch
    }

    private object wrappedLambda2( Identifier n, Func<dynamic, dynamic> func ) {
        dynamic dyn = getIdentifierValueRaw( n );
        dyn = func( dyn );
        _setIdentifierValue( n, dyn );
        return dyn;
    }

    private object wrappedLambda3( Identifier n, dynamic m, Func<dynamic, dynamic, dynamic> func ) {
        dynamic dyn = getIdentifierValueRaw( n );
        dyn = func( dyn, m );
        _setIdentifierValue( n, dyn );
        return dyn;
    }

    private Operator createAssignmentOperator( string opString, Func<dynamic, dynamic> unary,
                                               Func<dynamic, dynamic, dynamic> nary ) {
        return new(opString,
                   ( unary != null ) ? lambda2( ( n ) => wrappedLambda2( n, unary ) ) : null,
                   ( nary != null ) ? lambda3( ( n, m ) => wrappedLambda3( n, m, nary ) ) : null,
                   HoldType.First);
    }

    private string dotOpExceptionMessage( object n, object m ) {
        return "Identifier-Identifier, CompositeIdentifier-Identifier or object-string pair expected; found "
            + MiscUtils.toDebugString( n ) + " and " + MiscUtils.toDebugString( m ) + " instead";
    }

    private object execOnBlock( IExecutable iexecutable, object n, object m ) {
        ISyntaxGroup cb = m as ISyntaxGroup;
        if ( cb == null ) {
            throw new InvalidOperationException( dotOpExceptionMessage( n, m ) );
        }

        if ( cb.isEmpty() ) {
            return iexecutable.exec( this, null );
        }

        object cbEval = cb.eval( this );
        ValueList vl = cbEval as ValueList;
        return ( vl != null ) ? iexecutable.exec( this, vl.ToArray() ) : iexecutable.exec( this, cbEval );
    }

    private object objectMethodHandler( object n, object m, Identifier idM ) {
        IEvaluable ievaN = n as IEvaluable;
        if ( ievaN == null ) {
            ObjectMethod om = n as ObjectMethod;
            if ( om == null ) {
                throw new InvalidOperationException( dotOpExceptionMessage( n, m ) );
            }

            return execOnBlock( om, n, m );
        }

        object oN = ievaN.eval( this );
        Type t = oN as Type;
        if ( t == null ) {
            t = oN.GetType();
        }

        string s;
        object oM;
        if ( idM != null ) {
            s = idM.Name;
            oM = null;
        } else {
            IEvaluable ievaM = m as IEvaluable;
            if ( ievaM == null ) {
                throw new InvalidOperationException( dotOpExceptionMessage( n, m ) );
            }

            oM = ievaM.eval( this );
            s = oM as string;
        }

        MethodInfo mi;
        if ( s == null ) {
            mi = oM as MethodInfo;
            if ( mi == null ) {
                throw new InvalidOperationException( dotOpExceptionMessage( n, m ) );
            }
        } else {
            mi = t.GetMethod( s );
            if ( mi == null ) {
                throw new InvalidOperationException( "method '" + s + "' not found in type '" + t + "'" );
            }
        }

        return new ObjectMethod( oN, mi );
    }

    protected void createDefaultOperators() {
        Operator[] defaultOperators = {
            //// internal script engine operators
            new(".", null, // method operator
                ( n, m ) => {
                    CompositeIdentifier ci = n as CompositeIdentifier;
                    Identifier idM = m as Identifier;
                    if ( ci == null ) {
                        if ( idM == null ) {
                            return objectMethodHandler( n, m, idM );
                        } // else m is a valid Identifier

                        Identifier idN = n as Identifier;
                        if ( idN == null ) {
                            return objectMethodHandler( n, m, idM );
                        }

                        return new CompositeIdentifier() { idN, idM };
                    }

                    if ( idM == null ) {
                        return execOnBlock( ci, n, m );
                    }

                    ci.Add( idM );
                    return ci;
                }, HoldType.All),
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
            new("'", ( n ) => { // Identifier<->string operator; coincidentally doubles as "alternative string quotes"
                ValueWrapper vw = n as ValueWrapper;
                if ( vw != null ) {
                    n = vw.Value; // note: not recursive!
                }

                string s = n as string;
                if ( s != null ) {
                    return getIdentifier( s );
                }

                Identifier id = n as Identifier;
                if ( id != null ) {
                    return id.Name;
                }

                return n;
            }, null, HoldType.All),
            new("`", ( n ) => { // hold operator
                ValueWrapper vw = n as ValueWrapper;
                if ( vw != null ) {
                    n = vw.Value; // note: not recursive!
                }

                Identifier id = n as Identifier;
                return ( id != null ) ? /*getIdentifierValue( id )*/ id : Wrapper.wrap( n );
            }, null, HoldType.All),
            new("?",
                ( n ) => MiscUtils.getTypeFor( n ),
                //null
                ( n, m ) => {
                    // note: maybe it's not the best idea, but I leave it here for reference & future generations
                    IList list = n as IList;
                    if ( list == null ) {
                        list = new ValueList();
                        list.Add( MiscUtils.getTypeFor( n ) );
                    }

                    list.Add( MiscUtils.getTypeFor( m ) );
                    return list;
                }
            ), // 'typeof' operator
            new("=>",
                ( n ) => new Function( (IEvaluable) n )
                , // it has to be IEvaluable since we have Hold on it (HoldType.All)
                ( n, m ) => createFunction( n, m ),
                HoldType.All
            ),
            new("@", ( n ) => getIdentifierValue( (Identifier) n ), null, HoldType.All),
            //new Operator( "*&", () => callStack.Last.Previous.Previous.Value ),
            ////regular unary/nary operators
            new("+",
                ( n ) => +n,
                ( n, m ) => n + m),
            new("-",
                ( n ) => -n,
                ( n, m ) => n - m),
            new("!", ( n ) => !n, null),
            new("~", ( n ) => ~n, null),
            new("::", ( n ) => new ValueList( 1 ) { n },
                ( n, m ) => {
                    IList list = n as IList;
                    if ( list == null ) {
                        list = new ValueList( 1 ) { n };
                    }

                    list.Add( m );
                    return list;
                }),
            new("=!", ( n ) => {
                ValueWrapper vw = getIdentifierValue( n );
                dynamic val = vw.Value;
                val = !val;
                setIdentifierValue( n, val );
                return val;
            }, null, HoldType.First),
            new("=???", null, ( n ) => {
                dynamic val = MiscUtils.getRandomForType( getIdentifierValue( n ).Value.GetType() );
                setIdentifierValue( n, val );
                return val;
            }, null, HoldType.First, Associativity.LeftToRight),
            new("???",
                () => MiscUtils.RNG.NextDouble(),
                ( n ) => MiscUtils.RNG.Next( n ),
                ( n, m ) => MiscUtils.RNG.Next( n, m ),
                HoldType.None, Associativity.LeftToRight
            ),
            createAssignmentOperator( "++", ( n ) => ++n, null ),
            createAssignmentOperator( "--", ( n ) => --n, null ),
            new("*", null, ( n, m ) => n * m),
            new("/", null, ( n, m ) => n / m),
            new("%", null, ( n, m ) => n % m),
            new("<<", null, ( n, m ) => n << m),
            new(">>", null, ( n, m ) => n >> m),
            new("^", null, ( n, m ) => n ^ m),
            new("|", null, ( n, m ) => n | m),
            new("&", null, ( n, m ) => n & m),
            new("||", null, ( n, m ) => ( (IEvaluable) n ).eval( this ) || ( (IEvaluable) m ).eval( this ), HoldType.All
            ), // short-circuit op
            new("&&", null, ( n, m ) => ( (IEvaluable) n ).eval( this ) && ( (IEvaluable) m ).eval( this ), HoldType.All
            ), // short-circuit op
            new("==", null, ( n, m ) => n == m),
            new("!=", null, ( n, m ) => n != m),
            new("===", null, ( n, m ) =>
                    n.Equals( m )), // note: in JS, this would be more like above ==, and above == would be more or less this
            new("!==", null, ( n, m ) => !n.Equals( m )),
            new(">", null, ( n, m ) => n > m),
            new(">=", null, ( n, m ) => n >= m),
            new("<", null, ( n, m ) => n < m),
            new("<=", null, ( n, m ) => n <= m),
            new("??", null, ( n, m ) => n ?? m),
            // TODO ternary as "?:" ?
            new("=", null, ( n, m ) => {
                setIdentifierValue( m, n );
                return n;
            }, HoldType.AllButFirst, Associativity.RightToLeft),
            new(":=", null, ( n, m ) => {
                setIdentifierValue( m, n );
                return n;
            }, HoldType.All, Associativity.RightToLeft),
            new("+=", null, ( n, m ) => {
                // since we have HoldType.First here
                Identifier id = n as Identifier;
                if ( id != null ) {
                    return wrappedLambda3( id, m, lambda3(
                                               ( n2, m2 ) => n2 + m2
                                           ) );
                }

                ValueWrapper wrap = n as ValueWrapper;
                if ( wrap != null ) {
                    dynamic o = wrap.Value;
                    if ( o is Stack ) {
                        o.Push( m );
                        return n;
                    }

                    if ( o is Queue ) {
                        o.Enqueue( m );
                        return n;
                    }

                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if ( o is ICollection ) {
                        o.Add( m );
                        return n;
                    }
                }

                throw new InvalidOperationException( "found '" + n + "'" );
            }, HoldType.First),
            new("-=", null, ( n, m ) => {
                // since we have HoldType.First here
                Identifier id = n as Identifier;
                if ( id != null ) {
                    return wrappedLambda3( id, m, lambda3(
                                               ( n2, m2 ) => n2 - m2
                                           ) );
                }

                ValueWrapper wrap = n as ValueWrapper;
                if ( wrap != null ) {
                    dynamic o = wrap.Value;

                    if ( o is Stack ) {
                        o.Pop( m );
                        return n;
                    }

                    if ( o is Queue ) {
                        o.Dequeue( m );
                        return n;
                    }

                    //if ( o is LinkedList<> ) { o.RemoveLast(); return n; }
                    if ( o is ICollection ) {
                        o.Remove( m );
                        return n;
                    }
                }

                throw new InvalidOperationException( "found '" + n + "'" );
            }, HoldType.First),
            createAssignmentOperator( "*=", null, ( n, m ) => n * m ),
            createAssignmentOperator( "/=", null, ( n, m ) => n / m ),
            createAssignmentOperator( "%=", null, ( n, m ) => n % m ),
            createAssignmentOperator( "<<=", null, ( n, m ) => n << m ),
            createAssignmentOperator( ">>=", null, ( n, m ) => n >> m ),
            createAssignmentOperator( "^=", null, ( n, m ) => n ^ m ),
            createAssignmentOperator( "&=", null, ( n, m ) => n & m ),
            createAssignmentOperator( "|=", null, ( n, m ) => n | m ),
        };
        foreach ( Operator op in defaultOperators ) {
            addOperator( op );
        }

        Operator indexer = new("[]", null, ( n, m ) => n[m]);
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

    public void addOperator( Operator op ) {
        operatorMap[op.OperatorString] = op;
    }

    public void addOperator( Operator op, string alias ) {
        operatorMap[alias] = op;
    }

    public Operator operatorValueOf( string opString ) {
        Operator op = null;
        if ( !operatorMap.TryGetValue( opString, out op ) )
            throw new InvalidOperationException( "no matching operator in this engine for '" + opString + "'" );
        return op;
    }

    public Func<dynamic, dynamic> getUnaryOperator( string opString ) {
        return operatorValueOf( opString ).UnaryLambda;
    }

    public Func<dynamic, dynamic, dynamic> getNaryOperator( string opString ) {
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

    protected object wrapRetVal( object ret ) {
        ExecutionReturn er = ret as ExecutionReturn;
        if ( er != null ) {
            ret = er.getValue();
        }

        ValueWrapper vw = ret as ValueWrapper;
        retVal.Value = ( vw != null ) ? vw.Value : ret;
        return ret;
    }

    public object eval( string s ) {
        return eval( new StringLexer( s, this ) ); // retVal set inside
    }

    public object eval( Lexer lexer ) {
        return eval( lexer.createLinearSyntax() ); // retVal set inside
    }

    public object eval( LinearSyntax linearSyntax ) {
        return eval( linearSyntax.buildParseTree() ); // retVal set inside
    }

    public object eval( IEvaluable iEvaluable ) {
        callStack.Clear();
        object ret;
        try {
            ret = iEvaluable.eval( this );
        } catch ( ScriptExitException ex ) {
            ret = ex.ReturnValue;
        } catch ( Exception ex ) {
            ret = ex;
            if ( RethrowExceptions ) {
                throw ex;
            }
        }

        return wrapRetVal( ret );
    }

    public object exec( IExecutable iExecutable, params dynamic[] arguments ) {
        callStack.Clear();
        object ret;
        try {
            ret = iExecutable.exec( this, arguments );
        } catch ( ScriptExitException ex ) {
            ret = ex.ReturnValue;
        } catch ( Exception ex ) {
            ret = ex;
            if ( RethrowExceptions ) {
                throw ex;
            }
        }

        return wrapRetVal( ret );
    }

    /**
         * Note: raw values get wrapped automagically; ISyntaxElement-s ain't.
         */
    public dynamic debugApplyStringOperator( String opString, params dynamic[] arguments ) {
        int max = arguments.Length;
        ISyntaxElement[] ises = new ISyntaxElement[max + 1];
        ises[0] = operatorValueOf( opString );
        for ( int i = 0; i < max; i++ ) {
            dynamic arg = arguments[i];
            ISyntaxElement ise = arg as ISyntaxElement;
            ises[i + 1] = ( ise != null ) ? ise : Wrapper.wrap( arg );
        }

        //Console.WriteLine( string.Join<ISyntaxElement>( ",", ises ) ); // in case of debug
        return new SyntaxGroup( ises ).eval( this );
    }

    public void loop() {
        Console.Write( getPrompt() );
        for ( string line = Console.ReadLine(); line != null && line.Length != 0; line = Console.ReadLine() ) {
            try {
                Console.WriteLine( MiscUtils.toString( eval( line ) ) );
            } catch ( Exception ex ) {
                Console.WriteLine( Test.exceptionToString( ex ) );
            }

            Console.Write( getPrompt() );
        }
    }
}

}
