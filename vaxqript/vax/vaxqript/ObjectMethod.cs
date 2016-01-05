using System;
using System.Reflection;

namespace vax.vaxqript {
    public class ObjectMethod : IExecutable {
        object Object { get; set; }

        MethodInfo Method { get; set; }

        public ObjectMethod ( Object obj, MethodInfo method ) {
            Object = obj;
            Method = method;
        }

        public object invoke ( params object[] args ) {
            return Method.Invoke( Object, args );
        }

        public HoldType getHoldType ( Engine engine ) {
            return HoldType.None;
        }

        // added for convenience
        public dynamic exec ( Engine engine, params dynamic[] arguments ) {
            engine.pushCallStack( this );
            object ret = invoke( arguments );
            engine.popCallStack();
            return ret;
        }

        public override string ToString () {
            return string.Format( "[ObjectMethod]\nobject: " + MiscUtils.toString( Object ) + "\nmethod: " + MiscUtils.toString( Method ) );
        }
    }
}

