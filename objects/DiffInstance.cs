using System;
using System.Collections.Generic;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.objects
{
    public class DiffInstance
    {
        public string                Section { get; set; }
        public readonly string       difference;
        public readonly DiffType     diffType;
        public readonly List<string> details;

        public readonly DateTime snapshotCreationDate;

        public DiffInstance(string aDifference, string aSection, DiffType aDiffType, List<string> aDetails, DateTime aSnapshotCreationDate)
        {
            Section = aSection;
            difference = aDifference;
            diffType = aDiffType;
            details = aDetails;

            snapshotCreationDate = aSnapshotCreationDate;
        }
    }
}
