using System;
using System.Text;

namespace vax.vaxqript {
    public class FlowOperator : ISyntaxElement {
        public Flow Flow { get; set; }

        public FlowOperator ( Flow flow ) {
            Flow = flow;
        }

        public static FlowOperator valueOf( Flow flow ) { // TODO add cache here
            switch (flow) {
            case Flow.Down:
                return new FlowOperator( flow );
            case Flow.Up:
                return new FlowOperator( flow );
            case Flow.Separator:
                return new FlowOperator( flow );
            }
            throw new InvalidOperationException( "uknown Flow '" + flow + "'" );
        }

        public static string ToString ( Flow flow ) {
            switch (flow) {
            case Flow.Down:
                return "{";
            case Flow.Up:
                return "}";
            case Flow.Separator:
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

