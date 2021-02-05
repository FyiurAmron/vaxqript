using System.Collections;
using System.Text;

namespace vax.vaxqript {

public class ValueWrapper : IWrapper {
    public object Value { get; set; }

    public dynamic eval( Engine engine ) {
        return Value;
    }

    public ValueWrapper( object value ) {
        Value = value;
    }

    // TODO handle string instances specially (quote them!) OR add a StringWrapper : ValueWrapper extension maybe?

    private const char QUOTE_CHAR = '"';
    // TODO get based on engine?

    public string valueToString() {
        switch ( Value ) {
            case null:
                return "null";
            case string s: {
                StringBuilder sb = new();
                sb.Append( QUOTE_CHAR );
                foreach ( char c in s ) {
                    sb.Append( Lexer.escape( c ) );
                }

                sb.Append( QUOTE_CHAR );
                return sb.ToString();
            }
            default:
                return ( Value is IEnumerable ie )
                    ? ie.toString()
                    : Value.ToString();
        }
    }

    public override string ToString() {
        return ( Value != null )
            ? "/* " + Value.GetType().Name + " */ " + valueToString()
            : "/* null */ null";
    }
}

}
