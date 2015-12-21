using System;

namespace vax.vaxqript {
    public interface IScriptOperatorOverload {
        //Result process( string opString, params dynamic[] arguments );
        ValueWrapper processLeft ( string opString, dynamic argument );

        ValueWrapper processRight ( string opString, dynamic argument );
    }
}

