﻿using System;

namespace vax.vaxqript {
    /*
    public class ExecutableWrapper : IWrapper {
        public ExecutableWrapper () {
        }

        public IExecutable Executable { get; set; }

        public object eval ( Engine engine ) {
            return Value;
        }

        public ValueWrapper ( object value ) {
            Value = value;
        }

        // TODO handle string instances specially (quote them!) OR add a StringWrapper : ValueWrapper extension maybe?

        private const char QUOTE_CHAR = '"';
        // TODO get based on engine?

        public string valueToString () {
            string s = Value as String;
            if( s != null ) {
                StringBuilder sb = new StringBuilder();
                sb.Append( QUOTE_CHAR );
                foreach( char c in s.ToCharArray() ) {
                    sb.Append( Lexer.escape( c ) );
                }
                sb.Append( QUOTE_CHAR );
                return sb.ToString();
            }
            IEnumerable ie = Value as IEnumerable;
            return ( ie == null ) ? Value.ToString() : MiscUtils.join( ",", ie );
        }

        public override string ToString () {
            return "/* " + Value.GetType().Name + " * / " + valueToString();
        }
    }
*/
}

