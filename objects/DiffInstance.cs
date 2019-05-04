using System;
using System.Collections.Generic;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.objects
{
    public class DiffInstance
    {
        public string difference;
        public string section;
        public DiffType diffType;
        public string customTitle;

        public List<string> details;

        public DateTime snapshotCreationDate;

        public DiffInstance(string aDifference, string aSection, DiffType aDiffType, List<string> aDetails, DateTime aSnapshotCreationDate)
        {
            difference = aDifference;
            section = aSection;
            diffType = aDiffType;
            customTitle = null;

            details = aDetails;

            snapshotCreationDate = aSnapshotCreationDate;
        }
    }
}
