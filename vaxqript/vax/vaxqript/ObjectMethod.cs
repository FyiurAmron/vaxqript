using System;
using System.Reflection;

namespace vax.vaxqript {
    public class ObjectMethod {
        object Object { get; set; }

        MethodInfo Method { get; set; }

        public ObjectMethod ( Object obj, MethodInfo method ) {
            Object = obj;
            Method = method;
        }

        public object invoke ( params object[] args ) {
            return Method.Invoke( Object, args );
        }

        public override string ToString () {
            return string.Format( "object: " + MiscUtils.toString(Object) + "\nmethod: " + Method );
        }
    }
}

