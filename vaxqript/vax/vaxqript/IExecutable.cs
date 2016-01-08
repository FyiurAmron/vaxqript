using System;

namespace vax.vaxqript {
    /**
    Syntax element that can be executed on <b>arbitrary</b> arguments (operator, lambda, method etc.).
    <p>
    Note that elements that have a fixed set of arguments (e.g. <code>SyntaxGroup</code>) must implement <code>IEvaluable</code> instead.
    */
    public interface IExecutable : ISyntaxElement, IHoldable {
        // TODO make arguments a IList<dynamic> object, but use a wrapped array implementation for compatibility?
        // sometimes ValueList should be passed as arguments[]...
        dynamic exec ( Engine engine, params dynamic[] arguments );
    }
}

