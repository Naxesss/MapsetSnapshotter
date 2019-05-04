using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class TimingTranslator : DiffTranslator
    {
        public override string Section { get => "Timing"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            List<Tuple<DiffInstance, TimingLine>> addedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();
            List<Tuple<DiffInstance, TimingLine>> removedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();

            foreach (DiffInstance diff in aDiffs)
            {
                TimingLine timingLine = null;
                try
                { timingLine = new TimingLine(diff.difference); }
                catch { }

                if (timingLine != null)
                {
                    if (diff.diffType == DiffType.Added)
                        addedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                    else
                        removedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                }
                else
                    // Failing to parse a changed line shouldn't stop it from showing.
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
                    if (addedLine.offset == removedLine.offset)
                    {
                        string removedType = removedLine.uninherited ? "Uninherited line" : "Inherited line";

                        if (type == removedType)
                        {
                            List<string> changes = new List<string>();

                            if (addedLine.kiai != removedLine.kiai)
                                changes.Add("Kiai changed from " + (addedLine.kiai ? "enabled" : "disabled") +
                                    " to " + (removedLine.kiai ? "enabled" : "disabled") + ".");

                            if (addedLine.meter != removedLine.meter)
                                changes.Add("Timing signature changed from 4/" + addedLine.meter +
                                    " to 4/" + removedLine.meter + ".");

                            if (addedLine.sampleset != removedLine.sampleset)
                                changes.Add("Sampleset changed from " +
                                    addedLine.sampleset.ToString().ToLower() + " to " +
                                    removedLine.sampleset.ToString().ToLower() + ".");

                            if (addedLine.volume != removedLine.volume)
                                changes.Add("Volume changed from " + addedLine.volume +
                                   " to " + removedLine.volume + ".");

                            if (type == "Uninherited line")
                            {
                                UninheritedLine addedUninherited = new UninheritedLine(addedLine.code);
                                UninheritedLine removedUninherited = new UninheritedLine(removedLine.code);

                                if (addedUninherited.bpm != removedUninherited.bpm)
                                    changes.Add("BPM changed from " + addedUninherited.bpm +
                                        " to " + removedUninherited.bpm + ".");
                            }
                            else if (addedLine.svMult != removedLine.svMult)
                                changes.Add("Slider velocity multiplier changed from " + addedLine.svMult +
                                    " to " + removedLine.svMult + ".");

                            if (changes.Count == 1)
                                yield return new DiffInstance(stamp + changes[0],
                                    Section, DiffType.Changed, new List<string>(), addedDiff.snapshotCreationDate);
                            else if (changes.Count > 1)
                                yield return new DiffInstance(stamp + type + " changed.",
                                    Section, DiffType.Changed, changes, addedDiff.snapshotCreationDate);

                            found = true;
                            removedTimingLines.RemoveAll(aTuple => aTuple.Item2.code == removedLine.code);
                        }
                    }
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
