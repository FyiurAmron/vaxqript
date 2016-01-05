using System;
using System.Text;

namespace vax.vaxqript {
    public class SyntaxFlowOperator : ISyntaxElement {
        public SyntaxFlow Flow { get; set; }

        public SyntaxFlowOperator ( SyntaxFlow flow ) {
            Flow = flow;
        }

        public static SyntaxFlowOperator valueOf( SyntaxFlow flow ) { // TODO add cache here
            switch (flow) {
            case SyntaxFlow.Down:
                return new SyntaxFlowOperator( flow );
            case SyntaxFlow.Up:
                return new SyntaxFlowOperator( flow );
            case SyntaxFlow.Separator:
                return new SyntaxFlowOperator( flow );
            }
            throw new InvalidOperationException( "uknown Flow '" + flow + "'" );
        }

        public static string ToString ( SyntaxFlow flow ) {
            switch (flow) {
            case SyntaxFlow.Down:
                return "{";
            case SyntaxFlow.Up:
                return "}";
            case SyntaxFlow.Separator:
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

