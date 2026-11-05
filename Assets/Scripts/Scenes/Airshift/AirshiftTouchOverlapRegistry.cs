using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MajdataPlay.IO;
using MajdataPlay.Scenes.Game.Notes.Behaviours;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Provides visual-only complementary halves for precisely co-located
    /// opposing TOUCHes. It never changes NoteManager queues or judgments.
    /// </summary>
    internal static class AirshiftTouchOverlapRegistry
    {
        private const string MaterialPath = "AirshiftOverlaps/TouchSplit";
        private static readonly int SideId = Shader.PropertyToID("_SplitSide");
        private static readonly int OriginId = Shader.PropertyToID("_SplitOrigin");
        private static readonly int NormalId = Shader.PropertyToID("_SplitNormal");
        private static readonly int WidthId = Shader.PropertyToID("_DividerHalfWidth");

        private readonly struct Key : IEquatable<Key>
        {
            private readonly object _session;
            private readonly SensorArea _sensor;
            private readonly float _timing;
            private readonly float _speed;
            private readonly Sprite _fan;
            private readonly Sprite _point;

            internal Key(object session, SensorArea sensor, float timing, float speed,
                         Sprite fan, Sprite point)
            {
                _session = session;
                _sensor = sensor;
                _timing = timing;
                _speed = speed;
                _fan = fan;
                _point = point;
            }

            public bool Equals(Key other) =>
                ReferenceEquals(_session, other._session) &&
                _sensor == other._sensor &&
                _timing == other._timing &&
                _speed == other._speed &&
                ReferenceEquals(_fan, other._fan) &&
                ReferenceEquals(_point, other._point);

            public override bool Equals(object? obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = RuntimeHelpers.GetHashCode(_session);
                    hash = (hash * 397) ^ (int)_sensor;
                    hash = (hash * 397) ^ _timing.GetHashCode();
                    hash = (hash * 397) ^ _speed.GetHashCode();
                    hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(_fan);
                    return (hash * 397) ^ RuntimeHelpers.GetHashCode(_point);
                }
            }
        }

        private sealed class Entry
        {
            internal readonly TouchDrop Note;
            internal readonly SpriteRenderer[] Renderers;
            internal readonly Material?[] OriginalMaterials;
            internal readonly MaterialPropertyBlock[] OriginalProperties;
            internal bool Split;

            internal Entry(TouchDrop note, SpriteRenderer[] fans,
                           SpriteRenderer point, SpriteRenderer border)
            {
                Note = note;
                Renderers = new SpriteRenderer[6];
                Array.Copy(fans, Renderers, 4);
                Renderers[4] = point;
                Renderers[5] = border;
                OriginalMaterials = new Material?[Renderers.Length];
                OriginalProperties = new MaterialPropertyBlock[Renderers.Length];
                for (var i = 0; i < Renderers.Length; i++)
                {
                    OriginalMaterials[i] = Renderers[i].sharedMaterial;
                    var props = new MaterialPropertyBlock();
                    Renderers[i].GetPropertyBlock(props);
                    OriginalProperties[i] = props;
                }
            }
        }

        private static readonly Dictionary<Key, List<Entry>> Groups = new();
        private static readonly Dictionary<TouchDrop, Key> Registered = new();

        /// <summary>Registers a newly initialized plain L/R TOUCH.</summary>
        internal static void Register(TouchDrop note, object session, SensorArea sensor,
            float timing, float speed, SpriteRenderer[] fans,
            SpriteRenderer point, SpriteRenderer border)
        {
            Unregister(note);
            if (fans.Length != 4 || fans[0].sprite == null || point.sprite == null)
            {
                return;
            }

            var key = new Key(session, sensor, timing, speed, fans[0].sprite, point.sprite);
            if (!Groups.TryGetValue(key, out var group))
            {
                group = new List<Entry>(2);
                Groups.Add(key, group);
            }

            group.Add(new Entry(note, fans, point, border));
            Registered.Add(note, key);
            Refresh(group);
        }

        /// <summary>Releases the pair on hit/miss, pool reuse or destruction.</summary>
        internal static void Unregister(TouchDrop note)
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
                Refresh(group);
            }
        }

        private static void Refresh(List<Entry> group)
        {
            foreach (var entry in group)
            {
                Restore(entry);
            }

            if (group.Count != 2 ||
                group[0].Note.RequiredHand == group[1].Note.RequiredHand)
            {
                return;
            }

            var material = Resources.Load<Material>(MaterialPath);
            if (material == null)
            {
                // A missing optional rendering asset must never block judging.
                return;
            }

            Apply(group[0], material);
            Apply(group[1], material);
        }

        private static void Apply(Entry entry, Material material)
        {
            if (entry.Note == null)
            {
                return;
            }

            // All six renderers use the *same world-space plane*. Fan positions
            // animate independently but the blue/red boundary stays fixed
            // through the TOUCH center (and rotates with its parent transform).
            var center = entry.Note.transform.position;
            var right = entry.Note.transform.right;
            var origin = new Vector4(center.x, center.y, center.z, 0f);
            var normal = new Vector4(right.x, right.y, right.z, 0f);
            var side = entry.Note.RequiredHand == AirshiftRequiredHand.Left ? -1f : 1f;

            for (var i = 0; i < entry.Renderers.Length; i++)
            {
                var renderer = entry.Renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var props = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(props);
                props.SetFloat(SideId, side);
                props.SetVector(OriginId, origin);
                props.SetVector(NormalId, normal);
                props.SetFloat(WidthId, 0.022f);
                renderer.SetPropertyBlock(props);
                renderer.sharedMaterial = material;
            }

            entry.Split = true;
        }

        private static void Restore(Entry entry)
        {
            if (!entry.Split)
            {
                return;
            }

            for (var i = 0; i < entry.Renderers.Length; i++)
            {
                var renderer = entry.Renderers[i];
                if (renderer == null)
                {
                    continue;
                }
                renderer.sharedMaterial = entry.OriginalMaterials[i];
                renderer.SetPropertyBlock(entry.OriginalProperties[i]);
            }
            entry.Split = false;
        }
    }
}
