using System;
using System.Collections.Generic;
using MajdataPlay.Scenes.Game.Notes.Behaviours;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Visually pairs exactly two compatible, opposite-hand STAR bodies.
    /// Their note objects, input consumption, and scoring remain independent.
    /// </summary>
    internal static class AirshiftStarOverlapRegistry
    {
        private const string MaterialResourcePath = "AirshiftOverlaps/StarSplit";
        private static readonly int SplitSideId = Shader.PropertyToID("_SplitSide");
        private static readonly int SplitCenterId = Shader.PropertyToID("_SplitCenter");
        private static readonly int DividerHalfWidthId = Shader.PropertyToID("_DividerHalfWidth");

        private static readonly Dictionary<StarKey, List<StarEntry>> Groups = new();
        private static readonly Dictionary<TapDrop, StarKey> Registered = new();

        /// <summary>Identifies a visually identical STAR trajectory within one gameplay session.</summary>
        private readonly struct StarKey : IEquatable<StarKey>
        {
            private readonly object _session;
            private readonly Sprite _sprite;
            private readonly int _startPos;
            private readonly float _timing;
            private readonly float _speed;
            private readonly float _rotateSpeed;

            /// <summary>Creates a key from one active note and its renderer.</summary>
            internal StarKey(TapDrop note, SpriteRenderer renderer, object session)
            {
                _session = session;
                _sprite = renderer.sprite;
                _startPos = note.StartPos;
                _timing = note.Timing;
                _speed = note.Speed;
                _rotateSpeed = note.RotateSpeed;
            }

            /// <inheritdoc/>
            public bool Equals(StarKey other)
            {
                return ReferenceEquals(_session, other._session) &&
                       ReferenceEquals(_sprite, other._sprite) &&
                       _startPos == other._startPos &&
                       _timing == other._timing &&
                       _speed == other._speed &&
                       _rotateSpeed == other._rotateSpeed;
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                return obj is StarKey other && Equals(other);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _session.GetHashCode();
                    hash = (hash * 397) ^ _sprite.GetHashCode();
                    hash = (hash * 397) ^ _startPos;
                    hash = (hash * 397) ^ _timing.GetHashCode();
                    hash = (hash * 397) ^ _speed.GetHashCode();
                    return (hash * 397) ^ _rotateSpeed.GetHashCode();
                }
            }
        }

        /// <summary>Stores the renderer state that must survive a pool reuse.</summary>
        private sealed class StarEntry
        {
            /// <summary>The source note.</summary>
            internal readonly TapDrop Note;
            /// <summary>The source renderer.</summary>
            internal readonly SpriteRenderer Renderer;
            /// <summary>The skin-selected material before split rendering.</summary>
            internal readonly Material? OriginalMaterial;
            /// <summary>Preserves per-renderer properties without modifying shared materials.</summary>
            internal readonly MaterialPropertyBlock Properties = new();
            /// <summary>Whether the split material is currently applied.</summary>
            internal bool Split;

            /// <summary>Captures the note's original material.</summary>
            internal StarEntry(TapDrop note, SpriteRenderer renderer)
            {
                Note = note;
                Renderer = renderer;
                OriginalMaterial = renderer.sharedMaterial;
            }
        }

        /// <summary>
        /// Registers a colored STAR after its skin has been assigned. Pairs only
        /// when exactly one left and one right STAR share the same trajectory.
        /// </summary>
        /// <param name="note">The initialized STAR.</param>
        /// <param name="renderer">Its main sprite renderer.</param>
        /// <param name="session">The gameplay controller owning the note.</param>
        internal static void Register(TapDrop note, SpriteRenderer renderer, object session)
        {
            Unregister(note);
            if (renderer.sprite == null || Registered.ContainsKey(note))
            {
                return;
            }

            var key = new StarKey(note, renderer, session);
            if (!Groups.TryGetValue(key, out var group))
            {
                group = new List<StarEntry>(2);
                Groups.Add(key, group);
            }

            group.Add(new StarEntry(note, renderer));
            Registered.Add(note, key);
            UpdateGroup(group);
        }

        /// <summary>
        /// Removes a judged/destroyed note and restores the partner's full color
        /// when the split pair ceases to exist.
        /// </summary>
        /// <param name="note">The STAR leaving the active note pool.</param>
        internal static void Unregister(TapDrop note)
        {
            if (!Registered.TryGetValue(note, out var key))
            {
                return;
            }

            Registered.Remove(note);
            var group = Groups[key];
            for (var i = group.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(group[i].Note, note))
                {
                    continue;
                }

                Restore(group[i]);
                group.RemoveAt(i);
                break;
            }

            if (group.Count == 0)
            {
                Groups.Remove(key);
            }
            else
            {
                UpdateGroup(group);
            }
        }

        /// <summary>Recomputes the pair without modifying any note's input or hit state.</summary>
        private static void UpdateGroup(List<StarEntry> group)
        {
            foreach (var entry in group)
            {
                Restore(entry);
            }

            if (group.Count != 2)
            {
                return;
            }

            var a = group[0];
            var b = group[1];
            if (a.Note.RequiredHand == b.Note.RequiredHand)
            {
                return;
            }

            var material = Resources.Load<Material>(MaterialResourcePath);
            if (material == null)
            {
                // The original colored STAR remains playable when this optional asset is absent.
                return;
            }

            ApplySplit(a, material);
            ApplySplit(b, material);
        }

        /// <summary>Applies one half of the shared sprite, respecting its local rotation.</summary>
        private static void ApplySplit(StarEntry entry, Material material)
        {
            if (entry.Renderer == null || entry.Renderer.sprite == null)
            {
                return;
            }

            var bounds = entry.Renderer.sprite.bounds;
            entry.Renderer.GetPropertyBlock(entry.Properties);
            entry.Properties.SetFloat(SplitSideId,
                entry.Note.RequiredHand == AirshiftRequiredHand.Left ? -1f : 1f);
            entry.Properties.SetFloat(SplitCenterId, bounds.center.x);
            entry.Properties.SetFloat(DividerHalfWidthId, bounds.size.x * 0.025f);
            entry.Renderer.SetPropertyBlock(entry.Properties);
            entry.Renderer.sharedMaterial = material;
            entry.Split = true;
        }

        /// <summary>Restores the skin-selected material, leaving original tint intact.</summary>
        private static void Restore(StarEntry entry)
        {
            if (!entry.Split)
            {
                return;
            }

            if (entry.Renderer != null)
            {
                entry.Renderer.sharedMaterial = entry.OriginalMaterial;
            }
            entry.Split = false;
        }
    }
}