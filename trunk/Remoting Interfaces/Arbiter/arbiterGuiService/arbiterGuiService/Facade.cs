using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.arbiterGuiService
{
    public abstract class Facade : MarshalByRefObject
    {
        // trigger the arbiterGui to update
        public abstract void TriggerUpdate();
    }
}
