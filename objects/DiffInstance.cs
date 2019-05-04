using System;
using System.Collections.Generic;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter
{
    public class DiffInstance
    {
        public string mDifference;
        public string mSection;
        public DiffType mDiffType;
        public string mCustomTitle;

        public List<string> mDetails;
        public string mIcon;

        public DateTime mSnapshotCreationDate;

        public DiffInstance(string aDifference, string aSection, DiffType aDiffType, List<string> aDetails, DateTime aSnapshotCreationDate)
        {
            mDifference = aDifference;
            mSection = aSection;
            mDiffType = aDiffType;
            mCustomTitle = null;

            mDetails = aDetails;
            mIcon = aDiffType == DiffType.Added ? "plus" :
                    aDiffType == DiffType.Removed ? "minus"
                                                    : "gear-blue";

            mSnapshotCreationDate = aSnapshotCreationDate;
        }
    }
}
