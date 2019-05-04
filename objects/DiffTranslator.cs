using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter
{
    public abstract class DiffTranslator
    {
        public abstract string Section { get; }

        public abstract IEnumerable<DiffInstance> Difference(IEnumerable<DiffInstance> aDiffs);
    }
}
