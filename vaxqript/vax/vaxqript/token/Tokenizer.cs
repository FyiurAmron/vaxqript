using System;

namespace vax.vaxqript {
    public abstract class Tokenizer {
        protected const int MAX_ASCII_CHAR = 256;

        protected const char
            QUOTE_CHAR = '"',
            ESCAPE_CHAR = '\\',
            PARSER_OP_CHAR = '#',
            COMMENT_CHAR = '/',
            COMMENT_MULTILINE_CHAR = '*',
            GROUP_OPEN_CHAR = '{',
            GROUP_CLOSED_CHAR = '}'
        ;

        protected readonly static char[] //
            known_newline = { '\n', '\r' },
            known_whitespace = { ' ', '\t', '\n', '\r', '\f', (char) 0x0B },
            known_alpha_char = {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
        },
            known_numeric_char = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' },
            known_numeric_char_ext = { '.', 'E', 'd', 'f' },
            known_identifier_char_ext = { '_', '$', '.' };
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
            is_newline = new bool[MAX_ASCII_CHAR];

        protected bool keepComments;

        private void init ( char[] charTable, bool[] boolTable ) {
            for( int i = charTable.Length - 1; i >= 0; i-- )
                boolTable[charTable[i]] = true;
        }

        public Tokenizer () {
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
                init( known_newline, is_newline );
                init( known_whitespace, is_whitespace );
                init( known_alpha_char, is_identifier );
                for( max = known_numeric_char.Length, i = 0; i < max; i++ ) {
                    c = known_numeric_char[i];
                    numeric_value_arr[c] = known_numeric_values[i];
                    is_identifier[c] = true;
                    is_numeric[c] = true;
                    is_numeric_ext[c] = true;
                }
                init( known_numeric_char_ext, is_numeric_ext );
                init( known_identifier_char_ext, is_identifier );
                /*
                foreach( char c2 in operator_char_list )
                    is_operator[c2] = true;
                    */
            } catch (IndexOutOfRangeException ex) { // shouldn't happen, just in case
                throw new NotSupportedException( "trying to run with invalid default codepage (index of one of base char > "
                + MAX_ASCII_CHAR + ") caused " + ex );
            }  
        }
    }

    public class StringTokenizer : Tokenizer {
        string inputString;
        int pos, endPos, maxPos;

        public StringTokenizer ( string inputString ) {
            this.inputString = inputString;
            pos = 0;
            maxPos = inputString.Length;
        }

        protected bool skipWhitespace () {
            for(; pos != maxPos; pos++ ) {
                if( !is_whitespace[inputString[pos]] )
                    return true;
            }
            pos--;
            return false;
        }


        protected bool findEndingWhitespace () {
            for(; endPos != maxPos; endPos++ ) {
                if( is_whitespace[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected bool findEndingNewline () {
            for(; endPos != maxPos; endPos++ ) {
                if( is_newline[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected bool findEndingQuote () {
            for(; endPos != maxPos; endPos++ ) {
                if( is_newline[inputString[endPos]] )
                    return true;
            }
            endPos--;
            return false;
        }

        protected void processParserOp ( string s ) {
        }

        public Token getNextToken () {
            int beginPos;
            while( pos < maxPos ) {
                if( !skipWhitespace() )
                    return null;
                char firstChar = inputString[pos];
                if( pos < maxPos - 1 ) {
                    switch (firstChar) {
                    case COMMENT_CHAR:
                        switch (inputString[pos + 1]) {
                        case COMMENT_CHAR:
                            findEndingNewline();
                            beginPos = pos;
                            pos = endPos + 1;
                            /*if( keepComments )
                                return new CommentToken( inputString.Substring( beginPos, endPos - beginPos + 1 ) );
                            else
                            */
                                continue;
                        case COMMENT_MULTILINE_CHAR:
                            int multilineCommentNesting = 1;
                            //while ( endPos )
                            // process nested comments, then
                            /*
                            beginPos = pos;
                            pos = endPos + 1;
                            if( keepComments )
                                return new CommentToken( inputString.Substring( beginPos, endPos - beginPos + 1 ) );
                            else
                                */
                            continue;
                        }
                        break;
                    case QUOTE_CHAR:
                        if( !findEndingQuote() ) {
                            throw new InvalidOperationException( "stray opening quote found near EOF" );
                        }
                        beginPos = pos;
                        pos = endPos + 1;
                        return new StringLiteralToken( inputString.Substring( beginPos, endPos - beginPos + 1 ) );
                        break;
                    case PARSER_OP_CHAR:
                        findEndingNewline();
                        processParserOp( inputString.Substring( pos, endPos - pos + 1 ) );
                        pos = endPos + 1;
                        continue;
                    }
                } else {
                    switch (firstChar) {
                    case COMMENT_CHAR:
                    case QUOTE_CHAR:
                    case PARSER_OP_CHAR:
                        throw new InvalidOperationException( "stray '" + firstChar + "' found near EOF" );
                    }
                
                }
                endPos = pos + 1;

                beginPos = pos;
                pos = endPos;

                return Token.createToken( inputString.Substring( beginPos, endPos - beginPos + 1 /*endPos - 1*/ ) );
            }
            return null;
            // NOTE: substring(int,int) takes (begin,end) in Java, but (begin,length) in c# !
        }
        // StringTokenizer(inputString)
    }
    // StringTokenizer
}

