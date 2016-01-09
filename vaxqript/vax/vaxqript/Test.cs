using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public static class Test {
        public static readonly int testField = 991;

        public static string exceptionToString ( Exception ex ) {
            return ">>> EXCEPTION: " + ex.GetType() + ":\n>>> " + ex.Message;
        }

        public static string testMethod () {
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
                Console.WriteLine( exceptionToString( ex ) );
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
            SyntaxGroup cb1 = new SyntaxGroup(), cb2 = new SyntaxGroup();
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
            LinearSyntax ls = sl.createLinearSyntax();
            Console.WriteLine( ls.debugToString() );
            Console.WriteLine( "" + ls );
            //Console.WriteLine( "" + ls.buildParseTree() );
        }

        public static void test2b ( Engine engine ) {
            var sl = new StringLexer( "for(i=1;i<2;i++) { print(i); }", engine );
            LinearSyntax ls = sl.createLinearSyntax();
            SyntaxGroup cb = ls.buildParseTree();
            Console.WriteLine( ls.debugToString() );
            Console.WriteLine( "" + ls );
            Console.WriteLine( "" + cb );
        }

        public static void test3 ( Engine engine ) {
            setupTestArrays( engine );
            string[] ss = {
                "{ + 4 1 { * 3 11 } }", // 38
                "{ + 4 2 { * 3 3", // 15
                "{ 4 + 2 + ( 3 * 3 )", // 15
                "4 2 ( 3 3 * ) +", // 15
                "4 + 2 + ( 3.1 * 3 )", // 15.3
                "foo", // 8
                "{ 4 + 2 + ( 3.1 * 3 ); 10.5; foo * 2", // note: 'foo' is declared in previous tests!
                // 16
                ":: { 4 + 2 + ( 3.1 * 3 )} {10.5} {foo * 2}", // note: 'foo' is declared in previous tests!
                // 15.3,10.5,16
                @"{
                    i = 3;
                    i++;
                    i
                }", // 4
                @"{
                    i = 3;
                    i = 10;
                    i++;
                    i + 7
                }", // 18
                "testObj1 + 1", // 13
                "vars := {$engine.globalVarsToString()};\"\"",
                "println(\"!!! text output\"+\"\t\"+ 7);\"\"",
            };
            testRun( ss, engine );
        }

        public static void test4 ( Engine engine ) {
            string[] ss = {
                "\"> test if\"",
                "if ((2+2)==4) {42}",
                "evi := {if(i>10)2 else if(i>5)1 else 0}",
                ":: {i=12;evi} {i=6;evi} {i=2;evi}",
                "\"> test while\"",
                "i=(-10);while(i<10){i++;}; i",
                "\"> test for\"",
                "for(i=1;i<10;i++) { println(i); };\"\"",
                "\"> test do\"",
                "i = 0; do { println(i); i--; } while (i>(-10));",
                "\"> test calls\"",
                //"$engine.\"globalVarsToString\"()",
                //"vax.vaxqript.Test.test1a($engine)"
                "{\"vax.vaxqript.Test\"?}.testMethod()",
                "vax.vaxqript.Test.testMethod()",
                "throw (new System.Exception(\"test exception ^_^\"))",
                "for(i = 0; i < 100; i++ ) {if(i>13){return 42}}", // 42
                "i", // 14
                "i=13;\"\"", //
                "i--;while(i>(-9001)){if(i<(-41)){break}; i-=2}", // null
                "i", // -42
                "i=0;do{i++;if(i>600){break 665}} while()", // 665
                "i", // 601
                "f:={z={$args0[0]};z*z}",
                "f(111)",
            };
            testRun( ss, engine );
        }

        public static void test5 ( Engine engine ) {
            string[] ss = {
                "(System.Environment.TickCount)",
                "x = (vax.vaxqript.Test.testField);?x", 
                "x := (vax.vaxqript.Test.testField);?x",
                "(x)",
                "throw (new System.Exception())",
                "1/0",
                "try(1/0)",
                "try{1/0};\"\"",
                "try{1/0}catch{($ex.Message)}",
                "try{throw (new System.ArithmeticException())}catch{($ex.Message)}",
                "try{throw (new System.ArithmeticException())}catch(System.ArithmeticException excc){(excc.Message)}",
                "try{throw (new System.Exception())}catch(System.ArithmeticException excc){(excc.Message)}",
                "i=113;try{i=0;1/0}finally{i=13}"
            };
            testRun( ss, engine );
        }

        public static void test6 ( Engine engine ) {
            string[] ss = {
                "fun = {new vax.vaxqript.Function( `{($args0[0]) * 2} )}",
                "fun(420)",
                "fun",
                "f={new vax.vaxqript.Function({`(x*2)},{::(`x)})}",
                "fun(420)",
                "fun = (function( ($args0[0]) + 1 ))",
                "fun(900)",
                "fun(\"900\")",
                "fun = (function(x,y){x*y})",
                "try{fun(900)}catch{($ex.Message)}",
                "try{fun}catch{($ex.Message)}",
                "fun(2,5)",
                "fun = ((x,y)=>{x*y})",
                "try{fun(900)}catch{($ex.Message)}",
                "fun(21,2)",
            };
            testRun( ss, engine );
        }

        public static void testTime ( Engine engine ) {
            string s = "loops = 100_000;" +
                "start = (vax.vaxqript.MiscUtils.getCurrentTimeMillis());" +
                "for(i=0;i<loops;i++){};" +
                "stop = (vax.vaxqript.MiscUtils.getCurrentTimeMillis());" +
                "(stop-start) + \" ms for \" + loops + \" iterations\"";
            testRun( s, engine );
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
            SyntaxGroup cn = ls.buildParseTree();
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

