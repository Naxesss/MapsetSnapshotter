using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.translators
{
    public class ColoursTranslator : DiffTranslator
    {
        public override string Section { get => "Colours"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            foreach (DiffInstance aDiff in Snapshotter.TranslateSettings(Section, aDiffs, TranslateKey))
                yield return aDiff;
        }

        private static string TranslateKey(string aKey)
        {
            return
                aKey == "Combo1" ? "Combo 1" :
                aKey == "Combo2" ? "Combo 2" :
                aKey == "Combo3" ? "Combo 3" :
                aKey == "Combo4" ? "Combo 4" :
                aKey == "Combo5" ? "Combo 5" :
                aKey == "Combo6" ? "Combo 6" :
                aKey == "Combo7" ? "Combo 7" :
                aKey == "Combo8" ? "Combo 8" :
                aKey == "Combo9" ? "Combo 9" :

                aKey == "SliderBody"          ? "Slider body" :
                aKey == "SliderTrackOverride" ? "Slider track override" :
                aKey == "SliderBorder"        ? "Slider border" :
                aKey;
        }
    }
}
