using System;

namespace vax.vaxqript  {
    public interface IEvaluable : ISyntaxElement {
        object eval ();
    }
}

