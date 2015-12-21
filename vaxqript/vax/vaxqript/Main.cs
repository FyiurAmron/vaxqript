using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;

namespace vax.vaxqript {
    class MainClass {
        public static void test1 () {
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

            Console.WriteLine( "=======================" );

            CodeBlock cb1 = new CodeBlock(), cb2 = new CodeBlock();
            cb1.addAll( new Operator( "+" ), new ValueWrapper( 42 ), new ValueWrapper( 2 ) );
            cb2.addAll( new Operator( "-" ), cb1, new ValueWrapper( 46 ), new ValueWrapper( new AddTestClass() ) );
            Console.WriteLine( cb2.eval() );
            cb2.clear();
            cb2.addAll( new Operator( "-" ), cb1, new ValueWrapper( 46 ) );
            Console.WriteLine( cb2.eval() );
            cb2.clear();
            cb2.addAll( new Operator( ":-)" ), cb1, new ValueWrapper( new AddTestClass() ) );
            Console.WriteLine( cb2.eval() );
            cb2.clear();
            cb2.addAll( new Operator( ":-)" ), new ValueWrapper( new AddTestClass() ), cb1 );
            Console.WriteLine( cb2.eval() );
        }

        public static void test2 () {
            object[] arr = { "{", "+", 4, 2, "{", "*", "3", "3", "}", "}" };
        }

        public static void test3 () {
            var st = new StringLexer( " test; 42 { + ++ += //slowo xlowo3 aaa } .\nslowo xlowo3 aaa.} \"strin\" 123 3.14" );

            Console.WriteLine( st.createLinearSyntax().debugToString() );
        }

        public static void Main ( string[] args ) {
            //test1(); // completed
            //test2(); // TODO
            test3();
            Console.ReadKey();
        }
    }

    class AddTestClass : IScriptOperatorOverload {
        public static int operator + ( AddTestClass a1, AddTestClass a2 ) {
            return 42;
        }

        public static int operator + ( int a1, AddTestClass a2 ) {
            return 13;
        }

        public static int operator - ( int a1, AddTestClass a2 ) {
            return 13;
        }

        public ValueWrapper processLeft ( string opString, dynamic argument ) {
            if( opString.Equals( ":-)" ) ) {
                return new ValueWrapper( 9001 );
            }
            return null;
        }

        public ValueWrapper processRight ( string opString, dynamic argument ) {
            if( opString.Equals( ":-)" ) ) {
                return new ValueWrapper( 3.14 );
            }
            return null;
        }
    }
}
