using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeTreeBuilder {
        private Stack<CodeBlock> codeBlockStack = new Stack<CodeBlock>();
        private CodeBlock currentCodeBlock = new CodeBlock();

        public CodeTreeBuilder () {
        }

        public void consume( Token token ) {
        }

        public void down () {
            CodeBlock newBlock = new CodeBlock();
            currentCodeBlock.add( newBlock );
            codeBlockStack.Push( currentCodeBlock );
            currentCodeBlock = newBlock;
        }

        public void up () {
            currentCodeBlock = codeBlockStack.Pop();
        }
    }
}

