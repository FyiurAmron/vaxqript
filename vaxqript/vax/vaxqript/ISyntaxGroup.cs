using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public interface ISyntaxGroup : IExecutable, IEvaluable {
        void add ( ISyntaxElement syntaxElement );
        IList<IEvaluable> getEvaluableList ();
        /** @return doesn't have to be equal to <code>getEvaluableList().Count == 0</code> */
        bool isEmpty ();
    }
}

