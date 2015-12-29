using System;
using System.Text;
using System.Collections.Generic;

namespace vax.vaxqript {
    public abstract class Lexer {
        protected const int MAX_ASCII_CHAR = 256;

        protected const char
            QUOTE_CHAR = '"',
            ESCAPE_CHAR = '\\',
            PARSER_OP_CHAR = '#',
            COMMENT_CHAR = '/',
            COMMENT_BLOCK_CHAR = '*',

            BLOCK_OPEN_CHAR_1 = '{',
            BLOCK_OPEN_CHAR_2 = '(',
            BLOCK_CLOSED_CHAR_1 = '}',
            BLOCK_CLOSED_CHAR_2 = ')',
            BLOCK_INLINE_CHAR = ';',
            SEPARATOR_CHAR = ',';

        protected readonly static char[] //
            known_newline = { '\n', '\r' },
            known_whitespace = { ' ', '\t', '\n', '\r', '\f', (char) 0x0B },
            known_alpha_char = {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
        },
            known_numeric_char = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' },
            known_numeric_char_ext = { '.', 'e', 'E',/* 'd', 'f'*/ },
            known_identifier_char_ext = { '_', '$'/*, '.'*/ };
        //known_flow_control_char = { '@', ';', ',', '#', '(', ')', '[', ']', '{', '}' };
        protected readonly static int[] // note: this allows for easy introduction of higher radices (e.g. hex digits)
            known_numeric_values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            numeric_value_arr = new int[MAX_ASCII_CHAR];
        protected readonly static bool[] //
            is_whitespace = new bool[MAX_ASCII_CHAR],
            is_identifier = new bool[MAX_ASCII_CHAR],
            is_operator = new bool[MAX_ASCII_CHAR],
            is_numeric = new bool[MAX_ASCII_CHAR],
            is_numeric_ext = new bool[MAX_ASCII_CHAR],
            is_newline = new bool[MAX_ASCII_CHAR],
            is_special = new bool[MAX_ASCII_CHAR];

        protected bool keepComments = true;

        private void init ( bool[] boolTable, params char[] charTable ) {
            for( int i = charTable.Length - 1; i >= 0; i-- )
                boolTable[charTable[i]] = true;
        }

        public Lexer () {
            //SC_operator[] values = SC_operator.values();
            //SC_HashSet<Character> operator_char_list = new SC_HashSet.linked<>( values.length );
            char[] op_char_arr;
            int i, max;
            char c;
            /*
            foreach( SC_operator op in values ) {
                op_char_arr = op.op_str.toCharArray();
                for( i = op_char_arr.length - 1; i >= 0; i-- )
                    operator_char_list.add( op_char_arr[i] );
                if ( op.is_flow )
                    flow_op_map[op.op_str.charAt( 0 )] = SC_token.get_factory_token( op );
            }
            */
            try {
                init( is_newline, known_newline );
                init( is_whitespace, known_whitespace );
                init( is_identifier, known_alpha_char );
                for( max = known_numeric_char.Length, i = 0; i < max; i++ ) {
                    c = known_numeric_char[i];
                    numeric_value_arr[c] = known_numeric_values[i];
                    is_identifier[c] = true;
                    is_numeric[c] = true;
                    is_numeric_ext[c] = true;
                }
                init( is_numeric_ext, known_numeric_char_ext );
                init( is_identifier, known_identifier_char_ext );
                init( is_special,
                    QUOTE_CHAR, PARSER_OP_CHAR,
                    BLOCK_OPEN_CHAR_1, BLOCK_OPEN_CHAR_2,
                    BLOCK_CLOSED_CHAR_1, BLOCK_CLOSED_CHAR_2,
                    BLOCK_INLINE_CHAR, SEPARATOR_CHAR );
                /*
                foreach( char c2 in operator_char_list )
                    is_operator[c2] = true;
                    */
            } catch (IndexOutOfRangeException ex) { // shouldn't happen, just in case
                throw new NotSupportedException( "trying to run with invalid default codepage (index of one of base char > "
                + MAX_ASCII_CHAR + ") caused " + ex );
            }  
        }

        public abstract LinearSyntax createLinearSyntax ();

        public static string escape ( char c ) {
            switch (c) {
            case '\b': // backspace
                return "\\b";
            case '\f': // form feed
                return "\\f";
            // common escape sequences
            case '\t': // tab
                return "\\t";
            case '\n': // newline
                return "\\n";
            case '\r': // carriage return
                return "\\r";
            case '\\': // backslash (escape char itself)
                return "\\\\";
            case '\'': // single quote
                return "\\'";
            case '\"': // double quote
                return "\\\"";
            case '\0': // NULL value (0x0)
                return "\\0";
            }
            return "" + c;
            //throw new InvalidOperationException( "char not an escape-able one: '" + c + "'" );
        }

