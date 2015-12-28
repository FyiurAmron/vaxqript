using System;

namespace vax.vaxqript {
    public class Wrapper {
        private Wrapper () {
            throw new InvalidOperationException();
        }

        public static ISyntaxElement wrap ( object o ) {
            IExecutable ie = o as IExecutable;
            if( ie != null )
                return ie;
            return new ValueWrapper( o ); // ternary doesn't work due to conflicting types
        }


        public static ISyntaxElement[] wrap ( object[] objects ) {
            int max = objects.Length;
            ISyntaxElement[] ret = new ISyntaxElement[max];
            for( int i = 0; i < max; i++ ) {
                ret[i] = wrap( objects[i] );
            }
            return ret;
        }
    }
}

