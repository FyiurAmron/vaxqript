using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeTreeBuilder {
        private Stack<CodeBlock> codeBlockStack = new Stack<CodeBlock>();
        private CodeBlock root = new CodeBlock();
        private CodeBlock currentCodeBlock;
        private bool implicitFirstDown = true,
            ignoreLastUpDown = true;
        // i.e. ';}' sequence, that should usually be interpreted as just '}'
        private Flow lastFlow = Flow.None;

        public CodeTreeBuilder () {
            currentCodeBlock = root;
            if( implicitFirstDown )
                _down();
        }

        public void consume ( ISyntaxElement syntaxElement ) {
            FlowOperator flowOp = syntaxElement as FlowOperator;
            if( flowOp != null ) {
                Flow flow = flowOp.Flow;
                switch (flow) {
                case Flow.Down:
                    _down();
                    break;
                case Flow.Up:
                    _upExt();
                    break;
                case Flow.UpDown:
                    _up();
                    _down();
                    break;
                default:
                    throw new InvalidOperationException( "unknown/unsupported Flow '" + flow + "'" );
                }
                lastFlow = flow;
                return;
            }  // else not a flow, so just attach as a node
            if( !( syntaxElement is IComment ) ) {
                lastFlow = Flow.None;
                currentCodeBlock.add( syntaxElement );
            }
            // else just ignore, since it's an IComment
        }

        public void end () {
            _end();
        }

        public void down () {
            _down();
        }

        public void up () {
            _upExt();
        }

        private void _up () {
            currentCodeBlock = codeBlockStack.Pop();
        }

        private void _upExt () {
            _up();
            if( ignoreLastUpDown && lastFlow == Flow.UpDown ) {
                var list = currentCodeBlock.getArgumentList();
                int count = list.Count;
                if( count > 0 ) {
                    list.RemoveAt( count - 1 );
                }
            }
        }

        private void _down () {
            CodeBlock newBlock = new CodeBlock();
            currentCodeBlock.add( newBlock );
            codeBlockStack.Push( currentCodeBlock );
            currentCodeBlock = newBlock;
        }

        private void _end () {
            while( codeBlockStack.Count > 0 ) {
                _upExt();
            }
        }

        public CodeBlock getRoot () {
            return root;
        }
    }
}

