using System;

namespace vax.vaxqript {
    public enum TokenType {
        StringLiteral,
        NumberLiteral,
        /*BooleanLiteral,*/
        // literals
        Identifier,
        Operator,
        ParserOp
    }

    public class UnknownToken : Token {
        string s;

        public UnknownToken ( string s ) {
            this.s = s;
        }

        public override string tokenToString () {
            return s;
        }
    }

    public class StringLiteralToken : Token {
        string s;

        public StringLiteralToken ( string s ) {
            this.s = s;
        }

        public override string tokenToString () {
            return s;
        }
    }

    public abstract class Token : IEvaluable {
        public Token () {
        }

        public static Token createToken ( string inputString ) {
            /*
            char classifier = inputString[0];
            if ( classifier >= '0' && classifier <= '9' ) {
                return new NumberToken( inputString );
            }
            switch (classifier) {
            case QUOTE_CHAR:
            case PARSER_OP_CHAR:
                
            }
            if ( inputString[0] == QUOTE_CHAR ){
                return new StringToken( inputString );
            }
            if ( in
            */
            return new UnknownToken( inputString );
        }

        public override string ToString () {
            return GetType().Name + " '" + tokenToString() + "'";
        }
         
        public  object eval() {
            return null;
        }

        public abstract string tokenToString ();
    }
}