        public static char unescape ( char c ) {
            // cross-compatible with Java's escape sequences
            switch (c) {
            /* those two are seldom used and Java incompatible
            case 'a': // alert/alarm (bell)
                return '\a';
            case 'v': // vertical tab
                return '\v';
            */
            // seldom used, but compatible
            case 'b': // backspace
                return '\b';
            case 'f': // form feed
                return '\f';
            // common escape sequences
            case 't': // tab
                return '\t';
            case 'n': // newline
                return '\n';
            case 'r': // carriage return
                return '\r';
            case '\\': // backslash (escape char itself)
                return '\\';
            case '\'': // single quote
                return '\'';
            case '"': // double quote
                return '"';
            case '0': // NULL value (0x0)
                return '\0';
            // TODO support full hex escape set maybe?
            }
            throw new InvalidOperationException( "uknown escape sequence '\\" + c + "'" );
        }

    }

    public class StringLexer : Lexer {
        protected string inputString;
        protected Engine engine;
        protected int pos, endPos, maxPos;

        public StringLexer ( string inputString, Engine engine ) {
            this.inputString = inputString;
            this.engine = engine;
            _reset();
        }

        private void _reset () {
            pos = 0;
            maxPos = inputString.Length;
        }

        public void reset () {
            _reset();
        }

        protected bool skipWhitespace () {
            for( ; pos != maxPos; pos++ ) {
                if( !is_whitespace[inputString[pos]] )
                    return true;
            }
            pos--;
            return false;
        }


