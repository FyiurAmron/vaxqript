﻿using System;

namespace vax.vaxqript {
    public class Identifier : IEvaluable {
        public string Text;

        public Identifier ( string text ) {
            Text = text;
        }

        public  object eval () {
            return Engine.varMap[this];
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

        public static Identifier forName ( string name ) {
            return new Identifier( name );
        }
    }
}

