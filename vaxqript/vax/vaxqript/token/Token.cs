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



    public abstract class Token {
        public string debugToString () {
            return "[" + GetType().Name + "] " + ToString();
        }
    }
}

