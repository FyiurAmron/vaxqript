using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class SyntaxTreeBuilder {
        private Stack<ISyntaxGroup> codeBlockStack = new Stack<ISyntaxGroup>();
        private Stack<bool> hasSeparatorStack = new Stack<bool>();
        private SyntaxGroup root = new SyntaxGroup();
        private ISyntaxGroup currentCodeBlock;
        private bool hasSeparator;
        private bool ignoreLastSeparator = true; // i.e. ';}' sequence, that should usually be interpreted as just '}'
        private SyntaxFlow lastFlow = SyntaxFlow.None;

        public SyntaxTreeBuilder () {
            currentCodeBlock = root;
        }

        public void consume ( ISyntaxElement syntaxElement ) {
            SyntaxFlowOperator flowOp = syntaxElement as SyntaxFlowOperator;
            if( flowOp != null ) {
                SyntaxFlow flow = flowOp.Flow;
                switch (flow) {
                case SyntaxFlow.Down:
                    _down();
                    break;
                case SyntaxFlow.Up:
                    _upExt();
                    break;
                case SyntaxFlow.Separator:
                    _separator();
                    break;
                default:
                    throw new InvalidOperationException( "unknown/unsupported Flow '" + flow + "'" );
                }
                lastFlow = flow;
                return;
            }  // else not a flow, so just attach as a node
            if( !( syntaxElement is IComment ) ) {
                lastFlow = SyntaxFlow.None;
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
            var list = currentCodeBlock.getEvaluableList();
            int count = list.Count;
            //if( count > 0 ) {
            list.RemoveAt( count - 1 ); // assert it's an empty CodeBlock since lastFlow == Flow.Separator
            //}
        }

        private void _separator () {
            ISyntaxGroup child = currentCodeBlock;
            _up();

            if( !hasSeparator ) {
                _back();
                _down( new SeparatorSyntaxGroup() );
                currentCodeBlock.add( child );
                hasSeparator = true;
            }
            //currentCodeBlock.add( BlockSeparator.Instance );
            _down();
        }

        private void _up () {
            if( codeBlockStack.Count == 0 ) {
                root = new SyntaxGroup();
                root.add( currentCodeBlock );
                currentCodeBlock = root;
            } else {
                currentCodeBlock = codeBlockStack.Pop();
                hasSeparator = hasSeparatorStack.Pop();
            }
        }

        private void _upExt () {
            _up();
            if( ignoreLastSeparator && lastFlow == SyntaxFlow.Separator ) {
                _back();
            }
            if ( hasSeparator ) {
                _up();
            }
        }

        private void _down ( ISyntaxGroup syntaxGroup ) {
            currentCodeBlock.add( syntaxGroup );
            codeBlockStack.Push( currentCodeBlock );
            hasSeparatorStack.Push( hasSeparator );
            hasSeparator = false;
            currentCodeBlock = syntaxGroup;
        }

        private void _down () {
            _down( new SyntaxGroup() );
        }

        private void _end () {
            while( codeBlockStack.Count > 0 ) {
                _upExt();
            }
        }

        public SyntaxGroup getRoot () {
            return root;
        }
    }
}

