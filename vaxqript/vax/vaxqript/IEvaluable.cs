using System;

namespace vax.vaxqript  {
    /**
    Syntax element that can be evaluated, either directly or by execution of some code on <b>fixed</b> arguments.
    <p>
    Note that elements that have a variable set of arguments (e.g. operators, lambdas, methods) must implement <code>IExecutable</code> instead.
    */
    public interface IEvaluable : ISyntaxElement {
        object eval ( Engine engine );
    }
}

