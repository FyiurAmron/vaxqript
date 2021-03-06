﻿using System;

namespace vax.vaxqript {
    public class SyntaxGroup : AbstractSyntaxGroup {
        public SyntaxGroup () : base() {
        }

        public SyntaxGroup ( params ISyntaxElement[] syntaxElements ) : base( syntaxElements ) {
        }

        override public dynamic _eval ( Engine engine ) {
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
                    if( count == 1 ) {
                        IEvaluable iEva = vw.Value as IEvaluable;
                        if( iEva != null ) {
                            return iEva.eval( engine );
                        }
                    } //else {
                        idExec = vw.Value as IExecutable;
                        if( idExec != null ) {
                            return idExec.exec( engine, prepareArguments( engine, 1 ) );
                        }
                    //}
                }
            }

            object o = arguments[0].eval( engine );
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
    }
}

