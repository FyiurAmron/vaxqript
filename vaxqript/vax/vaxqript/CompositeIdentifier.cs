using System;
using System.Collections.Generic;
using System.Reflection;

namespace vax.vaxqript {
    public class CompositeIdentifier : LinkedList<Identifier>, IExecutable {
        public CompositeIdentifier () {
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None;
        }

        public void Add ( Identifier identifier ) {
            AddLast( identifier );
        }

        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.pushCallStack( this );
            object ret = _exec( engine, arguments );
            engine.popCallStack();
            return ret;
        }

        public dynamic _exec ( Engine engine, params dynamic[] arguments ) {
            if( Count == 0 ) {
                throw new InvalidOperationException( "0 Identifiers in this CompositeIdentifier; nothing to execute" );
            }

            Identifier id0 = First.Value; //id0 = this[0];

            if( Count == 1 ) {
                return id0.exec( engine, arguments );
            }

            Identifier idLast = Last.Value; //idLast = this[Count - 1];
            string methodName = idLast.Name; // always
            object ret;

            if( id0.Equals( engine.NewIdentifier ) ) { // c-tor invocation
                //RemoveAt( 0 );
                RemoveFirst();
                ret = MiscUtils.createNew( toType(), arguments );
                AddFirst( id0 );
                return ret;
            }

            RemoveLast();
            object o;
            Type t;
            if( Count == 1 ) { // 1 after the removal, 2 before
                object typeObj;
                if ( engine.tryGetIdentifierValue( id0, out o ) ) {
                    typeObj = o;
                } else { // not using ternary due to 'out' semantics
                    typeObj = id0.Name;
                }
                t = MiscUtils.getTypeFor( typeObj );
            } else {
                t = toType();
                o = null;
            }
            MethodInfo mi = t.GetMethod( methodName, MiscUtils.toTypes( arguments ) );
            ret = mi.Invoke( o, arguments );
            AddLast( idLast );
            return ret;
        }

        public Type toType () {
            return Type.GetType( string.Join( ".", this ) );
        }

        public override string ToString () {
            return "[CompositeIdentifier] " + MiscUtils.join( ".", this );
        }
    }
}

