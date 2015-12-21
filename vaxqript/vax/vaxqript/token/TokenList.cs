using System;
using System.Collections.Generic;

namespace vax.vaxqript {
    public class TokenList : IEvaluable {
        List<IEvaluable> al = new List<IEvaluable>();

        public TokenList () {
        }

        public void add ( IEvaluable iEvaluable ) {
            al.Add( iEvaluable );
        }

        public object eval () {
            return null;
        }
    }
}

