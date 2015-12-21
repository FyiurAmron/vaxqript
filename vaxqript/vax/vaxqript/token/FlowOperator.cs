using System;
using System.Text;

namespace vax.vaxqript {
    public class FlowOperator : ISyntaxElement {
        public Flow Flow { get; set; }

        public FlowOperator ( Flow flow ) {
            Flow = flow;
        }

        public static string ToString ( Flow flow ) {
            switch (flow) {
            case Flow.Down:
                return "{";
            case Flow.Up:
                return "}";
            case Flow.UpDown:
                return ";";
            }
            throw new InvalidOperationException( "uknown Flow '" + flow + "'" );
        }

        public override string ToString () {
            return ToString( Flow );
        }
    }


    
    // StringTokenizer
}

