﻿using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class MainClass {
        public static void Main ( string[] args ) {
            try {
                _Main( args );
            } catch (Exception ex) {
                Console.WriteLine( Test.exceptionToString( ex ) );
            }
            Console.ReadKey();
        }

        public static void _Main ( string[] args ) {
            Engine engine = new Engine();

            // additional/brand new tests sandbox
            string[] inputs = {
                "\"=== QUICK TESTS ===\"",
                "fun = {new vax.vaxqript.Function( `{($args0[0]) * 2} )}",
                "fun(420)",
                "fun",
                "fun = (function( ($args0[0]) + 1 ))",
                "fun(900)",
                "fun(\"900\")",
                "\"=== END OF TESTS ===\"",
            };

            bool doTests = true;

            if( doTests ) {
                Console.WriteLine( "=== TEST 1a ===" );
                Test.test1a( engine ); // completed
                Console.WriteLine( "=== TEST 1b ===" );
                Test.test1b( engine ); // completed
                Console.WriteLine( "=== TEST 2a  ===" );
                Test.test2a( engine ); // completed
                Console.WriteLine( "=== TEST 2b  ===" );
                Test.test2b( engine ); // completed
                Console.WriteLine( "=== TEST 3  ===" );
                Test.test3( engine ); // completed
                Console.WriteLine( "=== TEST 4  ===" );
                Test.test4( engine ); // completed
                Console.WriteLine( "=== TEST 5  ===" );
                Test.test5( engine );
                Console.WriteLine( "=== TEST t  ===" );
                Test.testTime( engine ); // TODO profile; we had a nasty perf drop lately
                Test.testRun( inputs, engine );
            }

            Console.WriteLine( "=== READ-EVAL-PRINT LOOP ===" );
            engine.loop();
        }
    }
}
