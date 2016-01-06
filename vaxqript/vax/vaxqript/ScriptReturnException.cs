using System;

namespace vax.vaxqript {
	public class ScriptExitException : Exception	{
        public object ReturnValue { get; private set; }

        public ScriptExitException( object returnValue ) {
            ReturnValue = returnValue;   
        }
	}
}

