using MapsetParser.objects.events;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class EventsTranslator : DiffTranslator
    {
        public override string Section { get => "Events"; }

        /*  First argument
            0 : Background
            1 : Video
            2 : Break
            3 : (background color transformation)
            4 : Sprite
            5 : Sample
            6 : Animation
        */

        private static List<KeyValuePair<int, DiffInstance>> mDictionary = new List<KeyValuePair<int, DiffInstance>>();

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            // Assumes all events begin with an id of their type, see block comment above.
            foreach (DiffInstance diff in aDiffs)
                mDictionary.Add(new KeyValuePair<int, DiffInstance>(diff.difference[0] - 48, diff));

            // Handles all events with id 2 (i.e. breaks).
            foreach (DiffInstance diff in GetBreakTranslation())
                yield return diff;

            // Handles all other events.
            foreach (DiffInstance diff in mDictionary.Select(aPair => aPair.Value))
                yield return diff;
        }

        private IEnumerable<DiffInstance> GetBreakTranslation()
        {
            List<Tuple<Break, DiffInstance>> addedBreaks =
                mDictionary.Where(aPair => aPair.Key == 2 && aPair.Value.diffType == DiffType.Added)
                    .Select(aPair => new Tuple<Break, DiffInstance>(new Break(aPair.Value.difference), aPair.Value)).ToList();

            List<Tuple<Break, DiffInstance>> removedBreaks =
                mDictionary.Where(aPair => aPair.Key == 2 && aPair.Value.diffType == DiffType.Removed)
                    .Select(aPair => new Tuple<Break, DiffInstance>(new Break(aPair.Value.difference), aPair.Value)).ToList();

            mDictionary.RemoveAll(aPair => aPair.Key == 2);

            foreach (Tuple<Break, DiffInstance> added in addedBreaks)
            {
                Tuple<Break, DiffInstance> removedStart = removedBreaks.FirstOrDefault(aTuple => aTuple.Item1.time == added.Item1.time);

                string startStamp = Timestamp.Get(added.Item1.time);
                string endStamp = Timestamp.Get(added.Item1.endTime);
                if (removedStart != null)
                {
                    string newEndStamp = Timestamp.Get(removedStart.Item1.endTime);
                    yield return new DiffInstance(
                        "Break from " + startStamp + " to " + endStamp +
                        " now ends at " + newEndStamp + " instead.",
                        Section, DiffType.Changed, new List<string>(), added.Item2.snapshotCreationDate);

                    removedBreaks.Remove(removedStart);
                }
                else
                {
                    Tuple<Break, DiffInstance> removedEnd = removedBreaks.FirstOrDefault(aTuple => aTuple.Item1.endTime == added.Item1.endTime);

                    if (removedEnd != null)
                    {
                        string newStamp = Timestamp.Get(removedEnd.Item1.time);
                        yield return new DiffInstance(
                            "Break from " + startStamp + " to " + endStamp +
                            " now starts at " + newStamp + " instead.",
                            Section, DiffType.Changed, new List<string>(), added.Item2.snapshotCreationDate);

                        removedBreaks.Remove(removedEnd);
                    }
                    else
                        yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " added.",
                            Section, DiffType.Added, new List<string>(), added.Item2.snapshotCreationDate);
                }
            }

            foreach (Tuple<Break, DiffInstance> removed in removedBreaks)
            {
                string startStamp = Timestamp.Get(removed.Item1.time);
                string endStamp = Timestamp.Get(removed.Item1.endTime);

                yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " removed.",
                        Section, DiffType.Removed, new List<string>(), removed.Item2.snapshotCreationDate);
            }
        }
    }
}
