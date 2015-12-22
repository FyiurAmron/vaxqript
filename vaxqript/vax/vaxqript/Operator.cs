using System;

namespace vax.vaxqript {
    public class Operator : IExecutable {
        public string OperatorString { get; private set; }

        public HoldType EvalType { get; private set; }

        public Func<dynamic, dynamic> UnaryLambda { get; private set; }

        public Func<dynamic, dynamic, dynamic> NaryLambda { get; private set; }

        public Operator ( string operatorString, Func<dynamic, dynamic> unaryLambda,  Func<dynamic, dynamic, dynamic> naryLambda )
            : this( operatorString, unaryLambda, naryLambda, HoldType.HoldNone ) {
        }

        public Operator ( string operatorString, Func<dynamic, dynamic> unaryLambda,  Func<dynamic, dynamic, dynamic> naryLambda, HoldType evalType ) {
            OperatorString = operatorString;
            EvalType = evalType;
            UnaryLambda = unaryLambda;
            NaryLambda = naryLambda;
        }

        public object exec ( Engine engine, params dynamic[] arguments ) {
            return engine.applyGenericOperator( this, arguments );
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

