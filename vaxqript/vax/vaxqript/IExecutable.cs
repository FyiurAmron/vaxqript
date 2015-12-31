﻿using System;

namespace vax.vaxqript {
    /**
    Syntax element that can be executed on <b>arbitrary</b> arguments (operator, lambda, method etc.).
    <p>
    Note that elements that have a fixed set of arguments (e.g. <code>CodeBlock</code>) must implement <code>IEvaluable</code> instead.
    */
    public interface IExecutable : ISyntaxElement, IHoldable {
        object exec ( Engine engine, params dynamic[] arguments );
    }
}

