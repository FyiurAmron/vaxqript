using System;

namespace vax.vaxqript {
    public interface IExecutable : ISyntaxElement {
        object exec ( Engine engine, params dynamic[] arguments );
    }
}

