using System;

namespace vax.vaxqript {
    public class Identifier : IEvaluable {
        // TODO implement IExecutable?
        public string Name;

        public Identifier ( string text ) {
            Name = text;
        }

        public object eval ( Engine engine ) {
            ValueWrapper o = engine.getIdentifierValue( this );
            return ( o == null ) ? this : o.Value;
        }

        public override bool Equals ( object obj ) {
            if( obj == null )
                return false;
            if( obj == this )
                return true;
            Identifier fooItem = obj as Identifier;

            return fooItem.Name.Equals( this.Name );
        }

        public override int GetHashCode () {
            return Name.GetHashCode();
        }

        public override string ToString () {
            return Name;
        }

        /*
        public static Identifier valueOf ( string name ) {
            return new Identifier( name ); // TODO implement instance cache (map) via Engine
        }
        */
    }
}

