using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter
{
    public class TranslatorRegistry
    {
        private static List<DiffTranslator> translators = new List<DiffTranslator>();

        public static void RegisterTranslator(DiffTranslator aTranslator)
        {
            translators.Add(aTranslator);
        }

        public static IEnumerable<DiffTranslator> GetTranslators()
        {
            return new List<DiffTranslator>(translators);
        }
    }
}
