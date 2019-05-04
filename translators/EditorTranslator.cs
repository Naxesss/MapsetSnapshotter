using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.translators
{
    public class EditorTranslator : DiffTranslator
    {
        public override string Section { get => "Editor"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            foreach (DiffInstance aDiff in Snapshotter.TranslateSettings(Section, aDiffs, TranslateKey))
                yield return aDiff;
        }

        private static string TranslateKey(string aKey)
        {
            return
                aKey == "DistanceSpacing" ? "Distance spacing" :
                aKey == "BeatDivisor"     ? "Beat snap divisor" :
                aKey == "GridSize"        ? "Grid size" :
                aKey == "TimelineZoom"    ? "Timeline zoom" :
                aKey;
        }
    }
}
