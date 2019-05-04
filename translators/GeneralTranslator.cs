using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.translators
{
    public class GeneralTranslator
    {
        public class TranslatorGeneral : DiffTranslator
        {
            public static string mSectionName = "General";

            public static IEnumerable<DiffInstance> Execute(IEnumerable<DiffInstance> aDiffs)
            {
                foreach (DiffInstance aDiff in Snapshotter.TranslateSettings(mSectionName, aDiffs, TranslateKey))
                    yield return aDiff;
            }

            private static string TranslateKey(string aKey)
            {
                return aKey == "AudioFilename" ? "Audio filename" :
                        aKey == "AudioLeadIn" ? "Audio lead-in" :
                        aKey == "PreviewTime" ? "Preview time" :
                        aKey == "SampleSet" ? "Default sample set" :
                        aKey == "StackLeniency" ? "Stack leniency" :
                        aKey == "LetterboxInBreaks" ? "Letterboxing in breaks" :
                        aKey == "WidescreenStoryboard" ? "Widescreen storyboard" :
                        aKey == "StoryFireInFront" ? "Storyboard in front of combo fire" :
                        aKey == "SpecialStyle" ? "Special N+1 style" :
                        aKey == "UseSkinSprites" ? "Use skin sprites in storyboard" :
                        aKey == "EpilepsyWarning" ? "Epilepsy warning"
                                                        : aKey;
            }
        }
    }
}
