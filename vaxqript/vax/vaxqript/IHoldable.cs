using System;

namespace vax.vaxqript {
    public interface IHoldable {
         HoldType getHoldType ( Engine engine ); // not a property due to the need of easy script accessibility
    }
}

