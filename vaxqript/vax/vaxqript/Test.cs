using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public static class Test {
        public static string exceptionToString ( Exception ex ) {
            return ">>> EXCEPTION: " + ex.GetType() + ":\n>>> " + ex.Message;
        }

        public static string testMethod() {
            return "test complete!";
        }

        public static object operatorTest ( Engine engine, String opString, params dynamic[] arguments ) {
            try {
                object ret = engine.debugApplyStringOperator( opString, arguments );
                Console.WriteLine( MiscUtils.toString( ret ) );
                return ret;
            } catch (Exception ex) {
                Console.WriteLine( exceptionToString( ex ) );
                return null;
            }
        }

        public static object operatorTest ( Engine engine, Func<Engine,object> action ) {
            try {
                object ret = action( engine );
                Console.WriteLine( MiscUtils.toString( ret ) );
                return ret;
            } catch (Exception ex) {
                Console.WriteLine( exceptionToString(ex) );
                return null;
            }
        }

        public static void setupTestArrays ( Engine engine ) {
            engine.setIdentifierValue( "testObj1", new AddTestClass() );
            engine.setIdentifierValue( "testArr1", new int[]{ 2, 3, 5, 7, 11 } );
            engine.setIdentifierValue( "testArr2", new int[][] {
                new int[]{ 2, 3, 5, 7, 11 },
                new int[]{ 1, 2, 3, 4, 5 },
                new int[] {
                    2,
                    4,
                    6,
                    8,
                    10
                }
            } );
            engine.setIdentifierValue( "testArr3", new int[,] {
                { 2, 3, 5, 7, 11 },
                { 1, 2, 3, 4, 5 }, {
                    2,
                    4,
                    6,
                    8,
                    10
                }, {
                    1,
                    1,
                    2,
                    3,
                    5
                }
            } );
        }

        public static void test1a ( Engine engine ) {
            operatorTest( engine, "+", "test", "owy", 42 );
            operatorTest( engine, "+", new AddTestClass(), new AddTestClass(), " 16" );
            operatorTest( engine, "+", new AddTestClass(), new AddTestClass(), 16 );
            operatorTest( engine, "+", 16, new AddTestClass(), new AddTestClass(), 16 );

            Identifier fooI = engine.setIdentifierValue( "foo", null );

            operatorTest( engine, "=", fooI, 9001 );
            operatorTest( engine, "/=", fooI, 1001 );

            operatorTest( engine, (eng ) => {
                return eng.getIdentifierValue( "foo" ).Value;
            } );

            operatorTest( engine, (eng ) => {
                return string.Join( "\n", eng.debugApplyStringOperator( "<", new List<object>(), true, 1 ) );
            } );

            operatorTest( engine, (eng ) => {
                return string.Join( "\n", eng.debugApplyStringOperator( "+=", new List<object>(), true, 1 ) );
            } );

            operatorTest( engine, (eng ) => {
                return "" + engine.debugApplyStringOperator( "||", engine.debugApplyStringOperator( "&&", true, false ), false )
                    + '\n' + engine.debugApplyStringOperator( "||", engine.debugApplyStringOperator( "&&", true, true ), false );
            } );

            operatorTest( engine, "'", fooI );
        }

        public static void test1b ( Engine engine ) {
            CodeBlock cb1 = new CodeBlock(), cb2 = new CodeBlock();
            cb1.addAll( engine.operatorValueOf( "+" ), new ValueWrapper( 42 ), new ValueWrapper( 2 ) );
            cb2.addAll( engine.operatorValueOf( "-" ), cb1, 46, new AddTestClass() );
            Console.WriteLine( MiscUtils.toString( engine.eval( cb2 ) ) );
            cb2.clear();
            cb2.addAll( engine.operatorValueOf( "-" ), cb1, 46 );
            Console.WriteLine( MiscUtils.toString( engine.eval( cb2 ) ) );
            cb2.clear();
            Operator smileOp = new Operator( ":-)", null, null );
            cb2.addAll( smileOp, cb1, new AddTestClass() );
            Console.WriteLine( MiscUtils.toString( engine.eval( cb2 ) ) );
            cb2.clear();
            engine.addOperator( smileOp ); // add to cache
            cb2.addAll( engine.operatorValueOf( ":-)" ), new AddTestClass(), cb1 );
            Console.WriteLine( MiscUtils.toString( engine.eval( cb2 ) ) );
        }

        public static void test2a ( Engine engine ) {
            var sl = new StringLexer( " test; 42 /* blok */ { + ++ += //slowo xlowo3 aaa } .\nslowo xlowo3 aaa.} \"strin\" 123 3.14", engine );
            Console.WriteLine( sl.createLinearSyntax().debugToString() );
            Console.WriteLine( "" + sl.createLinearSyntax() );
        }

        public static void test2b ( Engine engine ) {
            var sl = new StringLexer( "for(i=1;i<2;i++) { print(i); }", engine );

            Console.WriteLine( sl.createLinearSyntax().debugToString() );
            Console.WriteLine( "" + sl.createLinearSyntax() );
        }

        public static void test3 ( Engine engine ) {
            string[] ss = {
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
                }",
                "testObj1 + 1",
            };
            testRun( ss, engine );
        }

        public static void testRun ( string[] inputs, Engine engine ) {
            foreach( string s in inputs ) {
                testRun( s, engine ); // completed
            }
        }

        public static void testRun ( string input, Engine engine ) {
            var sl = new StringLexer( input, engine );

            LinearSyntax ls = sl.createLinearSyntax();

            //Console.WriteLine( ls.ToString() );
            //Console.WriteLine( ls.debugToString() );
            CodeBlock cn = ls.buildParseTree();
            //Console.WriteLine( cn );
            try {
                Console.WriteLine( MiscUtils.toString( engine.eval( cn ) ) );
            } catch (Exception ex) {
                Console.WriteLine( exceptionToString( ex ) );
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

        public static int operator + ( AddTestClass a1, int a2 ) {
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

