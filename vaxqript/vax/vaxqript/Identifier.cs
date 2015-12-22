using System;

namespace vax.vaxqript {
    public class Identifier : IEvaluable { // TODO implement IExecutable?
        public string Text;

        public Identifier ( string text ) {
            Text = text;
        }

        public  object eval ( Engine engine) {
            object o;
            if( engine.varMap.TryGetValue( this, out o ) ) {
                return o;
            }
            return this;
            //return  ? o : this;
        }

        public override bool Equals ( object obj ) {
            if( obj == null )
                return false;
            if( obj == this )
                return true;
            Identifier fooItem = obj as Identifier;

            return fooItem.Text.Equals( this.Text );
        }

        public override int GetHashCode () {
            return Text.GetHashCode();
        }

        public override string ToString () {
            return Text;
        }

        public static Identifier valueOf ( string name ) {
            return new Identifier( name ); // TODO implement instance cache (map) via Engine
        }
    }
}

