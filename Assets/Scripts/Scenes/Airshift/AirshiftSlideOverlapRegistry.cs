using System;
using System.Collections.Generic;
using MajdataPlay.Scenes.Game.Notes.Behaviours;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Visual-only pairing of two *entirely identical* AIRSHIFT L/R SLIDEs.
    /// A matching start key alone is never enough. The full follower path,
    /// arrow geometry, sprite, time and pace must agree. All three original
    /// sensor-path judging and note-score objects remain independent.
    /// </summary>
    internal static class AirshiftSlideOverlapRegistry
    {
        private const string MaterialResourcePath = "AirshiftOverlaps/SlideSplit";
        private static readonly int SplitSideId = Shader.PropertyToID("_SplitSide");
        private static readonly int SplitAxisLocalId = Shader.PropertyToID("_SplitAxisLocal");
        private static readonly int SplitCenterLocalId = Shader.PropertyToID("_SplitCenterLocal");
        private static readonly int DividerHalfWidthId = Shader.PropertyToID("_DividerHalfWidth");

        private static readonly Dictionary<SingleSlideBase, Entry> Registered = new();
        private static readonly Dictionary<GroupKey, List<Entry>> Groups = new();

        private readonly struct GroupKey : IEquatable<GroupKey>
        {
            private readonly object _session;
            private readonly int _start, _end;
            private readonly float _timing, _startTiming, _length;

            internal GroupKey(SingleSlideBase slide)
            {
                _session = slide.OverlapSession;
                _start = slide.StartPos;
                _end = slide.EndPos;
                _timing = slide.Timing;
                _startTiming = slide.StartTiming;
                _length = slide.Length;
            }

            public bool Equals(GroupKey other) =>
                ReferenceEquals(_session, other._session) &&
                _start == other._start && _end == other._end &&
                _timing == other._timing &&
                _startTiming == other._startTiming &&
                _length == other._length;

            public override bool Equals(object? obj) =>
                obj is GroupKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _session.GetHashCode();
                    hash = hash * 397 ^ _start;
                    hash = hash * 397 ^ _end;
                    hash = hash * 397 ^ _timing.GetHashCode();
                    hash = hash * 397 ^ _startTiming.GetHashCode();
                    return hash * 397 ^ _length.GetHashCode();
                }
            }
        }

        private sealed class Entry
        {
            internal readonly SingleSlideBase Slide;
            internal readonly GroupKey Key;
            internal readonly SpriteRenderer Star;
            internal readonly SpriteRenderer[] Bars;
            internal readonly Material? StarMaterial;
            internal readonly Material?[] BarMaterials;
            internal readonly MaterialPropertyBlock Properties = new();
            internal bool Split;

            internal Entry(SingleSlideBase slide)
            {
                Slide = slide;
                Key = new GroupKey(slide);
                Star = slide.OverlapStarRenderer;
                StarMaterial = Star.sharedMaterial;
                Bars = new SpriteRenderer[slide.OverlapBarCount];
                BarMaterials = new Material?[Bars.Length];
                for (var i = 0; i < Bars.Length; i++)
                {
                    Bars[i] = slide.GetOverlapBarRenderer(i);
                    BarMaterials[i] = Bars[i].sharedMaterial;
                }
            }
        }

        /// <summary>
        /// Screen-right of travel for a downwards-moving arrow is +X;
        /// screen-left is -X. The axis rotates continuously with the path.
        /// </summary>
        internal static Vector2 RightOfTravel(Vector2 movement)
        {
            if (movement.sqrMagnitude < 1e-8f)
                return Vector2.right;
            var forward = movement.normalized;
            return new Vector2(-forward.y, forward.x);
        }

        internal static void Register(SingleSlideBase slide)
        {
            Unregister(slide);
            if (!slide.OverlapCanSplit || slide.OverlapBarCount == 0 ||
                slide.OverlapStarRenderer == null ||
                slide.OverlapStarPositionCount < 2)
                return;

            var entry = new Entry(slide);
            Registered.Add(slide, entry);
            if (!Groups.TryGetValue(entry.Key, out var group))
            {
                group = new List<Entry>(2);
                Groups.Add(entry.Key, group);
            }
            group.Add(entry);
            UpdateGroup(group);
        }

        internal static void Unregister(SingleSlideBase slide)
        {
            if (!Registered.TryGetValue(slide, out var entry))
                return;
            Registered.Remove(slide);
            Restore(entry);
            var group = Groups[entry.Key];
            group.Remove(entry);
            if (group.Count == 0)
                Groups.Remove(entry.Key);
            else
                UpdateGroup(group);
        }

        /// <summary>Refreshes moving star's local split axis after the path rotates.</summary>
        internal static void RefreshStar(SingleSlideBase slide)
        {
            if (!Registered.TryGetValue(slide, out var entry) || !entry.Split)
                return;
            var forward = slide.GetOverlapStarDirection();
            ApplyRenderer(entry.Star, entry.Properties, slide.RequiredHand,
                RightOfTravel(forward));
        }

        /// <summary>
        /// Restores the split material if an arrow's normal material changed
        /// (for example while an effect is playing). Original alpha comes
        /// from the renderer vertex color, so fade needs no extra state.
        /// </summary>
        internal static void RefreshBars(SlideBase slide)
        {
            if (slide is not SingleSlideBase single ||
                !Registered.TryGetValue(single, out var entry) || !entry.Split)
                return;
            var material = Resources.Load<Material>(MaterialResourcePath);
            if (material == null)
                return;
            for (var i = 0; i < entry.Bars.Length; i++)
            {
                var bar = entry.Bars[i];
                if (bar.sharedMaterial != material)
                {
                    entry.BarMaterials[i] = bar.sharedMaterial;
                    bar.sharedMaterial = material;
                }
            }
        }

        private static void UpdateGroup(List<Entry> group)
        {
            foreach (var entry in group)
                Restore(entry);

            var material = Resources.Load<Material>(MaterialResourcePath);
            if (material == null)
                return;

            // More than two identical paths are ambiguous; do not arbitrarily
            // pair one left with one right while hiding a third source.
            var claimed = new bool[group.Count];
            for (var i = 0; i < group.Count; i++)
            {
                if (claimed[i])
                    continue;
                var count = 1;
                var partner = -1;
                for (var j = i + 1; j < group.Count; j++)
                {
                    if (!SameCompletePath(group[i].Slide, group[j].Slide))
                        continue;
                    count++;
                    partner = j;
                }
                if (count != 2 || partner < 0)
                    continue;

                // Reject if an earlier unclaimed source is also an identical
                // path, even when note creation order is reversed.
                for (var j = 0; j < i; j++)
                {
                    if (SameCompletePath(group[i].Slide, group[j].Slide))
                    {
                        count++;
                        break;
                    }
                }
                if (count != 2)
                    continue;

                var first = group[i];
                var second = group[partner];
                if (first.Slide.RequiredHand == second.Slide.RequiredHand)
                    continue;
                claimed[i] = claimed[partner] = true;
                Activate(first, material);
                Activate(second, material);
            }
        }

        /// <summary>
        /// Geometry and motion must match through the entire path, not only
        /// at start/end or wherever two star sprites happen to cross.
        /// </summary>
        internal static bool SameCompletePath(SingleSlideBase left, SingleSlideBase right)
        {
            if (left.StartPos != right.StartPos ||
                left.EndPos != right.EndPos ||
                left.Timing != right.Timing ||
                left.StartTiming != right.StartTiming ||
                left.Length != right.Length ||
                left.Speed != right.Speed ||
                left.OverlapStarPositionCount != right.OverlapStarPositionCount ||
                left.OverlapBarCount != right.OverlapBarCount ||
                left.OverlapStarRenderer.sprite != right.OverlapStarRenderer.sprite ||
                left.OverlapStarPositionCount < 2 ||
                left.OverlapBarCount == 0)
                return false;

            for (var i = 0; i < left.OverlapStarPositionCount; i++)
            {
                if ((left.GetOverlapStarPosition(i) -
                     right.GetOverlapStarPosition(i)).sqrMagnitude > 1e-6f ||
                    Quaternion.Angle(left.GetOverlapStarRotation(i),
                                     right.GetOverlapStarRotation(i)) > 0.25f)
                    return false;
            }

            for (var i = 0; i < left.OverlapBarCount; i++)
            {
                var a = left.GetOverlapBarRenderer(i);
                var b = right.GetOverlapBarRenderer(i);
                if (a.sprite != b.sprite ||
                    (a.bounds.center - b.bounds.center).sqrMagnitude > 1e-6f ||
                    Quaternion.Angle(a.transform.rotation,
                                     b.transform.rotation) > 0.25f ||
                    (a.transform.lossyScale -
                     b.transform.lossyScale).sqrMagnitude > 1e-6f)
                    return false;
            }
            return true;
        }

        private static void Activate(Entry entry, Material material)
        {
            var slide = entry.Slide;
            entry.Split = true;
            // Bind every segment to its own travel tangent; curving and
            // turning paths therefore split down their actual centerline.
            for (var i = 0; i < entry.Bars.Length; i++)
            {
                var bar = entry.Bars[i];
                bar.sharedMaterial = material;
                ApplyRenderer(bar, entry.Properties, slide.RequiredHand,
                    RightOfTravel(slide.GetOverlapArrowDirection(bar.bounds.center)));
            }
            entry.Star.sharedMaterial = material;
            RefreshStar(slide);
        }

        private static void ApplyRenderer(SpriteRenderer source,
            MaterialPropertyBlock properties, AirshiftRequiredHand hand,
            Vector2 rightWorld)
        {
            if (source == null || source.sprite == null)
                return;

            var localRight = source.transform.worldToLocalMatrix.MultiplyVector(
                new Vector3(rightWorld.x, rightWorld.y, 0f));
            if (localRight.sqrMagnitude < 1e-8f)
                localRight = Vector3.right;
            localRight.Normalize();

            source.GetPropertyBlock(properties);
            properties.SetFloat(SplitSideId,
                hand == AirshiftRequiredHand.Left ? -1f : 1f);
            properties.SetVector(SplitAxisLocalId,
                new Vector4(localRight.x, localRight.y, 0f, 0f));
            var center = source.sprite.bounds.center;
            properties.SetVector(SplitCenterLocalId,
                new Vector4(center.x, center.y, 0f, 0f));
            properties.SetFloat(DividerHalfWidthId,
                Mathf.Min(source.sprite.bounds.size.x,
                          source.sprite.bounds.size.y) * 0.025f);
            source.SetPropertyBlock(properties);
        }

        private static void Restore(Entry entry)
        {
            if (!entry.Split)
                return;
            entry.Star.sharedMaterial = entry.StarMaterial;
            for (var i = 0; i < entry.Bars.Length; i++)
                entry.Bars[i].sharedMaterial = entry.BarMaterials[i];
            entry.Split = false;
        }
    }
}
