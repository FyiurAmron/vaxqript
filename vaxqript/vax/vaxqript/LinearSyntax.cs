using System;
using System.Text;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class LinearSyntax {
        public List<ISyntaxElement> ElementList { get; private set; }

        public LinearSyntax () {
            ElementList = new List<ISyntaxElement>();
        }

        public void Add ( ISyntaxElement iSyntaxElement ) {
            ElementList.Add( iSyntaxElement );
        }

        public override string ToString () {
            return string.Join( " ", ElementList );
        }

        public string debugToString () {
            StringBuilder sb = new StringBuilder();
            foreach( ISyntaxElement ise in ElementList ) {
                sb.Append( '[' ).Append( ise.GetType().Name ).Append( "] " ).Append( ise.ToString() ).Append( ' ' );
            }
            return sb.ToString();
        }

        public CodeBlock buildParseTree() {
            CodeTreeBuilder ctb = new CodeTreeBuilder();
            foreach( var elem in ElementList ) {
                ctb.consume( elem );
            }
            return ctb.getRoot();
        }
    }


    
    // StringTokenizer
}

