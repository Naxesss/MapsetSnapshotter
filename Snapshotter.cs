using MapsetParser.objects;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MapsetSnapshotter
{
    public class Snapshotter
    {
        private const string fileNameFormat = "yyyy-MM-dd HH-mm-ss";

        public enum DiffType
        {
            Added,
            Removed,
            Changed
        }

        public struct Snapshot
        {
            public readonly DateTime creationTime;
            public readonly string beatmapSetId;
            public readonly string beatmapId;
            public readonly string saveName;
            public readonly string code;

            public Snapshot(DateTime aCreationTime, string aBeatmapSetId, string aBeatmapId, string aSaveName, string aCode)
            {
                creationTime = aCreationTime;
                beatmapSetId = aBeatmapSetId;
                beatmapId = aBeatmapId;
                saveName = aSaveName;
                code = aCode;
            }
        }

        public static void SnapshotBeatmapSet(BeatmapSet aBeatmapSet)
        {
            DateTime creationDate = DateTime.UtcNow;

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                string beatmapSetId = beatmap.metadataSettings.beatmapSetId.ToString();
                string beatmapId = beatmap.metadataSettings.beatmapId.ToString();

                foreach (Beatmap otherBeatmap in aBeatmapSet.beatmaps)
                {
                    if (otherBeatmap.metadataSettings.beatmapId == beatmap.metadataSettings.beatmapId &&
                        beatmap.mapPath != null && otherBeatmap.mapPath != null)
                    {
                        DateTime date = File.GetCreationTimeUtc(beatmap.mapPath);
                        DateTime otherDate = File.GetCreationTimeUtc(otherBeatmap.mapPath);
                            
                        // We only save the beatmap id, so if we have two of the same beatmap
                        // in the folder, we should only save the newest one.
                        if (date < otherDate)
                            return;
                    }
                }
                
                List<Snapshot> snapshots = GetSnapshots(beatmapSetId, beatmapId).ToList();
                bool shouldSave = true;

                // If our snapshot is up to date, saving is redundant.
                foreach (Snapshot snapshot in snapshots)
                    if (snapshot.creationTime == snapshots.Max(aSnapshot => aSnapshot.creationTime) && snapshot.code == beatmap.code)
                        shouldSave = false;

                if (shouldSave)
                {
                    // ./snapshots/571202/258378/2019-01-26 22-12-49
                    string saveDirectory = "snapshots/" + beatmapSetId + "/" + beatmapId;
                    string saveName = creationDate.ToString(fileNameFormat) + ".osu";

                    if (!Directory.Exists(saveDirectory))
                        Directory.CreateDirectory(saveDirectory);

                    File.WriteAllText(saveDirectory + "/" + saveName, beatmap.code);
                }
            }

            SnapshotFiles(aBeatmapSet, creationDate);
        }

        private static void SnapshotFiles(BeatmapSet aBeatmapSet, DateTime aCreationTime)
        {
            string beatmapSetId = aBeatmapSet.beatmaps?.First().metadataSettings.beatmapSetId.ToString();

            StringBuilder fileSnapshot = new StringBuilder("[Files]\r\n");

            foreach (string filePath in aBeatmapSet.songFilePaths)
            {
                if (beatmapSetId == null)
                    break;

                // We already track .osu and .osb files so we ignore these.
                if (!filePath.EndsWith(".osu") && !filePath.EndsWith(".osb"))
                {
                    string fileName = filePath.Split('/', '\\').Last();

                    // Storing the complete file would quickly take up a lot of memory so we hash it instead.
                    byte[] bytes = File.ReadAllBytes(filePath);
                    byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

                    StringBuilder hash = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                        hash.Append(hashByte.ToString("X2"));

                    fileSnapshot.Append(fileName + ": " + hash.ToString() + "\r\n");
                }
            }

            string fileSnapshotString = fileSnapshot.ToString();
            if (fileSnapshotString.Length > 0)
            {
                List<Snapshot> snapshots = GetSnapshots(beatmapSetId, "files").ToList();
                bool shouldSave = true;

                foreach (Snapshot snapshot in snapshots)
                    if (snapshot.creationTime == snapshots.Max(aSnapshot => aSnapshot.creationTime) && snapshot.code == fileSnapshotString)
                        shouldSave = false;

                if (shouldSave)
                {
                    string filesSnapshotDirectory = "snapshots/" + beatmapSetId + "/files";
                    string filesSnapshotName = filesSnapshotDirectory + "/" + aCreationTime.ToString(fileNameFormat) + ".txt";

                    if (!Directory.Exists(filesSnapshotDirectory))
                        Directory.CreateDirectory(filesSnapshotDirectory);

                    File.WriteAllText(filesSnapshotName, fileSnapshotString);
                }
            }
        }

        public static IEnumerable<Snapshot> GetSnapshots(Beatmap aBeatmap) =>
            GetSnapshots(
                aBeatmap.metadataSettings.beatmapSetId.ToString(),
                aBeatmap.metadataSettings.beatmapId.ToString());

        public static IEnumerable<Snapshot> GetSnapshots(string aBeatmapSetId, string aBeatmapId)
        {
            string saveDirectory = "snapshots/" + aBeatmapSetId + "/" + aBeatmapId;
            if (Directory.Exists(saveDirectory))
            {
                string[] filePaths = Directory.GetFiles(saveDirectory);

                for (int i = 0; i < filePaths.Length; ++i)
                {
                    int forwardSlash = filePaths[i].LastIndexOf("/");
                    int backSlash = filePaths[i].LastIndexOf("\\");

                    string saveName = filePaths[i].Substring(Math.Max(forwardSlash, backSlash) + 1);
                    string code = File.ReadAllText(filePaths[i]);

                    DateTime creationTime = DateTime.ParseExact(saveName.Split('.')[0], fileNameFormat, null);

                    yield return new Snapshot(creationTime, aBeatmapSetId, aBeatmapId, saveName, code);
                }
            }
        }

        public static IEnumerable<Snapshot> GetLatestSnapshots(DateTime aDate, BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                IEnumerable<Snapshot> snapshots = GetSnapshots(beatmap).OrderByDescending(aSnapshot => aSnapshot.creationTime);

                yield return snapshots.LastOrDefault(aSnapshot => aSnapshot.creationTime <= aDate);
            }
        }

        public static IEnumerable<DiffInstance> Compare(Snapshot aSnapshot, string aCurrentCode)
        {
            string[] snapshotLines = aSnapshot.code.Replace("\r", "").Split('\n');
            string[] currentLines = aCurrentCode.Replace("\r", "").Split('\n');

            int maxLength = Math.Max(snapshotLines.Length, currentLines.Length);
            int minLength = Math.Min(snapshotLines.Length, currentLines.Length);

            string prevSection = null;
            string curSection = null;

            int offset = 0;
            for (int i = 0; i < maxLength; ++i)
            {
                if (i >= maxLength)
                    break;
                else if (i >= minLength || i + offset >= currentLines.Length)
                {
                    if (currentLines.Length - snapshotLines.Length - offset > 0)
                    {
                        // A line was added at the end of the file.
                        yield return new DiffInstance(
                            currentLines[i + offset], prevSection.Substring(1, prevSection.Length - 2),
                            DiffType.Added, new List<string>(), aSnapshot.creationTime);
                    }

                    if (snapshotLines.Length - currentLines.Length > 0)
                    {
                        // A line was removed from the end of the file.
                        yield return new DiffInstance(
                            snapshotLines[i], prevSection.Substring(1, prevSection.Length - 2),
                            DiffType.Removed, new List<string>(), aSnapshot.creationTime);
                    }
                }
                else
                {
                    if (currentLines[i + offset].StartsWith("[") && currentLines[i + offset].EndsWith("]"))
                        curSection = currentLines[i + offset];

                    if (snapshotLines[i].StartsWith("[") && snapshotLines[i].EndsWith("]"))
                        prevSection = snapshotLines[i];

                    if (snapshotLines[i] != currentLines[i + offset])
                    {
                        int originalOffset = offset;
                        for (; offset < minLength - i; ++offset)
                            if (snapshotLines[i] == currentLines[i + offset])
                                break;

                        if (offset >= minLength - i)
                        {
                            // A line was removed.
                            offset = originalOffset;
                            --offset;

                            yield return new DiffInstance(
                                snapshotLines[i], prevSection.Substring(1, prevSection.Length - 2),
                                DiffType.Removed, new List<string>(), aSnapshot.creationTime);
                        }
                        else
                            // A line was added.
                            for (int j = originalOffset; j < offset; j++)
                                yield return new DiffInstance(
                                    currentLines[i + j], prevSection.Substring(1, prevSection.Length - 2),
                                    DiffType.Added, new List<string>(), aSnapshot.creationTime);
                    }
                }
            }
        }
        
        public static IEnumerable<DiffInstance> TranslateComparison(IEnumerable<DiffInstance> aDiffs)
        {
            foreach (string section in aDiffs.Select(aDiff => aDiff.section).Distinct())
            {

                IEnumerable<DiffInstance> diffs =
                    aDiffs.Where(aDiff =>
                        aDiff.section == section &&
                        aDiff.difference.Length > 0);

                DiffTranslator translator = TranslatorRegistry.GetTranslators().FirstOrDefault(aTranslator => aTranslator.Section == section);
                if (translator != null)
                {
                    foreach (DiffInstance diff in translator.Translate(diffs))
                    {
                        // Since all translators should be able to translate sections, we do that here.
                        diff.section = translator.TranslatedSection;
                        yield return diff;
                    }
                }
                else
                    foreach (DiffInstance diff in diffs)
                        yield return diff;
            }
        }

        public struct Setting
        {
            public readonly string key;
            public readonly string value;

            public Setting(string aCode)
            {
                if (aCode.IndexOf(":") != -1)
                {
                    key = aCode.Substring(0, aCode.IndexOf(":")).Trim();
                    value = aCode.Substring(aCode.IndexOf(":") + ":".Length).Trim();
                }
                else
                {
                    key = "A line";
                    value = aCode;
                }
            }
        }

        private static DiffInstance GetTranslatedSettingDiff(
            string aSectionName, Func<string, string> aTranslateFunc,
            Setting aSetting, DiffInstance aDiff,
            Setting? anOtherSetting = null, DiffInstance anOtherDiff = null)
        {
            string key = aTranslateFunc != null ? aTranslateFunc(aSetting.key) : aSetting.key;
            if (anOtherSetting == null || anOtherDiff == null)
            {
                if (aDiff.diffType == DiffType.Added)
                    return new DiffInstance(key + " was added and set to " + aSetting.value + ".",
                        aSectionName, DiffType.Added, new List<string>(), aDiff.snapshotCreationDate);
                else
                    return new DiffInstance(key + " was removed and is no longer set to " + aSetting.value + ".",
                        aSectionName, DiffType.Removed, new List<string>(), aDiff.snapshotCreationDate);
            }
            else
            {
                return new DiffInstance(key + " was changed from " + anOtherSetting.GetValueOrDefault().value + " to " + aSetting.value + ".",
                    aSectionName, DiffType.Changed, new List<string>(), aDiff.snapshotCreationDate);
            }
        }

        public static IEnumerable<DiffInstance> TranslateSettings(string aSectionName, IEnumerable<DiffInstance> aDiffs, Func<string, string> aTranslateFunc)
        {
            List<DiffInstance> added = aDiffs.Where(aDiff => aDiff.diffType == DiffType.Added).ToList();
            List<DiffInstance> removed = aDiffs.Where(aDiff => aDiff.diffType == DiffType.Removed).ToList();

            foreach (DiffInstance addition in added)
            {
                Setting setting = new Setting(addition.difference);
                DiffInstance removal = removed.FirstOrDefault(aDiff => new Setting(aDiff.difference).key == setting.key);

                if (removal != null && removal.difference != null)
                {
                    Setting removedSetting = new Setting(removal.difference);

                    removed.Remove(removal);

                    if (removedSetting.key == "Bookmarks")
                    {
                        IEnumerable<double> prevBookmarks =
                            removedSetting.value.Split(',').Select(aValue => double.Parse(aValue.Trim()));
                        IEnumerable<double> curBookmarks =
                            setting.value.Split(',').Select(aValue => double.Parse(aValue.Trim()));

                        IEnumerable<double> removedBookmarks = prevBookmarks.Except(curBookmarks);
                        IEnumerable<double> addedBookmarks = curBookmarks.Except(prevBookmarks);

                        List<string> details = new List<string>();
                        if (addedBookmarks.Count() > 0)
                            details.Add("Added " + String.Join(", ", addedBookmarks.Select(aMark => Timestamp.Get(aMark))));
                        if (removedBookmarks.Count() > 0)
                            details.Add("Removed " + String.Join(", ", removedBookmarks.Select(aMark => Timestamp.Get(aMark))));

                        yield return new DiffInstance(
                            aTranslateFunc(setting.key) + " were changed.", aSectionName,
                            DiffType.Changed, details, addition.snapshotCreationDate);
                    }
                    else if (removedSetting.key == "Tags")
                    {
                        IEnumerable<string> prevTags = removedSetting.value.Split(' ').Select(aValue => aValue);
                        IEnumerable<string> curTags = setting.value.Split(' ').Select(aValue => aValue);

                        IEnumerable<string> removedTags = prevTags.Except(curTags);
                        IEnumerable<string> addedTags = curTags.Except(prevTags);

                        List<string> details = new List<string>();
                        if (addedTags.Count() > 0)
                            details.Add("Added " + String.Join(", ", addedTags.Select(aMark => Timestamp.Get(aMark))));
                        if (removedTags.Count() > 0)
                            details.Add("Removed " + String.Join(", ", removedTags.Select(aMark => Timestamp.Get(aMark))));

                        yield return new DiffInstance(
                            aTranslateFunc(setting.key) + " were changed.", aSectionName,
                            DiffType.Changed, details, addition.snapshotCreationDate);
                    }
                    else
                        yield return GetTranslatedSettingDiff(
                            aSectionName, aTranslateFunc,
                            setting, addition,
                            removedSetting, removal);
                }
                else
                {
                    yield return GetTranslatedSettingDiff(
                        aSectionName, aTranslateFunc, setting, addition);
                }
            }

            foreach (DiffInstance removal in removed)
            {
                Setting setting = new Setting(removal.difference);

                yield return GetTranslatedSettingDiff(
                    aSectionName, aTranslateFunc, setting, removal);
            }
        }
    }
}
