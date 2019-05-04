using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter
{
    class DiffTranslator
    {
        public Type mClass;
        public string mSection;

        public DiffTranslator(Type aClass, string aSection)
        {
            mClass = aClass;
            mSection = aSection;
        }
    }
}
