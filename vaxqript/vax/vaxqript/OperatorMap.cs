using System;
using System.Collections.Generic;
using System.Collections;

namespace vax.vaxqript {
    public class OperatorMap {

        public static readonly Dictionary<string,Func<dynamic, dynamic>> unaryOperatorDictionary = new Dictionary<string,Func<dynamic, dynamic>> {
            { "-", (n ) => { return -n; } },
            { "!", (n ) => { return !n; } },
            { "`", (n ) => { return Engine.varMap[Identifier.valueOf(n)]; } },
            { "++", (n ) => { return ++n; } },
            { "--", (n ) => { return --n; } },
        };
        public static readonly Dictionary<string,Func<dynamic, dynamic, dynamic>> naryOperatorDictionary = new Dictionary<string,Func<dynamic, dynamic, dynamic>> {
            { "+", (n, m ) => { return n + m; } },
            { "-", (n, m ) => { return n - m; } },
            { "*", (n, m ) => { return n * m; } },
            { "/", (n, m ) => { return n / m; } },
            { "%", (n, m ) => { return n % m; } },
            { "||",(n, m ) => { return n || m; } },
            { "&&",(n, m ) => { return n && m; } },
            { "=", (n, m ) => { Engine.varMap[n] = m; return m; } },
            { "+=", (n, m ) => {
                    if ( n is Stack ) { n.Push(m); return n; }
                    if ( n is Queue ) { n.Enqueue(m); return n; }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if ( n is ICollection ) { n.Add(m); return n; }
                    Engine.varMap[n] += m; return n; } },
            { "-=", (n, m ) => {
                    if ( n is Stack ) { n.Pop(m); return n; }
                    if ( n is Queue ) { n.Dequeue(m); return n; }
                    //else if ( n is LinkedList ) { n.AddLast(m); return n; }
                    if ( n is ICollection ) { n.Remove(m); return n; }        
                    Engine.varMap[n] -= m; return n; } },
            { "*=", (n, m ) => { Engine.varMap[n] *= m; return n; } },
            { "/=", (n, m ) => { Engine.varMap[n] /= m; return n; } },
            { "%=", (n, m ) => { Engine.varMap[n] %= m; return n; } },
            //{ "<", (n, m ) => { n.Add( m ); return n; } },
        };

        public static Func<dynamic, dynamic> getUnaryOperator( string opString ) {
            return unaryOperatorDictionary[opString];
        }

        public static Func<dynamic, dynamic, dynamic> getNaryOperator( string opString ) {
            Func<dynamic, dynamic, dynamic> ret;
            naryOperatorDictionary.TryGetValue(opString, out ret );
            return ret;
            //return naryOperatorDictionary[opString];
        }

        /*
        // flow operators
        OP_ParenthesisLeft( "(", -1, -1, type.OT_Prefix, true ),
        OP_ParenthesisRight( ")", -1, -1, type.OT_Postfix, true ),
        OP_BracketLeft( "[", -1, 1, type.OT_Prefix, true ),
        OP_BracketRight( "]", -1, 1, type.OT_Postfix, true ),
        OP_BraceLeft( "{", -1, -1, type.OT_Prefix, true ),
        OP_BraceRight( "}", -1, -1, type.OT_Postfix, true ),
        OP_Separator( ",", -1, 2, type.OT_Infix, true ),
        OP_Terminator( ";", -1, 1, type.OT_Postfix, true ),
        OP_Internal( "#", -1, 1, type.OT_Prefix, true ),
        // secondary flow operators
        OP_ForeachForward( ">>", -1, 2, type.OT_Infix ),
        OP_ForeachBackward( "<<", -1, 2, type.OT_Infix ), // last operator in enum
        OP_Ellipsis( "...", -1, 0, type.OT_Prefix ), // vararg def
        OP_New( "@", -1, 1, type.OT_Prefix ), // strange, but simple
        // regular operators
        OP_Select( ".", -2, 2, type.OT_Infix ), // single dot (selection operator) should create an identifier if followed by alpha char
        OP_Increment( "++", -1, 1, type.OT_PrefixPostfix ), // L-VAL
        OP_Decrement( "--", -1, 1, type.OT_PrefixPostfix ), // L-VAL
        OP_BoolNot( "!", 0, 1, type.OT_Prefix ),
        OP_Range( "..", 0, 2, type.OT_Infix ), // array range selector
        OP_InstanceOf( "??", 0, 2, type.OT_Infix ), // again, strange, but simple
        OP_Multiply( "*", 1, 2, type.OT_Infix ),
        OP_Divide( "/", 1, 2, type.OT_Infix ),
        OP_Remainder( "%", 1, 2, type.OT_Infix ),
        OP_Plus( "+", 2, 2, type.OT_Infix ),
        OP_Minus( "-", 2, 2, type.OT_Infix ),
        OP_MoreThan( ">", 3, 2, type.OT_Infix ),
        OP_LessThan( "<", 3, 2, type.OT_Infix ),
        OP_MoreOrEq( ">=", 3, 2, type.OT_Infix ),
        OP_LessOrEq( "<=", 3, 2, type.OT_Infix ),
        OP_IsEqual( "==", 4, 2, type.OT_Infix ),
        OP_IsNotEqual( "!=", 4, 2, type.OT_Infix ),
        OP_BoolAnd( "&&", 5, 2, type.OT_Infix ),
        OP_BoolAndLong( "&", 5, 2, type.OT_Infix ),
        OP_BoolOr( "||", 6, 2, type.OT_Infix ),
        OP_BoolOrLong( "|", 6, 2, type.OT_Infix ),
        OP_Equals( "=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisPlus( "+=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisMinus( "-=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisMultiply( "*=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisDivide( "/=", 7, 2, type.OT_Infix ), // L-VAL
        OP_ThisRemainder( "%=", 7, 2, type.OT_Infix ), // L-VAL
        //OP_TernaryStart( "?", 7, type.OT_Ternary ),
        //OP_TernaryEnd( ":", 7, type.OT_Ternary ), // shared with case selector in "case X:"; op_prec doesn't matter in that case
        ;
        */
    }
}

