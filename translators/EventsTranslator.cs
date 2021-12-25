using MapsetParser.objects.events;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class EventsTranslator : DiffTranslator
    {
        public override string Section => "Events";

        /*  First argument
            0 : Background
            1 : Video
            2 : Break
            3 : (background color transformation)
            4 : Sprite
            5 : Sample
            6 : Animation
        */

        private static List<KeyValuePair<int, DiffInstance>> mDictionary;

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            mDictionary = new List<KeyValuePair<int, DiffInstance>>();

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
                    .Select(aPair => new Tuple<Break, DiffInstance>(new Break(aPair.Value.difference.Split(',')), aPair.Value)).ToList();

            List<Tuple<Break, DiffInstance>> removedBreaks =
                mDictionary.Where(aPair => aPair.Key == 2 && aPair.Value.diffType == DiffType.Removed)
                    .Select(aPair => new Tuple<Break, DiffInstance>(new Break(aPair.Value.difference.Split(',')), aPair.Value)).ToList();

            mDictionary.RemoveAll(aPair => aPair.Key == 2);

            foreach (var (@break, diffInstance) in addedBreaks)
            {
                Tuple<Break, DiffInstance> removedStart = removedBreaks.FirstOrDefault(aTuple => aTuple.Item1.time.AlmostEqual(@break.time));

                string startStamp = Timestamp.Get(@break.time);
                string endStamp = Timestamp.Get(@break.endTime);
                if (removedStart != null)
                {
                    string oldStartStamp = Timestamp.Get(removedStart.Item1.time);
                    string oldEndStamp = Timestamp.Get(removedStart.Item1.endTime);
                    yield return new DiffInstance(
                        "Break from " + oldStartStamp + " to " + oldEndStamp +
                        " now ends at " + endStamp + " instead.",
                        Section, DiffType.Changed, new List<string>(), diffInstance.snapshotCreationDate);

                    removedBreaks.Remove(removedStart);
                }
                else
                {
                    Tuple<Break, DiffInstance> removedEnd = removedBreaks.FirstOrDefault(aTuple => aTuple.Item1.endTime.AlmostEqual(@break.endTime));

                    if (removedEnd != null)
                    {
                        string oldStartStamp = Timestamp.Get(removedEnd.Item1.time);
                        string oldEndStamp = Timestamp.Get(removedEnd.Item1.endTime);
                        yield return new DiffInstance(
                            "Break from " + oldStartStamp + " to " + oldEndStamp +
                            " now starts at " + startStamp + " instead.",
                            Section, DiffType.Changed, new List<string>(), diffInstance.snapshotCreationDate);

                        removedBreaks.Remove(removedEnd);
                    }
                    else
                        yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " added.",
                            Section, DiffType.Added, new List<string>(), diffInstance.snapshotCreationDate);
                }
            }

            foreach (var (@break, diffInstance) in removedBreaks)
            {
                string startStamp = Timestamp.Get(@break.time);
                string endStamp = Timestamp.Get(@break.endTime);

                yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " removed.",
                        Section, DiffType.Removed, new List<string>(), diffInstance.snapshotCreationDate);
            }
        }
    }
}
