using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.translators
{
    public class DifficultyTranslator : DiffTranslator
    {
        public override string Section { get => "Difficulty"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            foreach (DiffInstance aDiff in Snapshotter.TranslateSettings(Section, aDiffs, TranslateKey))
                yield return aDiff;
        }

        private static string TranslateKey(string aKey)
        {
            return
                aKey == "HPDrainRate"       ? "HP drain rate" :
                aKey == "CircleSize"        ? "Circle size" :
                aKey == "OverallDifficulty" ? "Overall difficulty" :
                aKey == "ApproachRate"      ? "Approach rate" :
                aKey;
        }
    }
}
