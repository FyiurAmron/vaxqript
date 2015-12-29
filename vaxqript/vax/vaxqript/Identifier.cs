using System;

namespace vax.vaxqript {
    public class Identifier : IEvaluable, IExecutable {
        public string Name;

        public Identifier ( string text ) {
            Name = text;
        }

        public object eval ( Engine engine ) {
            ValueWrapper o = engine.getIdentifierValue( this );
            if( o == null )
                return this;
            object val = o.Value;
            if( val == this )
                throw new StackOverflowException( "trying to evaluate an Identifier directly referencing itself" );
            IEvaluable ie = val as IEvaluable;
            return ( ie == null ) ? val : ie.eval( engine );
        }

        public object exec ( Engine engine, params dynamic[] arguments ) {
            ValueWrapper vw = engine.getIdentifierValue( this );
            if( vw == null )
                throw new InvalidOperationException( "identifier '" + Name + "' not defined yet" );

            Func<object[],object> func = vw.Value as Func<object[],object>;
            if( func == null ) {
                //throw new InvalidOperationException( "identifier '" + Name + "' doesn't refer to a valid Func<object[],object>" );
                return eval( engine );
            }
            return func( arguments );
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