        protected bool findEndingWhitespace () {
            for( ; endPos != maxPos; endPos++ ) {
                if( is_whitespace[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected bool findEndingNewline () {
            for( ; endPos != maxPos; endPos++ ) {
                if( is_newline[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected bool findIdentifierEnd () {
            for( ; endPos != maxPos; endPos++ ) {
                if( !is_identifier[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        /*
        protected bool findNumberEnd () {
            for( ; endPos != maxPos; endPos++ ) {
                if( !is_numeric_ext[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }
        */

        protected bool findOperatorEnd () {
            for( ; endPos != maxPos; endPos++ ) {
                char input = inputString[endPos];
                if( is_whitespace[input] || is_identifier[input] || is_numeric[input]
                    || is_special[input] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected void processParserOp ( string s ) {
            string[] args = s.Split( ' ' );
            string comm = args[0].ToLower();
            if( comm.Equals( "#exception" ) ) {
                if( args.Length == 1 )
                    throw new Exception( "#exception" );
                throw (Exception) Activator.CreateInstance( Type.GetType( args[1] ) );
            }
            if( comm.Equals( "#breakpoint" ) ) {
                Console.WriteLine( "#breakpoint" ); // place IDE breakpoint here
                return;
            }
            if( comm.Equals( "#write" ) ) {
                Console.WriteLine( s.Split( new char[]{ ' ' }, 2 )[1] );
                return;
            }
        }

        public override LinearSyntax createLinearSyntax () {
            var list = new LinearSyntax();
            for( var t = getNextSyntaxElement(); t != null; t = getNextSyntaxElement() ) {
                list.add( t );
            }
            reset();
            return list;
        }

        public ISyntaxElement getNextSyntaxElement () {
            int beginPos;
            while( pos < maxPos ) { 
                if( !skipWhitespace() )
                    return null;
                char firstChar = inputString[pos];
                // "special" cases first
                endPos = pos + 1;
                if( pos < maxPos - 1 ) {
                    switch (firstChar) {
                    case COMMENT_CHAR:
                        switch (inputString[pos + 1]) {
                        case COMMENT_CHAR:
                            findEndingNewline();
                            beginPos = pos;
                            pos = endPos + 1;
                            if( keepComments )
                                return new CommentLine( inputString.Substring( beginPos, endPos - beginPos + 1 ) );
                            else
                                continue;
                        case COMMENT_BLOCK_CHAR:
                            int blockCommentNesting = 1;
                            while( blockCommentNesting > 0 ) {
                                endPos++;
                                if( endPos == maxPos )
                                    throw new InvalidOperationException( "unterminated block comment" );
                                switch (inputString[endPos]) {
                                case COMMENT_CHAR: // possible block begin
                                    endPos++;
                                    if( endPos == maxPos )
                                        throw new InvalidOperationException( "unterminated block comment" );
                                    if( inputString[endPos] == COMMENT_BLOCK_CHAR ) {
                                        blockCommentNesting++;
                                    }
                                    break;
                                case COMMENT_BLOCK_CHAR: // possible block end
                                    endPos++;
                                    if( endPos == maxPos )
                                        throw new InvalidOperationException( "unterminated block comment" );
                                    if( inputString[endPos] == COMMENT_CHAR ) {
                                        blockCommentNesting--;
                                    }
                                    break;
                                }
                            }
                            beginPos = pos;
                            pos = endPos + 1;
                            if( keepComments )
                                return new CommentBlock( inputString.Substring( beginPos, endPos - beginPos + 1 ) );
                            else
                                continue;
                        }
                        break;
                    case QUOTE_CHAR:
                        StringBuilder sb = new StringBuilder();
                        pos++;
                        endPos++;
                        for( ; endPos != maxPos; endPos++ ) {
                            if( inputString[endPos] == QUOTE_CHAR ) {
                                beginPos = pos;
                                pos = endPos + 1;
                                return new ValueWrapper( inputString.Substring( beginPos, endPos - beginPos ) );
                            } else if( inputString[endPos] == ESCAPE_CHAR ) {
                                endPos++;
                                if( endPos == maxPos )
                                    throw new InvalidOperationException( "stray escape char found near EOF" );
                                sb.Append( unescape( inputString[endPos] ) );
                            } else {
                                sb.Append( inputString[endPos] );
                            }
                        }
                        throw new InvalidOperationException( "stray opening quote found near EOF" );
                    case PARSER_OP_CHAR:
                        findEndingNewline();
                        processParserOp( inputString.Substring( pos, endPos - pos + 1 ) );
                        pos = endPos + 1;
                        continue;
                    case BLOCK_OPEN_CHAR_1:
                    case BLOCK_OPEN_CHAR_2:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.Down );
                    case BLOCK_CLOSED_CHAR_1:
                    case BLOCK_CLOSED_CHAR_2:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.Up );
                    case BLOCK_INLINE_CHAR:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.UpDown );
                    case SEPARATOR_CHAR:
                        pos++;
                        endPos++;
                        continue;
                    }
                } else {
                    switch (firstChar) {
                    case BLOCK_OPEN_CHAR_1:
                    case BLOCK_OPEN_CHAR_2:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.Down );
                    case BLOCK_CLOSED_CHAR_1:
                    case BLOCK_CLOSED_CHAR_2:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.Up );
                    case BLOCK_INLINE_CHAR:
                        pos++;
                        endPos++;
                        return FlowOperator.valueOf( Flow.UpDown );
                    case COMMENT_CHAR:
                    case QUOTE_CHAR:
                    case PARSER_OP_CHAR:
                    case SEPARATOR_CHAR:
                        throw new InvalidOperationException( "stray '" + firstChar + "' found near EOF" );
                    }
                }
                // "regular" cases
                if( is_numeric[firstChar] ) {
                    bool hasExp = false, hasDot = false;

                    for( ; endPos != maxPos; endPos++ ) {
                        char c = inputString[endPos]; // TODO distinguish the special case of -/+ prefixing a number here
                        if( is_numeric[c] ) {
                            continue;
                        }
                        if( c == '.' ) {
                            if( hasDot || hasExp ) {
                                break;
                            }
                            hasDot = true;
                        } else if( c == 'e' || c == 'E' ) {
                            if( hasExp ) {
                                break;
                            }
                            hasExp = true;
                        } else {
                            break;
                        }
                    }
                    beginPos = pos;
                    pos = endPos;

                    string input = inputString.Substring( beginPos, endPos - beginPos );

                    int i;
                    float f;
                    if( !hasExp && !hasDot && Int32.TryParse( input, out i ) ) { // todo support more numeric types later on
                        return new ValueWrapper( i );    
                    } else if( Single.TryParse( input, out f ) ) {
                        return new ValueWrapper( f );
                    } else
                        return new UnknownElement( input );
                    
                } else if( is_identifier[firstChar] ) {
                    if( !findIdentifierEnd() ) {
                        beginPos = pos;
                        endPos++;
                        pos = endPos;
                    } else {
                        beginPos = pos;
                        pos = endPos;
                    }

                    return new Identifier( inputString.Substring( beginPos, endPos - beginPos ) );
                } else { // TODO distinguish the special case of -/+ prefixing a number here
                    if( !findOperatorEnd() ) {
                        beginPos = pos;
                        endPos++;
                        pos = endPos;
                    } else {
                        beginPos = pos;
                        pos = endPos;
                    }

                    return engine.operatorValueOf( inputString.Substring( beginPos, endPos - beginPos ) );
                }
            }
            return null;
            // NOTE: substring(int,int) takes (begin,end) in Java, but (begin,length) in c# ! endPos - beginPos -> endPos - 1
        }
        // StringTokenizer(inputString)
    }
    // StringTokenizer
}

