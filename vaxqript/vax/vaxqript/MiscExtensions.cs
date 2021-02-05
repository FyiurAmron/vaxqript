namespace vax.vaxqript {

using System.Collections;
using System.Collections.Generic;

public static class MiscExtensions {
    public static string toString( this IEnumerable iEnumerable ) {
        return MiscUtils.join( iEnumerable );
    }

    public static string toString<T>( this IEnumerable<T> iEnumerable ) {
        return MiscUtils.join( iEnumerable );
    }
}

}
