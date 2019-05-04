using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetSnapshotter.translators
{
    public class MetadataTranslator : DiffTranslator
    {
        public override string Section { get => "Metadata"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            foreach (DiffInstance aDiff in Snapshotter.TranslateSettings(Section, aDiffs, TranslateKey))
                yield return aDiff;
        }

        private static string TranslateKey(string aKey)
        {
            return
                aKey == "Title"         ? "Romanized title" :
                aKey == "TitleUnicode"  ? "Unicode title" :
                aKey == "Artist"        ? "Romanized artist" :
                aKey == "ArtistUnicode" ? "Unicode artist" :
                aKey == "Version"       ? "Difficulty name" :
                aKey == "BeatmapID"     ? "Beatmap ID" :
                aKey == "BeatmapSetID"  ? "Beatmapset ID" :
                aKey;
        }
    }
}
