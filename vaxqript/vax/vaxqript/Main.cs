using System;
using Microsoft.CSharp.RuntimeBinder;
using System.Collections.Generic;

namespace vax.vaxqript {
    class MainClass {
        public static void test1a ( Engine engine ) {
            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "+", "test", "owy", 42 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "+", new AddTestClass(), new AddTestClass(), " 16" ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "+", new AddTestClass(), new AddTestClass(), 16 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "+", 16, new AddTestClass(), new AddTestClass(), 16 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            Identifier fooI = Identifier.valueOf( "foo" );

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "=", fooI, 9001 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "/=", fooI, 1001 ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.varMap[fooI] );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            /*
            try {
                Console.WriteLine( string.Join( "\n", engine.debugApplyGenericOperator( "<", new List<object>(), true, 1 ) ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }*/

            try {
                Console.WriteLine( string.Join( "\n", engine.debugApplyGenericOperator( "+=", new List<object>(), true, 1 ) ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "||", engine.debugApplyGenericOperator( "&&", true, false ), false ) );
                Console.WriteLine( engine.debugApplyGenericOperator( "||", engine.debugApplyGenericOperator( "&&", true, true ), false ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            try {
                Console.WriteLine( engine.debugApplyGenericOperator( "`", "foo" ) );
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

        }

        public static void test1b ( Engine engine ) {
            CodeBlock cb1 = new CodeBlock(), cb2 = new CodeBlock();
            cb1.addAll( engine.operatorValueOf( "+" ), new ValueWrapper( 42 ), new ValueWrapper( 2 ) );
            cb2.addAll( engine.operatorValueOf( "-" ), cb1, new ValueWrapper( 46 ), new ValueWrapper( new AddTestClass() ) );
            Console.WriteLine( engine.eval( cb2 ) );
            cb2.clear();
            cb2.addAll( engine.operatorValueOf( "-" ), cb1, new ValueWrapper( 46 ) );
            Console.WriteLine( engine.eval( cb2 ) );
            cb2.clear();
            Operator smileOp = new Operator( ":-)", null, null );
            cb2.addAll( smileOp, cb1, new ValueWrapper( new AddTestClass() ) );
            Console.WriteLine( engine.eval( cb2 ) );
            cb2.clear();
            engine.addOperator( smileOp ); // add to cache
            cb2.addAll( engine.operatorValueOf( ":-)" ), new ValueWrapper( new AddTestClass() ), cb1 );
            Console.WriteLine( engine.eval( cb2 ) );
        }

        public static void test2 ( Engine engine ) {
            var sl = new StringLexer( " test; 42 { + ++ += //slowo xlowo3 aaa } .\nslowo xlowo3 aaa.} \"strin\" 123 3.14", engine );

            Console.WriteLine( sl.createLinearSyntax().debugToString() );
        }

        public static void test3 ( string input, Engine engine ) {
            var sl = new StringLexer( input, engine );

            LinearSyntax ls = sl.createLinearSyntax();

            //Console.WriteLine( ls.ToString() );
            //Console.WriteLine( ls.debugToString() );
            CodeBlock cn = ls.buildParseTree();
            //Console.WriteLine( cn );
            object o = engine.eval( cn );
            Console.WriteLine( ( o == null ) ? "null" : o );
        }

        public static void Main ( string[] args ) {
            Engine engine = new Engine();
            test1a( engine ); // completed
            Console.WriteLine( "=======================" );
            test1b( engine ); // completed
            Console.WriteLine( "=======================" );
            test2( engine ); // completed
            Console.WriteLine( "=======================" );
            string[] inputs = {
                "{ + 4 1 { * 3 11 } }",
                "{ + 4 2 { * 3 3",
                "{ 4 + 2 + ( 3 * 3 )",
                "4 + 2 + ( 3 * 3 )",
                "4 + 2 + ( 3.1 * 3 )",
                "foo",
                "{ 4 + 2 + ( 3.1 * 3 ); 10.5; foo * 2", // note: 'foo' is declared in previous tests!
                @"{
                    i = 3;
                    i++;
                }",
                @"{
                    i = 3;
                    i = 10;
                    i++;
                    i + 7;
                }"
            };
            foreach( string s in inputs ) {
                test3( s, engine ); // completed
            }
            

            for( string line = Console.ReadLine(); !line.StartsWith( "\n" ); line = Console.ReadLine() ) {
                try {
                    Console.WriteLine( ">>> " + engine.eval( line ) );
                } catch( Exception ex ) {
                    Console.WriteLine( ex );
                }
            }
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
