using System;
using System.Text;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CommentLine : IComment {
        public string Text { get; set; }

        public CommentLine ( string text ) {
            Text = text;
        }

        public override string ToString () {
            return Text;
        }
    }
}

