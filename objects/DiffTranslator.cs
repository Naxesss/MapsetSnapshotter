using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.objects
{
    public abstract class DiffTranslator
    {
        public abstract string Section { get; }

        public abstract IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs);
    }
}
