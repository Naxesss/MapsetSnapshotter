using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class TimingTranslator : DiffTranslator
    {
        public override string Section => "TimingPoints";
        public override string TranslatedSection => "Timing";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            List<Tuple<DiffInstance, TimingLine>> addedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();
            List<Tuple<DiffInstance, TimingLine>> removedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();

            foreach (DiffInstance diff in aDiffs)
            {
                TimingLine timingLine = null;
                try
                {
                    timingLine = new TimingLine(diff.difference.Split(','), beatmap: null);
                }
                catch
                {
                    // Failing to parse a changed line shouldn't stop it from showing.
                }

                if (timingLine != null)
                {
                    if (diff.diffType == DiffType.Added)
                        addedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                    else
                        removedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                }
                else
                    // Shows the raw .osu line change.
                    yield return diff;
            }

            foreach (Tuple<DiffInstance, TimingLine> addedTuple in addedTimingLines)
            {
                DiffInstance addedDiff = addedTuple.Item1;
                TimingLine addedLine = addedTuple.Item2;

                string stamp = Timestamp.Get(addedLine.offset);
                string type = addedLine.uninherited ? "Uninherited line" : "Inherited line";

                bool found = false;
                foreach (TimingLine removedLine in removedTimingLines.Select(aTuple => aTuple.Item2).ToList())
                {
                    if (!addedLine.offset.AlmostEqual(removedLine.offset))
                        continue;
                    
                    string removedType = removedLine.uninherited ? "Uninherited line" : "Inherited line";
                    if (type != removedType)
                        continue;
                    
                    List<string> changes = new List<string>();

                    if (addedLine.kiai != removedLine.kiai)
                        changes.Add("Kiai changed from " + (removedLine.kiai ? "enabled" : "disabled") +
                                    " to " + (addedLine.kiai ? "enabled" : "disabled") + ".");

                    if (addedLine.meter != removedLine.meter)
                        changes.Add("Timing signature changed from " + removedLine.meter + "/4" +
                                    " to " + addedLine.meter + "/4.");

                    if (addedLine.sampleset != removedLine.sampleset)
                        changes.Add("Sampleset changed from " +
                                    removedLine.sampleset.ToString().ToLower() + " to " +
                                    addedLine.sampleset.ToString().ToLower() + ".");
                            
                    if (addedLine.customIndex != removedLine.customIndex)
                        changes.Add("Custom sampleset index changed from " +
                                    removedLine.customIndex.ToString().ToLower() + " to " +
                                    addedLine.customIndex.ToString().ToLower() + ".");

                    if (addedLine.volume.AlmostEqual(removedLine.volume))
                        changes.Add("Volume changed from " + removedLine.volume +
                                    " to " + addedLine.volume + ".");

                    if (type == "Uninherited line")
                    {
                        UninheritedLine addedUninherited = new UninheritedLine(addedLine.code.Split(','), beatmap: null);
                        UninheritedLine removedUninherited = new UninheritedLine(removedLine.code.Split(','), beatmap: null);

                        if (addedUninherited.bpm.AlmostEqual(removedUninherited.bpm))
                            changes.Add("BPM changed from " + removedUninherited.bpm +
                                        " to " + addedUninherited.bpm + ".");
                    }
                    else if (addedLine.svMult.AlmostEqual(removedLine.svMult))
                        changes.Add("Slider velocity multiplier changed from " + removedLine.svMult +
                                    " to " + addedLine.svMult + ".");

                    if (changes.Count == 1)
                        yield return new DiffInstance(stamp + changes[0],
                            Section, DiffType.Changed, new List<string>(), addedDiff.snapshotCreationDate);
                    else if (changes.Count > 1)
                        yield return new DiffInstance(stamp + type + " changed.",
                            Section, DiffType.Changed, changes, addedDiff.snapshotCreationDate);

                    found = true;
                    removedTimingLines.RemoveAll(aTuple => aTuple.Item2.code == removedLine.code);
                }

                if (!found)
                    yield return new DiffInstance(stamp + type + " added.",
                        Section, DiffType.Added, new List<string>(), addedDiff.snapshotCreationDate);
            }

            foreach (Tuple<DiffInstance, TimingLine> removedTuple in removedTimingLines)
            {
                DiffInstance removedDiff = removedTuple.Item1;
                TimingLine removedLine = removedTuple.Item2;

                string stamp = Timestamp.Get(removedLine.offset);
                string type = removedLine.uninherited ? "Uninherited line" : "Inherited line";

                yield return new DiffInstance(stamp + type + " removed.",
                    Section, DiffType.Removed, new List<string>(), removedDiff.snapshotCreationDate);
            }
        }
    }
}
