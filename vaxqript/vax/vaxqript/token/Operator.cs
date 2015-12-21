﻿using System;

namespace vax.vaxqript {
    public class Operator : IExecutable {
        private string OperatorString { get; set; }

        public Operator ( string operatorString ) {
            OperatorString = operatorString;
        }

        public object exec ( params dynamic[] arguments ) {
            return applyGenericOperator( OperatorString, arguments );
        }

        public static dynamic applyGenericOperator ( string opString, params dynamic[] arguments ) {
            switch (arguments.Length) {
            case 0:
                return applyNullaryOperator( opString );
            case 1:
                return applyUnaryOperator( opString, arguments[0] );
            default:
                return applyNaryOperator( opString, arguments );
            }
        }

        public static dynamic applyNullaryOperator ( string opString ) {
            throw new NotSupportedException( "not supported nullary '" + opString + "'" );
        }

        public static dynamic applyUnaryOperator ( string opString, dynamic argument ) {
            IScriptOperatorOverload isoo = argument as IScriptOperatorOverload;
            if( isoo != null ) {
                ValueWrapper ret = isoo.process( opString, argument );
                if( ret != null )
                    return ret.Value;
            }
            return OperatorMap.getUnaryOperator( opString )( argument );
        }

        public static dynamic applyNaryOperator ( string opString, params dynamic[] arguments ) {
            dynamic result = arguments[0];
            var operatorLambda = OperatorMap.getNaryOperator( opString );
            int i = 1;
            for( ; i < arguments.Length; i++ ) {
                IScriptOperatorOverload isoo = result as IScriptOperatorOverload;
                if( isoo != null ) {
                    ValueWrapper ret = isoo.process( opString, arguments[i] );
                    if( ret != null )
                        result = ret.Value;
                }
                result = operatorLambda( result, arguments[i] );
            }
            return result;
        }
    }
}
