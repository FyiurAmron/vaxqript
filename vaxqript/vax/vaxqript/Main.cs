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
                "(\"vax.vaxqript.Test\"?).testMethod()",
                "vax.vaxqript.Test.testMethod()",
                //"vax.vaxqript.Test.test1a($engine)"
            };

            Test.testRun( inputs, engine );

            Console.WriteLine( "=== READ-EVAL-PRINT LOOP ===" );
            for( string line = Console.ReadLine(); line != null && line.Length != 0; line = Console.ReadLine() ) {
                try {
                    Console.WriteLine( ">>> " + MiscUtils.toString( engine.eval( line ) ) );
                } catch (Exception ex) {
                    Console.WriteLine( Test.exceptionToString( ex ) );
                }
            }
        }
    }
}
