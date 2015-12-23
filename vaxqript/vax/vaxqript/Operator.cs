using System;

namespace vax.vaxqript {
    public class Operator : IExecutable {
        public string OperatorString { get; private set; }

        public HoldType HoldType { get; private set; }

        public Associativity Associativity{ get; private set; }

        public Func<dynamic, dynamic> UnaryLambda { get; private set; }

        public Func<dynamic, dynamic, dynamic> NaryLambda { get; private set; }

        public Operator ( string operatorString, Func<dynamic, dynamic> unaryLambda, Func<dynamic, dynamic, dynamic> naryLambda )
            : this( operatorString, unaryLambda, naryLambda, HoldType.None, Associativity.LeftToRight ) {
        }

        public Operator ( string operatorString, Func<dynamic, dynamic> unaryLambda, Func<dynamic, dynamic, dynamic> naryLambda, HoldType holdType )
            : this( operatorString, unaryLambda, naryLambda, holdType, Associativity.LeftToRight ) {
        }

        public Operator ( string operatorString, Func<dynamic, dynamic> unaryLambda, Func<dynamic, dynamic, dynamic> naryLambda, HoldType holdType,
                          Associativity associativity ) {
            OperatorString = operatorString;
            HoldType = holdType;
            UnaryLambda = unaryLambda;
            NaryLambda = naryLambda;
            Associativity = associativity;
        }

        public object exec ( Engine engine, params dynamic[] arguments ) {
            // note: engine is ignored here since the engine-related lambdas are engine-instance-related anyway
            switch (arguments.Length) {
            case 0:
                return applyNullary();
            case 1:
                return applyUnary( arguments[0] );
            default:
                return applyNary( arguments );
            }
        }

        public dynamic applyNullary () {
            throw new NotSupportedException( "not supported nullary '" + this + "'" );
        }

        public dynamic applyUnary ( dynamic argument ) {
            IScriptOperatorOverload isoo = argument as IScriptOperatorOverload;
            if( isoo != null ) {
                ValueWrapper ret = isoo.processLeft( OperatorString, argument );
                if( ret != null )
                    return ret.Value;
            }
            return UnaryLambda( argument );
        }

        public dynamic applyNary ( params dynamic[] arguments ) {
            dynamic result = arguments[0];
            int i = 1;
            for(; i < arguments.Length; i++ ) {
                IScriptOperatorOverload isoo = result as IScriptOperatorOverload;
                if( isoo != null ) {
                    ValueWrapper ret = isoo.processLeft( OperatorString, arguments[i] );
                    if( ret != null ) {
                        result = ret.Value;
                        continue;
                    }
                }
                isoo = arguments[i] as IScriptOperatorOverload;
                if( isoo != null ) {
                    ValueWrapper ret = isoo.processRight( OperatorString, arguments[i] );
                    if( ret != null ) {
                        result = ret.Value;
                        continue;
                    }
                }
                result = NaryLambda( result, arguments[i] );
            }
            return result;
        }

        public override bool Equals ( object obj ) {
            if( obj == null )
                return false;
            if( obj == this )
                return true;
            Operator op = obj as Operator;
            if( op == null )
                return false;
            return OperatorString.Equals( op.OperatorString );
        }

        public override int GetHashCode () {
            return OperatorString.GetHashCode();
        }

        public override string ToString () {
            return OperatorString;
        }
    }
}

