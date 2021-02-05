using System;

namespace vax.vaxqript {

using System.Collections.Generic;

public class ArgumentGroup : AbstractSyntaxGroup {
    public ArgumentGroup() : base() {
    }

    public ArgumentGroup( IEnumerable<ISyntaxElement> syntaxElements ) : base( syntaxElements ) {
    }

    public ArgumentGroup( params ISyntaxElement[] syntaxElements ) : base( syntaxElements ) {
    }

    public override dynamic _eval( Engine engine ) {
        int count = arguments.Count;
        if ( op != null ) { // doing this before count check allows nullary (const) operators
            return op.exec( engine, prepareArguments( engine, 0 ) );
        }

        if ( count == 0 ) {
            return null;
        }

        if ( arguments[0] is Identifier id ) {
            ValueWrapper vw = engine.getIdentifierValue( id );
            if ( vw != null ) {
                if ( count == 1 ) {
                    if ( vw.Value is IEvaluable iEva ) {
                        return iEva.eval( engine );
                    }
                } //else {

                idExec = vw.Value as IExecutable;
                if ( idExec != null ) {
                    return idExec.exec( engine, prepareArguments( engine, 1 ) );
                }

                //}
            }
        }

        object o = arguments[0].eval( engine );

        if ( o is CompositeIdentifier cid ) {
            if ( count == 1 ) {
                return cid.eval( engine );
            } else {
                idExec = cid;
                return idExec.exec( engine, prepareArguments( engine, 1 ) );
            }
        }

        ValueList ret = new(count) { o };
        int i = 0;
        foreach ( IEvaluable iEva in arguments ) {
            if ( i != 0 ) {
                ret.Add( iEva.eval( engine ) );
            } else {
                i++;
            }
        }

        return ret;
    }

    public override string ToString() {
        return " ( " + ( ( op != null )
            ? op + " " + string.Join( " ", arguments )
            : string.Join( ",", arguments ) ) + " ) ";
    }
}

}
