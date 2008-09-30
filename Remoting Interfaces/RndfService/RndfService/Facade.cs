using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using UrbanChallenge.Rndf;

namespace UrbanChallenge.RndfService
{
    public abstract class Facade : MarshalByRefObject
    {
        // create mdf and rndf through memorystreams as filestreams can't be serialized
        public abstract void createRndf(MemoryStream memoryStream);
        public abstract void createMdf(MemoryStream memoryStream);

        // retreive rndf and mdf
        public abstract IRndf getRndf();
        public abstract IMdf getMdf();
    }
}
