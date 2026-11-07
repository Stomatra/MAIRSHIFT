using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    /// <summary>
    /// Receives only red/blue camera coordinates via a loopback-only UDP bridge.
    /// Poll on the Unity main thread. No external packages or threads required.
    /// </summary>
    internal sealed class AirshiftCameraUdpSource : IDisposable
    {
        [Serializable]
        private sealed class CameraPacket
        {
            public int seq;
            public float tMs;
            public bool redFound;
            public float redX;
            public float redY;
            public bool blueFound;
            public float blueX;
            public float blueY;
        }

        private const double DisconnectSeconds = 0.40;
        private const float MaxPositionJump = 0.85f;
        private readonly UdpClient _receiver;
        private readonly List<CameraPacket> _batch = new(64);
        private readonly bool[] _hasPosition = new bool[2];
        private readonly IPEndPoint _anySender = new(IPAddress.Any, 0);
        private bool _seenAnyFrame;
        private double _lastArrival;
        private int _lastSeq = -1;
        private bool _disposed;

        internal int ReceivedCount { get; private set; }
        internal int LostSequenceCount { get; private set; }
        internal bool HasLiveFrames(double now) => _seenAnyFrame && now - _lastArrival < DisconnectSeconds;

        internal AirshiftCameraUdpSource(int port)
        {
            _receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            _receiver.Client.ReceiveBufferSize = 256 * 1024;
            Debug.Log($"[AIRSHIFT CAMERA] UDP listening at 127.0.0.1:{port}");
        }

        internal void Poll(
            AirshiftDualCursorTracker tracker,
            double now,
            Action onSourceDiscontinuity)
        {
            if (_disposed) return;
            _batch.Clear();
            // Avoid blocking the game frame; reject stale backlog rather than playing delayed motion.
            bool overflow = false;
            while (_receiver.Available > 0)
            {
                IPEndPoint sender = _anySender;
                byte[] data;
                try { data = _receiver.Receive(ref sender); }
                catch (SocketException) { break; }
                if (sender.Address != IPAddress.Loopback || data.Length < 2 || data.Length > 512)
                    continue;
                CameraPacket? packet;
                try { packet = JsonUtility.FromJson<CameraPacket>(Encoding.UTF8.GetString(data)); }
                catch (Exception) { continue; }
                if (packet == null || !IsValid(packet)) continue;
                if (_batch.Count == 64)
                {
                    // Too much queued video: discard earlier frames and re-anchor instead of generating bursts.
                    _batch.RemoveAt(0);
                    overflow = true;
                }
                _batch.Add(packet);
            }

            if (_batch.Count == 0)
            {
                if (_seenAnyFrame && now - _lastArrival >= DisconnectSeconds)
                {
                    _seenAnyFrame = false;
                    Invalidate(tracker, now, onSourceDiscontinuity);
                    Debug.LogWarning("[AIRSHIFT CAMERA] Tracking stream timed out; motion stopped.");
                }
                return;
            }

            bool restartOrReconnect = !_seenAnyFrame || overflow || now - _lastArrival > DisconnectSeconds;
            if (_lastSeq >= 0 && _batch[0].seq < _lastSeq)
                restartOrReconnect = true; // browser refresh restarted the sequence
            if (restartOrReconnect)
            {
                Invalidate(tracker, now, onSourceDiscontinuity);
                _lastSeq = -1;
            }
            _seenAnyFrame = true;
            _lastArrival = now;

            var latestMs = _batch[_batch.Count - 1].tMs;
            foreach (var frame in _batch)
            {
                if (frame.seq <= _lastSeq) continue;
                if (_lastSeq >= 0 && frame.seq > _lastSeq + 1)
                    LostSequenceCount += frame.seq - _lastSeq - 1;
                _lastSeq = frame.seq;
                ReceivedCount++;
                if (ReceivedCount <= 2 || ReceivedCount % 50 == 0)
                    Debug.Log($"[AIRSHIFT CAMERA] received {ReceivedCount} frames (blue={frame.blueFound}, red={frame.redFound}), seq gaps={LostSequenceCount}");

                // Preserve inter-camera-frame intervals even if two UDP packets arrive in one Unity Update.
                double sampleTime = now - Math.Max(0, latestMs - frame.tMs) / 1000.0;
                if (sampleTime < now - DisconnectSeconds)
                {
                    Invalidate(tracker, now, onSourceDiscontinuity);
                    continue;
                }
                ApplyOne(tracker, HandId.Left, frame.blueFound, frame.blueX, frame.blueY,
                    sampleTime, onSourceDiscontinuity);
                ApplyOne(tracker, HandId.Right, frame.redFound, frame.redX, frame.redY,
                    sampleTime, onSourceDiscontinuity);
            }
        }

        private void ApplyOne(
            AirshiftDualCursorTracker tracker,
            HandId handId,
            bool found,
            float x,
            float y,
            double timestamp,
            Action onSourceDiscontinuity)
        {
            int i = (int)handId;
            if (!found)
            {
                if (_hasPosition[i])
                {
                    tracker.MarkHandIdle(handId, timestamp);
                    _hasPosition[i] = false;
                    onSourceDiscontinuity();
                }
                return;
            }
            var target = new Vector2(x, y);
            var hand = tracker.GetHand(handId);
            double t = Math.Max(timestamp, hand.Timestamp + 0.001);
            if (!_hasPosition[i] || Vector2.Distance(hand.Position, target) > MaxPositionJump)
            {
                tracker.SnapHandPosition(handId, target, t);
                _hasPosition[i] = true;
                onSourceDiscontinuity();
                return;
            }
            tracker.ApplyHandPosition(handId, target, t, AirshiftInputSource.CameraTracking);
        }

        private void Invalidate(
            AirshiftDualCursorTracker tracker, double timestamp, Action onSourceDiscontinuity)
        {
            _hasPosition[0] = _hasPosition[1] = false;
            tracker.MarkHandIdle(HandId.Left, timestamp);
            tracker.MarkHandIdle(HandId.Right, timestamp);
            onSourceDiscontinuity();
        }

        private static bool IsValid(CameraPacket p) =>
            p.seq >= 0 &&
            ValidNumber(p.tMs) &&
            ValidNumber(p.redX) && ValidNumber(p.redY) &&
            ValidNumber(p.blueX) && ValidNumber(p.blueY) &&
            Mathf.Abs(p.redX) <= 5f && Mathf.Abs(p.redY) <= 5f &&
            Mathf.Abs(p.blueX) <= 5f && Mathf.Abs(p.blueY) <= 5f;

        private static bool ValidNumber(float n) => !float.IsNaN(n) && !float.IsInfinity(n);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _receiver.Close();
            _receiver.Dispose();
        }
    }
}
