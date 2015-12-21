using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;

namespace vax.vaxqript {
    class MainClass {
        public static void Main ( string[] args ) {
            /*
            var st = new StringTokenizer( " test slowo xlowo3 aaa ." );

            for( Token t = st.getNextToken(); t != null; t = st.getNextToken() ) {
                Console.WriteLine( t );
            }
            */
            try {
                Console.WriteLine( Operator.applyGenericOperator( "+", "test", "owy", 42 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "+", new AddTestClass(), new AddTestClass(), " 16" ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "+", new AddTestClass(), new AddTestClass(), 16 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "+", 16, new AddTestClass(), new AddTestClass(), 16 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            Identifier fooI = Identifier.forName( "foo" );

            try {
                Console.WriteLine( Operator.applyGenericOperator( "=", fooI, 9001 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "/=", fooI, 1001 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Engine.varMap[fooI] );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            /*
            try {
                Console.WriteLine( string.Join( "\n", Operator.applyGenericOperator( "<", new List<object>(), true, 1 ) ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }*/

            try {
                Console.WriteLine( string.Join( "\n", Operator.applyGenericOperator( "+=", new List<object>(), true, 1 ) ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "||", Operator.applyGenericOperator( "&&", true, false ), false ) );
                Console.WriteLine( Operator.applyGenericOperator( "||", Operator.applyGenericOperator( "&&", true, true ), false ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( Operator.applyGenericOperator( "`", "foo" ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            Console.ReadKey();
        }
    }

    class AddTestClass {
        public static int operator + ( AddTestClass a1, AddTestClass a2 ) {
            return 42;
        }

        public static int operator + ( int a1, AddTestClass a2 ) {
            return 13;
        }

    }
}
