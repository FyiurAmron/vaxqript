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
        public object exec ( Engine engine, params dynamic[] arguments ) {
            engine.increaseStackCount();
            object ret = invoke( arguments );
            engine.decreaseStackCount();
            return ret;
        }

        public override string ToString () {
            return string.Format( "[ObjectMethod]\nobject: " + MiscUtils.toString( Object ) + "\nmethod: " + MiscUtils.toString( Method ) );
        }
    }
}

