﻿using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeTreeBuilder {
        private Stack<CodeBlock> codeBlockStack = new Stack<CodeBlock>();
        private CodeBlock root = new CodeBlock();
        private CodeBlock currentCodeBlock;
        private bool implicitFirstDown = true;

        public CodeTreeBuilder () {
            currentCodeBlock = root;
            if( implicitFirstDown )
                down();
        }

        public void consume ( ISyntaxElement syntaxElement ) {
            FlowOperator flow = syntaxElement as FlowOperator;
            if( flow != null ) {
                switch (flow.Flow) {
                case Flow.Down:
                    down();
                    return;
                case Flow.Up:
                    up();
                    return;
                case Flow.UpDown:
                    up();
                    down();
                    return;
                default:
                    throw new InvalidOperationException( "unknown Flow '" + flow.Flow + "'" );
                }
            } // else not a flow, so just attach as a node
            currentCodeBlock.add( syntaxElement );
        }

        public void down() {
            _down();
        }

        public void up() {
            _up();
        }

        private void _down () {
            CodeBlock newBlock = new CodeBlock();
            currentCodeBlock.add( newBlock );
            codeBlockStack.Push( currentCodeBlock );
            currentCodeBlock = newBlock;
        }

        private void _up () {
            currentCodeBlock = codeBlockStack.Pop();
        }

        public CodeBlock getRoot () {
            return root;
        }
    }
}

