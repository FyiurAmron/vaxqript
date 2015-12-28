using System;
using System.Text;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CommentBlock : IComment {
        public string Text { get; set; }

        public CommentBlock ( string text ) {
            Text = text;
        }

        public override string ToString () {
            return Text;
        }
    }


    
    // StringTokenizer
}

