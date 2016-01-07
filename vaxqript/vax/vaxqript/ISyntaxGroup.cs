using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public interface ISyntaxGroup : IExecutable, IEvaluable {
        void add ( ISyntaxElement syntaxElement );
        IList<IEvaluable> getEvaluableList ();
    }
}

