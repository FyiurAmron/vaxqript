﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class Function : IExecutable {
        /*, IEvaluable*/
        private IEvaluable evaluable;
        private Identifier[] localIdentifierMapping;

        public Function ( IEvaluable syntaxGroup ) {
            this.evaluable = syntaxGroup;
        }

        public Function ( IEvaluable syntaxGroup, params Identifier[] identifiers ) {
            this.evaluable = syntaxGroup;
            this.localIdentifierMapping = identifiers;
        }

        public Function ( IEvaluable syntaxGroup, IList identifiers ) {
            this.evaluable = syntaxGroup;
            this.localIdentifierMapping = new Identifier[identifiers.Count];
            identifiers.CopyTo( localIdentifierMapping, 0 );
        }

        private dynamic _eval ( Engine engine ) {
            return evaluable.eval( engine );
        }

        private dynamic eval ( Engine engine ) {
            engine.pushCall( this );
            engine.pushFunction( this );
            object ret = _eval( engine );
            IExecutionFlow ief = ret as IExecutionFlow;
            if( ief != null ) {
                ret = ief.getValue();
            }
            engine.popFunction();
            engine.popCall();
            return ret;
        }

        private void ensureArgumentCount ( int required, int found ) {
            if( found < required ) {
                throw new InvalidOperationException( "function called with too few arguments (" + found + " found, " + required + " required)" );
            }
        }

        private void setFunctionArguments( Engine engine, int foundLength, IList argumentsList, params dynamic[] arguments ) {
            if( localIdentifierMapping != null ) {
                int ids = localIdentifierMapping.Length;
                ensureArgumentCount( ids, foundLength );
                for( int i = 0; i < ids; i++ ) {
                    engine.setIdentifierValue( localIdentifierMapping[i], argumentsList[i] );
                }
            }
            engine.setFunctionArguments( arguments );     
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            if( arguments == null || arguments.Length == 0 ) {
                if( localIdentifierMapping != null ) {
                    ensureArgumentCount( localIdentifierMapping.Length, 0 );
                }
                return eval( engine );
            }

            ValueList vl = arguments[0] as ValueList;
            if( vl != null ) {
                setFunctionArguments( engine, vl.Count, vl, arguments );
            } else { // we don't have our arguments wrapped - possibly a raw, unparenthesised expression
                setFunctionArguments( engine, arguments.Length, arguments, new object[]{ arguments } );
            }
            return eval( engine );
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None; // default behaviour - use ScriptMethod wrapper to change it
        }
    }
}

