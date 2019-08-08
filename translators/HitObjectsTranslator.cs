using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetSnapshotter.objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MapsetSnapshotter.Snapshotter;

namespace MapsetSnapshotter.translators
{
    public class HitObjectsTranslator : DiffTranslator
    {
        public override string Section { get => "HitObjects"; }
        public override string TranslatedSection { get => "Hit Objects"; }

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> aDiffs)
        {
            List<Tuple<DiffInstance, HitObject>> addedHitObjects = new List<Tuple<DiffInstance, HitObject>>();
            List<Tuple<DiffInstance, HitObject>> removedHitObjects = new List<Tuple<DiffInstance, HitObject>>();

            foreach (DiffInstance diff in aDiffs)
            {
                HitObject hitObject = null;
                try
                { hitObject = new HitObject(diff.difference.Split(','), null); }
                catch
                {
                    // Cannot yield in a catch clause, so checks for null in the following statement instead.
                }

                if (hitObject != null)
                {
                    if (diff.diffType == DiffType.Added)
                        addedHitObjects.Add(new Tuple<DiffInstance, HitObject>(diff, hitObject));
                    else
                        removedHitObjects.Add(new Tuple<DiffInstance, HitObject>(diff, hitObject));
                }
                else
                    // Failing to parse a changed line shouldn't stop it from showing.
                    yield return diff;
            }

            foreach (Tuple<DiffInstance, HitObject> addedTuple in addedHitObjects)
            {
                DiffInstance addedDiff = addedTuple.Item1;
                HitObject addedObject = addedTuple.Item2;

                string stamp = Timestamp.Get(addedObject.time);
                string type = addedObject.GetObjectType();

                bool found = false;
                foreach (HitObject removedObject in removedHitObjects.Select(aTuple => aTuple.Item2).ToList())
                {
                    if (addedObject.time == removedObject.time)
                    {
                        string removedType = removedObject.GetObjectType();

                        if (type == removedType)
                        {
                            List<string> changes = new List<string>();

                            if (addedObject.Position != removedObject.Position)
                                changes.Add("Moved from " + removedObject.Position.X + "; " + removedObject.Position.Y +
                                    " to " + addedObject.Position.X + "; " + addedObject.Position.Y + ".");

                            if (addedObject.hitSound != removedObject.hitSound)
                            {
                                foreach (HitObject.HitSound hitSound in Enum.GetValues(typeof(HitObject.HitSound)))
                                {
                                    if (addedObject.HasHitSound(hitSound) && !removedObject.HasHitSound(hitSound))
                                        changes.Add("Added " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() + ".");

                                    if (!addedObject.HasHitSound(hitSound) && removedObject.HasHitSound(hitSound))
                                        changes.Add("Removed " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() + ".");
                                }
                            }

                            if (addedObject.sampleset != removedObject.sampleset)
                                changes.Add("Sampleset changed from " +
                                    removedObject.sampleset.ToString().ToLower() + " to " +
                                    addedObject.sampleset.ToString().ToLower() + ".");

                            if (addedObject.addition != removedObject.addition)
                                changes.Add("Addition changed from " +
                                    removedObject.addition.ToString().ToLower() + " to " +
                                    addedObject.addition.ToString().ToLower() + ".");

                            if ((addedObject.customIndex ?? 0) != (removedObject.customIndex ?? 0))
                                changes.Add("Custom sampleset index changed from " +
                                    (removedObject.customIndex?.ToString() ?? "default") + " to " +
                                    (addedObject.customIndex?.ToString() ?? "default") + ".");

                            if (addedObject.type.HasFlag(HitObject.Type.NewCombo) && !removedObject.type.HasFlag(HitObject.Type.NewCombo))
                                changes.Add("Added new combo.");

                            if (!addedObject.type.HasFlag(HitObject.Type.NewCombo) && removedObject.type.HasFlag(HitObject.Type.NewCombo))
                                changes.Add("Removed new combo.");

                            int addedComboSkip = 0;
                            if (addedObject.type.HasFlag(HitObject.Type.ComboSkip1))
                                addedComboSkip += 1;
                            if (addedObject.type.HasFlag(HitObject.Type.ComboSkip2))
                                addedComboSkip += 2;
                            if (addedObject.type.HasFlag(HitObject.Type.ComboSkip3))
                                addedComboSkip += 4;

                            int removedComboSkip = 0;
                            if (removedObject.type.HasFlag(HitObject.Type.ComboSkip1))
                                removedComboSkip += 1;
                            if (removedObject.type.HasFlag(HitObject.Type.ComboSkip2))
                                removedComboSkip += 2;
                            if (removedObject.type.HasFlag(HitObject.Type.ComboSkip3))
                                removedComboSkip += 4;

                            if (addedComboSkip != removedComboSkip)
                                changes.Add("Changed skipped combo amount from " + removedComboSkip + " to " +
                                    addedComboSkip + ".");

                            if (addedObject.filename != removedObject.filename)
                                changes.Add("Hit sound filename changed from " + removedObject.filename + " to " +
                                        addedObject.filename + ".");

                            if (addedObject.volume != removedObject.volume)
                                changes.Add("Hit sound volume changed from " + (removedObject.volume?.ToString() ?? "inherited") + " to " +
                                        (addedObject.volume?.ToString() ?? "inherited") + ".");

                            if (type == "Slider")
                            {
                                Slider addedSlider = new Slider(addedObject.code.Split(','), null);
                                Slider removedSlider = new Slider(removedObject.code.Split(','), null);

                                if (addedSlider.curveType != removedSlider.curveType)
                                    changes.Add("Curve type changed from " + removedSlider.curveType + " to " +
                                        addedSlider.curveType + ".");

                                if (addedSlider.edgeAmount != removedSlider.edgeAmount)
                                    changes.Add("Reverse amount changed from " + (removedSlider.edgeAmount - 1) + " to " +
                                         (addedSlider.edgeAmount - 1) + ".");

                                if (addedSlider.endSampleset != removedSlider.endSampleset)
                                    changes.Add("Tail sampleset changed from " +
                                        removedSlider.endSampleset.ToString().ToLower() + " to " +
                                        addedSlider.endSampleset.ToString().ToLower() + ".");

                                if (addedSlider.endAddition != removedSlider.endAddition)
                                    changes.Add("Tail addition changed from " +
                                        removedSlider.endAddition.ToString().ToLower() + " to " +
                                        addedSlider.endAddition.ToString().ToLower() + ".");

                                if (addedSlider.endHitSound != removedSlider.endHitSound)
                                {
                                    foreach (HitObject.HitSound hitSound in Enum.GetValues(typeof(HitObject.HitSound)))
                                    {
                                        if (addedSlider.HasHitSound(hitSound) && !removedSlider.HasHitSound(hitSound))
                                            changes.Add("Added " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                " to tail.");

                                        if (!addedSlider.HasHitSound(hitSound) && removedSlider.HasHitSound(hitSound))
                                            changes.Add("Removed " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                " from tail.");
                                    }
                                }

                                if (addedSlider.pixelLength != removedSlider.pixelLength)
                                    changes.Add("Pixel length changed from " + removedSlider.pixelLength +
                                        " to " + addedSlider.pixelLength + ".");

                                if (addedSlider.nodePositions.Count == removedSlider.nodePositions.Count)
                                {
                                    // The first node is the start, which we already checked.
                                    for (int i = 1; i < addedSlider.nodePositions.Count; ++i)
                                    {
                                        if (addedSlider.nodePositions[i] != removedSlider.nodePositions[i])
                                            changes.Add("Node " + (i + 1) + " moved from " +
                                                removedSlider.nodePositions[i].X + "; " + removedSlider.nodePositions[i].Y + " to " +
                                                addedSlider.nodePositions[i].X + "; " + addedSlider.nodePositions[i].Y + ".");
                                    }
                                }
                                else
                                    changes.Add("Node count changed from " + removedSlider.nodePositions.Count +
                                        " to " + addedSlider.nodePositions.Count + " (possibly positions as well).");

                                if (addedSlider.edgeAmount == removedSlider.edgeAmount)
                                {
                                    for (int i = 0; i < addedSlider.reverseSamplesets.Count; ++i)
                                    {
                                        if (addedSlider.reverseSamplesets[i] != removedSlider.reverseSamplesets[i])
                                            changes.Add("Reverse #" + (i + 1) + " sampleset changed from " +
                                                removedSlider.reverseSamplesets[i].ToString().ToLower() + " to " +
                                                addedSlider.reverseSamplesets[i].ToString().ToLower() + ".");
                                    }

                                    for (int i = 0; i < addedSlider.reverseAdditions.Count; ++i)
                                    {
                                        if (addedSlider.reverseAdditions[i] != removedSlider.reverseAdditions[i])
                                            changes.Add("Reverse #" + (i + 1) + " addition changed from " +
                                                removedSlider.reverseAdditions[i].ToString().ToLower() + " to " +
                                                addedSlider.reverseAdditions[i].ToString().ToLower() + ".");
                                    }

                                    for (int i = 0; i < addedSlider.reverseAdditions.Count; ++i)
                                    {
                                        if (addedSlider.reverseHitSounds[i] != removedSlider.reverseHitSounds[i])
                                        {
                                            foreach (HitObject.HitSound hitSound in Enum.GetValues(typeof(HitObject.HitSound)))
                                            {
                                                if (addedSlider.reverseHitSounds[i].HasFlag(hitSound) &&
                                                    !removedSlider.reverseHitSounds[i].HasFlag(hitSound))
                                                    changes.Add("Added " +
                                                        Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                        " to reverse #" + (i + 1) + ".");

                                                if (!addedSlider.reverseHitSounds[i].HasFlag(hitSound) &&
                                                    removedSlider.reverseHitSounds[i].HasFlag(hitSound))
                                                    changes.Add("Removed " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                        " from reverse #" + (i + 1) + ".");
                                            }
                                        }
                                    }
                                }

                                if (addedSlider.startHitSound != removedSlider.startHitSound)
                                {
                                    foreach (HitObject.HitSound hitSound in Enum.GetValues(typeof(HitObject.HitSound)))
                                    {
                                        if (addedSlider.startHitSound.HasFlag(hitSound) &&
                                            !removedSlider.startHitSound.HasFlag(hitSound))
                                            changes.Add("Added " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                " to head.");

                                        if (!addedSlider.startHitSound.HasFlag(hitSound) &&
                                            removedSlider.startHitSound.HasFlag(hitSound))
                                            changes.Add("Removed " + Enum.GetName(typeof(HitObject.HitSound), hitSound).ToLower() +
                                                " from head.");
                                    }
                                }

                                if (addedSlider.startSampleset != removedSlider.startSampleset)
                                    changes.Add("Head sampleset changed from " +
                                        removedSlider.startSampleset.ToString().ToLower() + " to " +
                                        addedSlider.startSampleset.ToString().ToLower() + ".");

                                if (addedSlider.startAddition != removedSlider.startAddition)
                                    changes.Add("Head addition changed from " +
                                        removedSlider.startAddition.ToString().ToLower() + " to " +
                                        addedSlider.startAddition.ToString().ToLower() + ".");
                            }

                            if (type == "Spinner")
                            {
                                Spinner addedSpinner = new Spinner(addedObject.code.Split(','), null);
                                Spinner removedSpinner = new Spinner(removedObject.code.Split(','), null);

                                if (addedSpinner.endTime != removedSpinner.endTime)
                                    changes.Add("End time changed from " + removedSpinner.endTime + " to " +
                                        addedSpinner.endTime + ".");
                            }

                            if (type == "Hold note")
                            {
                                HoldNote addedNote = new HoldNote(addedObject.code.Split(','), null);
                                HoldNote removedNote = new HoldNote(removedObject.code.Split(','), null);

                                if (addedNote.endTime != removedNote.endTime)
                                    changes.Add("End time changed from " + removedNote.endTime + " to " +
                                        addedNote.endTime + ".");
                            }

                            if (changes.Count == 1)
                                yield return new DiffInstance(stamp + changes[0],
                                    Section, DiffType.Changed, new List<string>(), addedDiff.snapshotCreationDate);
                            else if (changes.Count > 1)
                                yield return new DiffInstance(stamp + type + " changed.",
                                    Section, DiffType.Changed, changes, addedDiff.snapshotCreationDate);

                            found = true;
                            removedHitObjects.RemoveAll(aTuple => aTuple.Item2.code == removedObject.code);
                        }
                    }
                }

                if (!found)
                    yield return new DiffInstance(stamp + type + " added.",
                        Section, DiffType.Added, new List<string>(), addedDiff.snapshotCreationDate);
            }

            foreach (Tuple<DiffInstance, HitObject> removedTuple in removedHitObjects)
            {
                DiffInstance removedDiff = removedTuple.Item1;
                HitObject removedObject = removedTuple.Item2;

                string stamp = Timestamp.Get(removedObject.time);
                string type = removedObject.GetObjectType();

                yield return new DiffInstance(stamp + type + " removed.",
                    Section, DiffType.Removed, new List<string>(), removedDiff.snapshotCreationDate);
            }
        }
    }
}
