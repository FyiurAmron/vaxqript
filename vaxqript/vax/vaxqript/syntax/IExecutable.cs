using System;

namespace vax.vaxqript {
    public interface IExecutable : ISyntaxElement {
        object exec ( params dynamic[] arguments );
    }
}

