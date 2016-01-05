using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class CodeTreeBuilder {
        private Stack<CodeBlock> codeBlockStack = new Stack<CodeBlock>();
        private Stack<bool> hasSeparatorStack = new Stack<bool>();
        private CodeBlock root = new CodeBlock();
        private CodeBlock currentCodeBlock;
        private bool hasSeparator;
        private bool ignoreLastSeparator = true; // i.e. ';}' sequence, that should usually be interpreted as just '}'
        private Flow lastFlow = Flow.None;

        public CodeTreeBuilder () {
            currentCodeBlock = root;
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
                case Flow.Separator:
                    _separator();
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

        public void separator () {
            _separator();
        }

        private void _back () {
            var list = currentCodeBlock.getArgumentList();
            int count = list.Count;
            //if( count > 0 ) {
            list.RemoveAt( count - 1 ); // assert it's an empty CodeBlock since lastFlow == Flow.Separator
            //}
        }

        private void _separator () {
            CodeBlock child = currentCodeBlock;
            _up();
            if( hasSeparator ) {
                _down();
            } else {
                _back();
                _down();
                currentCodeBlock.add( child );
                hasSeparator = true;
                _down();
            }
        }

        private void _up () {
            if( codeBlockStack.Count == 0 ) {
                root = new CodeBlock();
                root.add( currentCodeBlock );
                currentCodeBlock = root;
            } else {
                currentCodeBlock = codeBlockStack.Pop();
                hasSeparator = hasSeparatorStack.Pop();
            }
        }

        private void _upExt () {
            _up();
            if( ignoreLastSeparator && lastFlow == Flow.Separator ) {
                _back();
            }
            if ( hasSeparator ) {
                _up();
            }
        }

        private void _down () {
            CodeBlock newBlock = new CodeBlock();
            currentCodeBlock.add( newBlock );
            codeBlockStack.Push( currentCodeBlock );
            hasSeparatorStack.Push( hasSeparator );
            hasSeparator = false;
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

