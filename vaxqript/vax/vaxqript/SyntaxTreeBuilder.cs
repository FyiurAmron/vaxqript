using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class SyntaxTreeBuilder {
        private Stack<ISyntaxGroup> syntaxGroupStack = new Stack<ISyntaxGroup>();
        private Stack<bool> hasSeparatorStack = new Stack<bool>();
        private SyntaxGroup root = new SyntaxGroup();
        private ISyntaxGroup currentSyntaxGroup;
        private bool hasSeparator;
        private bool ignoreLastSeparator = false;
        // i.e. ';}' sequence, that should usually be interpreted as just '}'
        private SyntaxFlow lastFlow = SyntaxFlow.None;

        public SyntaxTreeBuilder () {
            currentSyntaxGroup = root;
        }

        public void consume ( ISyntaxElement syntaxElement ) {
            SyntaxFlowOperator flowOp = syntaxElement as SyntaxFlowOperator;
            if( flowOp != null ) {
                SyntaxFlow flow = flowOp.Flow;
                switch (flow) {
                case SyntaxFlow.Down:
                    _down( new SyntaxGroup() );
                    break;
                case SyntaxFlow.DownArguments:
                    _down( new ArgumentGroup() );
                    break;
                case SyntaxFlow.Up:
                case SyntaxFlow.UpArguments:
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
                currentSyntaxGroup.add( syntaxElement );
            }
            // else just ignore, since it's an IComment
        }

        public void end () {
            _end();
        }

        public void down ( ISyntaxGroup syntaxGroup ) {
            _down( syntaxGroup );
        }

        public void up () {
            _upExt();
        }

        public void separator () {
            _separator();
        }

        private void _back () {
            var list = currentSyntaxGroup.getEvaluableList();
            int count = list.Count;
            //if( count > 0 ) {
            list.RemoveAt( count - 1 ); // assert it's an empty CodeBlock since lastFlow == Flow.Separator
            //}
        }

        private void _separator () {
            ISyntaxGroup child = currentSyntaxGroup;
            _up();  
            if( !hasSeparator ) {
                // TODO optimise this
                ArgumentGroup ag = child as ArgumentGroup;
                if ( ag != null ) {
                    SyntaxGroup sg = new SyntaxGroup();
                    sg.setFrom( ag );
                    child = sg;
                }
                _back();
                _down( new SeparatorSyntaxGroup() );
                currentSyntaxGroup.add( child );
                hasSeparator = true;
            }
            //currentCodeBlock.add( BlockSeparator.Instance );
            _down( new SyntaxGroup() );
        }

        private void _up () {
            if( syntaxGroupStack.Count == 0 ) {
                root = new SyntaxGroup();
                root.add( currentSyntaxGroup );
                currentSyntaxGroup = root;
            } else {
                currentSyntaxGroup = syntaxGroupStack.Pop();
                hasSeparator = hasSeparatorStack.Pop();
            }
        }

        private void _upExt () {
            _up();
            if( ignoreLastSeparator && lastFlow == SyntaxFlow.Separator ) {
                _back();
            }
            if( hasSeparator ) {
                _up();
            }
        }

        private void _down ( ISyntaxGroup syntaxGroup ) {
            currentSyntaxGroup.add( syntaxGroup );
            syntaxGroupStack.Push( currentSyntaxGroup );
            hasSeparatorStack.Push( hasSeparator );
            hasSeparator = false;
            currentSyntaxGroup = syntaxGroup;
        }

        private void _end () {
            while( syntaxGroupStack.Count > 0 ) {
                _upExt();
            }
        }

        public SyntaxGroup getRoot () {
            return root;
        }
    }
}

