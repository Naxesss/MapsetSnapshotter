using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class FilesTranslator : DiffTranslator
    {
        public override string Section { get => "Files"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
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

                    yield return new DiffInstance("\"" + setting.key + "\" was modified.",
                        Section, DiffType.Changed, new List<string>(), addition.snapshotCreationDate);
                }
                else
                {
                    yield return new DiffInstance("\"" + setting.key + "\" was added.",
                        Section, DiffType.Added, new List<string>(), addition.snapshotCreationDate);
                }
            }

            foreach (DiffInstance removal in removed)
            {
                Setting setting = new Setting(removal.difference);

                yield return new DiffInstance("\"" + setting.key + "\" was removed.",
                    Section, DiffType.Removed, new List<string>(), removal.snapshotCreationDate);
            }
        }
    }
}
