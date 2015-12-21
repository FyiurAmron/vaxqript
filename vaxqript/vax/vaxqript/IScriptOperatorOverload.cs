using System;

namespace vax.vaxqript {
    public interface IScriptOperatorOverload {
        //Result process( string opString, params dynamic[] arguments );
        ValueWrapper process ( string opString, dynamic argument );
    }
}

