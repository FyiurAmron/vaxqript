using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class MainClass {
        public static void Main ( string[] args ) {
            try{
                _Main(args);
            } catch ( Exception ex ) {
                Console.WriteLine( Test.exceptionToString( ex ) );
            }
            Console.ReadKey();
        }

        public static void _Main ( string[] args ) {
            Engine engine = new Engine();

            Console.WriteLine( "=== TEST 1a ===" );
            Test.test1a( engine ); // completed
            Console.WriteLine( "=== TEST 1b ===" );
            Test.test1b( engine ); // completed
            Console.WriteLine( "=== TEST 2a  ===" );
            Test.test2a( engine ); // completed
            Console.WriteLine( "=== TEST 2b  ===" );
            Test.test2b( engine ); // completed
            Console.WriteLine( "=== TEST 3  ===" );
            Test.setupTestArrays( engine );
            Test.test3( engine );
            Console.WriteLine( "=== TEST 4  ===" );

            string[] inputs = {
                "@(println(\"!!! text output\"+\"\t\"+ 7))",
                "if ((2+2)==4) {42}",
                "\"> test while\"",
                "i=(-10);while(i<10){i++;}; i;",
                "\"> test for\"",
                "@{for(i=1;i<10;i++) { println(i); }}",
                "\"> test do\"",
                "i = 0; do { println(i); i--; } while (i>(-10));",
                "\"> test calls\"",
                //"$engine.\"globalVarsToString\"()",
                //"vax.vaxqript.Test.test1a($engine)"
                "(\"vax.vaxqript.Test\"?).testMethod()",
                "vax.vaxqript.Test.testMethod()",
                "evi := (if(i>10)2 else if(i>5)1 else 0)",
                ":: {i=12;evi} {i=6;evi} {i=2;evi}",
                "throw (new System.Exception(\"test exception ^_^\"))",
                "for(i = 0; i < 100; i++ ) {if(i>13){return 42}}",
                "i",
                "@(i=13)",
                "i--;while(i>(-9001)){if(i<(-41)){break}; i-=2}",
                "i",
                "i=0;do{i++;if(i>600){break 665}} while(true)",
                "i",
                "f:=(z=($args[0]);z*z)",
                "f(111)"
            };

            Test.testRun( inputs, engine );

            Console.WriteLine( "=== READ-EVAL-PRINT LOOP ===" );
            engine.loop();
        }
    }
}
