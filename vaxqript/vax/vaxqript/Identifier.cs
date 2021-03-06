﻿using System;

namespace vax.vaxqript {
    public class Identifier : IEvaluable, IExecutable {
        public string Name;

        public Identifier ( string text ) {
            Name = text;
        }

        public HoldType getHoldType ( Engine engine ) {
            return ( (IExecutable) engine.getIdentifierValue( this ).Value ).getHoldType( engine );
        }
        // note: shouldn't be used in normal situations

        public dynamic eval ( Engine engine ) {
            ValueWrapper o = engine.getIdentifierValue( this );
            if( o == null ) {
                return this;
            }
            object val = o.Value;
            if( val == this )
                throw new StackOverflowException( "trying to evaluate an Identifier directly referencing itself" );
            IEvaluable ie = val as IEvaluable;
            engine.pushCall( this );
            object ret = ( ie == null ) ? val : ie.eval( engine );
            engine.popCall();
            return ret;
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            ValueWrapper vw = engine.getIdentifierValue( this );
            if( vw == null )
                throw new InvalidOperationException( "identifier '" + Name + "' not defined yet" );

            MethodWrapper methodWrapper = vw.Value as MethodWrapper;
            engine.pushCall( this );
            object ret = ( methodWrapper == null )
                ? eval( engine )
                : methodWrapper.invokeWith( arguments );
            engine.popCall();
            return ret;
        }

        public override bool Equals ( object obj ) {
            if( obj == null )
                return false;
            if( obj == this )
                return true;
            Identifier fooItem = obj as Identifier;

            return ( fooItem == null ) ? false : fooItem.Name.Equals( this.Name );
        }

        public override int GetHashCode () {
            return Name.GetHashCode();
        }

        public override string ToString () {
            return Name;
        }
    }
}

