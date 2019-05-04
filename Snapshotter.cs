using MapsetParser.objects;
using MapsetParser.statics;
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
        private static string mFileNameFormat = "yyyy-MM-dd HH-mm-ss";

        public enum DiffType
        {
            Added,
            Removed,
            Changed
        }

        public struct Snapshot
        {
            public DateTime mCreationTime;
            public string mBeatmapSetId;
            public string mBeatmapId;
            public string mSaveName;
            public string mCode;

            public Snapshot(DateTime aCreationTime, string aBeatmapSetId, string aBeatmapId, string aSaveName, string aCode)
            {
                mCreationTime = aCreationTime;
                mBeatmapSetId = aBeatmapSetId;
                mBeatmapId = aBeatmapId;
                mSaveName = aSaveName;
                mCode = aCode;
            }
        }

        public static void SnapshotBeatmapSet(BeatmapSet aBeatmapSet)
        {
            DateTime myCreationDate = DateTime.UtcNow;
            string myBeatmapSetId = null;

            foreach (Beatmap myBeatmap in aBeatmapSet.beatmaps)
            {
                myBeatmapSetId = myBeatmap.metadataSettings.beatmapSetId.ToString();
                string myBeatmapId = myBeatmap.metadataSettings.beatmapId.ToString();

                foreach (Beatmap myOtherBeatmap in aBeatmapSet.beatmaps)
                {
                    if (myOtherBeatmap.metadataSettings.beatmapId == myBeatmap.metadataSettings.beatmapId)
                    {
                        if (myBeatmap.mapPath != null && myOtherBeatmap.mapPath != null)
                        {
                            DateTime myDate = File.GetCreationTimeUtc(myBeatmap.mapPath);
                            DateTime myOtherDate = File.GetCreationTimeUtc(myOtherBeatmap.mapPath);

                            // since I don't save the name of the file in the snapshots
                            // having the same id would override the previous, even if the previous was newer
                            if (myDate < myOtherDate)
                                return;
                        }
                    }
                }

                // ./snapshots/571202/258378/2019-01-26 22-12-49
                string mySaveDirectory = "snapshots/" + myBeatmapSetId + "/" + myBeatmapId;
                string mySaveName = myCreationDate.ToString(mFileNameFormat) + ".osu";

                List<Snapshot> mySnapshots = GetSnapshots(myBeatmapSetId, myBeatmapId).ToList();
                bool myShouldSave = true;

                // duplicates would quickly take up a lot of memory
                foreach (Snapshot mySnapshot in mySnapshots)
                    if (mySnapshot.mCreationTime == mySnapshots.Max(aSnapshot => aSnapshot.mCreationTime))
                        if (mySnapshot.mCode == myBeatmap.code)
                            myShouldSave = false;

                if (myShouldSave)
                {
                    if (!Directory.Exists(mySaveDirectory))
                        Directory.CreateDirectory(mySaveDirectory);

                    File.WriteAllText(mySaveDirectory + "/" + mySaveName, myBeatmap.code);
                }
            }

            StringBuilder myFileSnapshot = new StringBuilder("[Files]\r\n");

            foreach (string myFilePath in aBeatmapSet.songFilePaths)
            {
                if (myBeatmapSetId == null)
                    break;

                if (!myFilePath.EndsWith(".osu") && !myFilePath.EndsWith(".osb"))
                {
                    string myFileName = myFilePath.Split('/', '\\').Last();

                    byte[] myBytes = File.ReadAllBytes(myFilePath);
                    byte[] myHashBytes = SHA1.Create().ComputeHash(myBytes);

                    StringBuilder myHash = new StringBuilder();
                    foreach (byte myHashByte in myHashBytes)
                        myHash.Append(myHashByte.ToString("X2"));

                    myFileSnapshot.Append(myFileName + ": " + myHash.ToString() + "\r\n");
                }
            }

            string myFileSnapshotString = myFileSnapshot.ToString();
            if (myFileSnapshotString.Length > 0)
            {
                string myFilesSnapshotDirectory = "snapshots/" + myBeatmapSetId + "/files";
                string myFilesSnapshotName = myFilesSnapshotDirectory + "/" + myCreationDate.ToString(mFileNameFormat) + ".txt";

                List<Snapshot> mySnapshots = GetSnapshots(myBeatmapSetId, "files").ToList();
                bool myShouldSave = true;

                foreach (Snapshot mySnapshot in mySnapshots)
                    if (mySnapshot.mCreationTime == mySnapshots.Max(aSnapshot => aSnapshot.mCreationTime))
                        if (mySnapshot.mCode == myFileSnapshotString)
                            myShouldSave = false;

                if (myShouldSave)
                {
                    if (!Directory.Exists(myFilesSnapshotDirectory))
                        Directory.CreateDirectory(myFilesSnapshotDirectory);

                    File.WriteAllText(myFilesSnapshotName, myFileSnapshotString);
                }
            }
        }

        public static IEnumerable<Snapshot> GetSnapshots(Beatmap aBeatmap)
        {
            IEnumerable<Snapshot> mySnapshots = GetSnapshots(
                aBeatmap.metadataSettings.beatmapSetId.ToString(),
                aBeatmap.metadataSettings.beatmapId.ToString());
            return mySnapshots;
        }

        public static IEnumerable<Snapshot> GetSnapshots(string aBeatmapSetId, string aBeatmapId)
        {
            string mySaveDirectory = "snapshots/" + aBeatmapSetId + "/" + aBeatmapId;

            if (Directory.Exists(mySaveDirectory))
            {
                string[] myFilePaths = Directory.GetFiles(mySaveDirectory);

                for (int i = 0; i < myFilePaths.Length; ++i)
                {
                    int myForwardSlash = myFilePaths[i].LastIndexOf("/");
                    int myBackSlash = myFilePaths[i].LastIndexOf("\\");

                    string mySaveName = myFilePaths[i].Substring(Math.Max(myForwardSlash, myBackSlash) + 1);
                    string myCode = File.ReadAllText(myFilePaths[i]);

                    DateTime myCreationTime = DateTime.ParseExact(mySaveName.Split('.')[0], mFileNameFormat, null);

                    yield return new Snapshot(myCreationTime, aBeatmapSetId, aBeatmapId, mySaveName, myCode);
                }
            }
        }

        public static IEnumerable<Snapshot> GetLatestSnapshots(DateTime aDate, BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap myBeatmap in aBeatmapSet.beatmaps)
            {
                IEnumerable<Snapshot> mySnapshots = GetSnapshots(myBeatmap).OrderByDescending(aSnapshot => aSnapshot.mCreationTime);

                yield return mySnapshots.LastOrDefault(aSnapshot => aSnapshot.mCreationTime <= aDate);
            }
        }

        public static IEnumerable<DiffInstance> Compare(Snapshot aSnapshot, string aCurrentCode)
        {
            List<DiffInstance> myDifferences = new List<DiffInstance>();

            string[] mySnapshotLines = aSnapshot.mCode.Replace("\r", "").Split('\n');
            string[] myCurrentLines = aCurrentCode.Replace("\r", "").Split('\n');

            int myMaxLength = Math.Max(mySnapshotLines.Length, myCurrentLines.Length);
            int myMinLength = Math.Min(mySnapshotLines.Length, myCurrentLines.Length);

            string myPrevSection = null;
            string myCurSection = null;

            int myOffset = 0;
            for (int i = 0; i < myMaxLength; ++i)
            {
                if (i >= myMinLength || i + myOffset >= myCurrentLines.Length)
                    break;
                else
                {
                    if (myCurrentLines[i + myOffset].StartsWith("[") && myCurrentLines[i + myOffset].EndsWith("]"))
                        myCurSection = myCurrentLines[i + myOffset];

                    if (mySnapshotLines[i].StartsWith("[") && mySnapshotLines[i].EndsWith("]"))
                        myPrevSection = mySnapshotLines[i];

                    if (mySnapshotLines[i] != myCurrentLines[i + myOffset])
                    {
                        int myOriginalOffset = myOffset;
                        for (; myOffset < myMinLength - i; ++myOffset)
                            if (mySnapshotLines[i] == myCurrentLines[i + myOffset])
                                break;

                        if (myOffset >= myMinLength - i)
                        {
                            // removed
                            myOffset = myOriginalOffset;
                            --myOffset;

                            yield return new DiffInstance(
                                mySnapshotLines[i], myPrevSection.Substring(1, myPrevSection.Length - 2),
                                DiffType.Removed, new List<string>(), aSnapshot.mCreationTime);
                        }
                        else
                            // added
                            for (int j = myOriginalOffset; j < myOffset; j++)
                                yield return new DiffInstance(
                                    myCurrentLines[i + j], myPrevSection.Substring(1, myPrevSection.Length - 2),
                                    DiffType.Added, new List<string>(), aSnapshot.mCreationTime);
                    }
                }
            }
        }

        private static IEnumerable<DiffTranslator> mTranslators = null;
        private static void InitTranslators()
        {
            if (mTranslators != null)
                return;

            mTranslators = TranslatorRegistry.GetTranslators();
        }

        public static IEnumerable<DiffInstance> TranslateComparison(IEnumerable<DiffInstance> aDiffs)
        {
            InitTranslators();
            foreach (string mySection in aDiffs.Select(aDiff => aDiff.mSection).Distinct())
            {
                IEnumerable<DiffInstance> myDiffs = aDiffs.Where(aDiff => aDiff.mSection == mySection && aDiff.mDifference.Length > 0);

                DiffTranslator myTranslator = mTranslators.FirstOrDefault(aTranslator => aTranslator.Section == mySection);
                if (myTranslator != null)
                    foreach (DiffInstance myDiff in myTranslator.Difference(myDiffs))
                        yield return myDiff;
                else
                    foreach (DiffInstance myDiff in myDiffs)
                        yield return myDiff;
            }
        }

        public struct Setting
        {
            public string mKey;
            public string mValue;

            public Setting(string aCode)
            {
                if (aCode.IndexOf(":") != -1)
                {
                    mKey = aCode.Substring(0, aCode.IndexOf(":")).Trim();
                    mValue = aCode.Substring(aCode.IndexOf(":") + ":".Length).Trim();
                }
                else
                {
                    mKey = "A line";
                    mValue = aCode;
                }
            }
        }

        private static DiffInstance GetTranslatedSettingDiff(
            string aSectionName, Func<string, string> aTranslateFunc,
            Setting aSetting, DiffInstance aDiff,
            Setting? anOtherSetting = null, DiffInstance anOtherDiff = null)
        {
            string myKey = aTranslateFunc != null ? aTranslateFunc(aSetting.mKey) : aSetting.mKey;
            if (anOtherSetting == null || anOtherDiff == null)
            {
                if (aDiff.mDiffType == DiffType.Added)
                    return new DiffInstance(myKey + " was added and set to " + aSetting.mValue + ".",
                        aSectionName, DiffType.Added, new List<string>(), aDiff.mSnapshotCreationDate);
                else
                    return new DiffInstance(myKey + " was removed and is no longer set to " + aSetting.mValue + ".",
                        aSectionName, DiffType.Removed, new List<string>(), aDiff.mSnapshotCreationDate);
            }
            else
            {
                return new DiffInstance(myKey + " was changed from " + anOtherSetting.GetValueOrDefault().mValue + " to " + aSetting.mValue + ".",
                    aSectionName, DiffType.Changed, new List<string>(), aDiff.mSnapshotCreationDate);
            }
        }

        public static IEnumerable<DiffInstance> TranslateSettings(string aSectionName, IEnumerable<DiffInstance> aDiffs, Func<string, string> aTranslateFunc)
        {
            List<DiffInstance> myAdded = aDiffs.Where(aDiff => aDiff.mDiffType == DiffType.Added).ToList();
            List<DiffInstance> myRemoved = aDiffs.Where(aDiff => aDiff.mDiffType == DiffType.Removed).ToList();

            foreach (DiffInstance myAddition in myAdded)
            {
                Setting mySetting = new Setting(myAddition.mDifference);
                DiffInstance myRemoval = myRemoved.FirstOrDefault(aDiff => new Setting(aDiff.mDifference).mKey == mySetting.mKey);

                if (myRemoval != null && myRemoval.mDifference != null)
                {
                    Setting myRemovedSetting = new Setting(myRemoval.mDifference);

                    myRemoved.Remove(myRemoval);

                    if (myRemovedSetting.mKey == "Bookmarks")
                    {
                        IEnumerable<double> myPrevBookmarks =
                            myRemovedSetting.mValue.Split(',').Select(aValue => double.Parse(aValue.Trim()));
                        IEnumerable<double> myCurBookmarks =
                            mySetting.mValue.Split(',').Select(aValue => double.Parse(aValue.Trim()));

                        IEnumerable<double> myRemovedBookmarks = myPrevBookmarks.Except(myCurBookmarks);
                        IEnumerable<double> myAddedBookmarks = myCurBookmarks.Except(myPrevBookmarks);

                        List<string> myDetails = new List<string>();
                        if (myAddedBookmarks.Count() > 0)
                            myDetails.Add("Added " + String.Join(", ", myAddedBookmarks.Select(aMark => Timestamp.Get(aMark))));
                        if (myRemovedBookmarks.Count() > 0)
                            myDetails.Add("Removed " + String.Join(", ", myRemovedBookmarks.Select(aMark => Timestamp.Get(aMark))));

                        yield return new DiffInstance(
                            aTranslateFunc(mySetting.mKey) + " were changed.", aSectionName,
                            DiffType.Changed, myDetails, myAddition.mSnapshotCreationDate);
                    }
                    else if (myRemovedSetting.mKey == "Tags")
                    {
                        IEnumerable<string> myPrevTags = myRemovedSetting.mValue.Split(' ').Select(aValue => aValue);
                        IEnumerable<string> myCurTags = mySetting.mValue.Split(' ').Select(aValue => aValue);

                        IEnumerable<string> myRemovedTags = myPrevTags.Except(myCurTags);
                        IEnumerable<string> myAddedTags = myCurTags.Except(myPrevTags);

                        List<string> myDetails = new List<string>();
                        if (myAddedTags.Count() > 0)
                            myDetails.Add("Added " + String.Join(", ", myAddedTags.Select(aMark => Timestamp.Get(aMark))));
                        if (myRemovedTags.Count() > 0)
                            myDetails.Add("Removed " + String.Join(", ", myRemovedTags.Select(aMark => Timestamp.Get(aMark))));

                        yield return new DiffInstance(
                            aTranslateFunc(mySetting.mKey) + " were changed.", aSectionName,
                            DiffType.Changed, myDetails, myAddition.mSnapshotCreationDate);
                    }
                    else
                        yield return Snapshotter.GetTranslatedSettingDiff(
                            aSectionName, aTranslateFunc,
                            mySetting, myAddition,
                            myRemovedSetting, myRemoval);
                }
                else
                {
                    yield return Snapshotter.GetTranslatedSettingDiff(
                        aSectionName, aTranslateFunc, mySetting, myAddition);
                }
            }

            foreach (DiffInstance myRemoval in myRemoved)
            {
                Setting mySetting = new Setting(myRemoval.mDifference);

                yield return Snapshotter.GetTranslatedSettingDiff(
                    aSectionName, aTranslateFunc, mySetting, myRemoval);
            }
        }
    }
}
