using MapsetSnapshotter.objects;
using MapsetSnapshotter.translators;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter
{
    public class TranslatorRegistry
    {
        private static List<DiffTranslator> translators = new List<DiffTranslator>();
        private static bool initialized;

        public static void InitalizeTranslators()
        {
            if (initialized)
                return;

            RegisterTranslator(new ColoursTranslator());
            RegisterTranslator(new DifficultyTranslator());
            RegisterTranslator(new EditorTranslator());
            RegisterTranslator(new EventsTranslator());
            RegisterTranslator(new FilesTranslator());
            RegisterTranslator(new GeneralTranslator());
            RegisterTranslator(new HitObjectsTranslator());
            RegisterTranslator(new MetadataTranslator());
            RegisterTranslator(new TimingTranslator());

            initialized = true;
        }

        public static void RegisterTranslator(DiffTranslator aTranslator)
        {
            translators.Add(aTranslator);
        }

        public static IEnumerable<DiffTranslator> GetTranslators()
        {
            InitalizeTranslators();
            return new List<DiffTranslator>(translators);
        }

    }
}
