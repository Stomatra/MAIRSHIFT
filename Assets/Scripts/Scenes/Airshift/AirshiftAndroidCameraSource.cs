// AIRSHIFT Android camera tracking proof of concept.
// All image processing stays on device. No network, no new packages.
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Android;

#nullable enable
namespace MajdataPlay.Scenes.Airshift
{
    internal sealed class AirshiftAndroidCameraSource : IDisposable
    {
        private const int ScanWidth = 160;
        private const int ScanHeight = 120;
        private const int ScanPixels = ScanWidth * ScanHeight;
        private const float Smoothing = 0.12f;
        // AIRSHIFT_COMFORT_V04: physical motion needed to reach a fixed note radius is 85% of v0.3.
        // Do NOT scale the rendered game disc or sensor geometry: only amplify camera movement.
        private const float PhysicalReachScale = 0.85f;
        private const float MaxJump = 0.85f;
        private const double LostSeconds = 0.40;
        private const double ProcessPeriod = 1.0 / 25.0;

        private readonly byte[] _redMask = new byte[ScanPixels];
        private readonly byte[] _blueMask = new byte[ScanPixels];
        private readonly byte[] _visited = new byte[ScanPixels];
        private readonly int[] _queue = new int[ScanPixels];
        private readonly Color32[] _previewPixels = new Color32[ScanPixels];
        private readonly Texture2D _preview;
        private readonly bool[] _seen = new bool[2];
        private readonly bool[] _filteredValid = new bool[2];
        private readonly Vector2[] _lastSpot = new Vector2[2];
        private readonly Vector2[] _filtered = new Vector2[2];

        private WebCamTexture? _camera;
        private Color32[]? _cameraPixels;
        private bool _preferFrontCamera = true;
        private bool _mirrorFront = true;
        private bool _isPaused;
        private bool _disposed;
        private bool _requestSourceReset;
        private double _lastProcessed;
        private double _lastCameraFrame;
        private double _nextStartAttempt;
        private double _lastFpsCheck;
        private int _fpsFrames;
        private float _actualFps;
        private int _processedFrames;
        private int _redArea;
        private int _blueArea;
        private int _failedReads;
        private string _status = "Checking Android camera permission";
        private float _brightnessThreshold = 110f;
        private float _saturationThreshold = 0.37f;
        private int _minArea = 4; // 160x120 scale versus previous browser 320x240.

        internal AirshiftAndroidCameraSource()
        {
            _preview = new Texture2D(ScanWidth, ScanHeight, TextureFormat.RGBA32, false);
            _preview.filterMode = FilterMode.Bilinear;
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                _status = "Please allow Camera permission";
            }
            else
            {
                _status = "Camera permission granted";
            }
            Debug.Log("[AIRSHIFT ANDROID CAMERA] Camera source created. No UDP required.");
        }

        internal void Poll(AirshiftDualCursorTracker tracker, double now, Action onSourceDiscontinuity)
        {
            if (_disposed || _isPaused) return;
            if (_requestSourceReset)
            {
                ClearTracking(tracker, now, onSourceDiscontinuity);
                _filteredValid[0] = _filteredValid[1] = false;
                _requestSourceReset = false;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                _status = "Camera permission missing: tap ALLOW below";
                StopCamera();
                ClearTracking(tracker, now, onSourceDiscontinuity);
                return;
            }
            if (_camera == null && now >= _nextStartAttempt)
            {
                _nextStartAttempt = now + 2.0;
                StartCamera();
            }
            if (_camera == null) return;
            if (!_camera.isPlaying)
            {
                _status = "Camera stopped - retry with SWITCH CAM";
                return;
            }
            if (_camera.width <= 16 || _camera.height <= 16)
            {
                _status = "Waiting for first video frame";
                return;
            }
            if (!_camera.didUpdateThisFrame)
            {
                if (_lastCameraFrame > 0 && now - _lastCameraFrame > LostSeconds)
                {
                    _status = "Camera frames stalled";
                    ClearTracking(tracker, now, onSourceDiscontinuity);
                }
                return;
            }
            _lastCameraFrame = now;
            if (now - _lastProcessed < ProcessPeriod) return;
            _lastProcessed = now;
            try
            {
                int expected = _camera.width * _camera.height;
                if (_cameraPixels == null || _cameraPixels.Length != expected)
                    _cameraPixels = new Color32[expected];
                _camera.GetPixels32(_cameraPixels);
                AnalyzePixels(_cameraPixels, _camera.width, _camera.height,
                    _camera.videoRotationAngle, _camera.videoVerticallyMirrored,
                    _preferFrontCamera && _mirrorFront);
            }
            catch (Exception ex)
            {
                _failedReads++;
                if (_failedReads <= 3) Debug.LogWarning("[AIRSHIFT ANDROID CAMERA] Read failed: " + ex.Message);
                _status = "GetPixels32 failed: " + ex.GetType().Name;
                ClearTracking(tracker, now, onSourceDiscontinuity);
                return;
            }

            _preview.SetPixels32(_previewPixels);
            _preview.Apply(false, false);
            _processedFrames++;
            _fpsFrames++;
            if (_lastFpsCheck <= 0) _lastFpsCheck = now;
            if (now - _lastFpsCheck >= 1.0)
            {
                _actualFps = (float)(_fpsFrames / (now - _lastFpsCheck));
                _lastFpsCheck = now;
                _fpsFrames = 0;
            }

            // Camera's image coordinates are bottom-left origin; AIRSHIFT positions are also Y-up.
            UpdateHand(tracker, HandId.Left, _blueMask, now, onSourceDiscontinuity, out _blueArea);
            UpdateHand(tracker, HandId.Right, _redMask, now, onSourceDiscontinuity, out _redArea);
            _status = string.Format("{0}x{1}, rot={2}, {3:F1} fps, frames={4}",
                _camera.width, _camera.height, _camera.videoRotationAngle, _actualFps, _processedFrames);
            if (_processedFrames == 1 || _processedFrames % 100 == 0)
                Debug.Log("[AIRSHIFT ANDROID CAMERA] " + _status +
                    " | BLUE " + (_seen[0] ? "FOUND" : "NONE") +
                    " | RED " + (_seen[1] ? "FOUND" : "NONE"));
        }

        private void StartCamera()
        {
            if (_disposed || _isPaused) return;
            try
            {
                var devices = WebCamTexture.devices;
                if (devices == null || devices.Length == 0)
                {
                    _status = "No cameras found";
                    return;
                }
                var selected = devices[0];
                foreach (var device in devices)
                {
                    if (device.isFrontFacing == _preferFrontCamera)
                    {
                        selected = device;
                        break;
                    }
                }
                _preferFrontCamera = selected.isFrontFacing;
                _camera = new WebCamTexture(selected.name, 640, 480, 30);
                _camera.Play();
                _cameraPixels = null;
                _lastCameraFrame = 0;
                _lastProcessed = 0;
                _status = "Starting " + (_preferFrontCamera ? "FRONT" : "BACK") + " camera";
                Debug.Log("[AIRSHIFT ANDROID CAMERA] Starting: " + selected.name);
            }
            catch (Exception ex)
            {
                StopCamera();
                _status = "Camera start failed: " + ex.Message;
                Debug.LogError("[AIRSHIFT ANDROID CAMERA] " + _status);
            }
        }

        // This is the inverse of clockwise videoRotationAngle (raw texture -> upright preview).
        // Sample the raw pixels directly into an upright and optionally mirrored small preview.
        private void AnalyzePixels(Color32[] source, int rawWidth, int rawHeight,
            int angle, bool verticalMirror, bool horizontalMirror)
        {
            int rotation = ((angle % 360) + 360) % 360;
            for (int y = 0; y < ScanHeight; y++)
            {
                float v = (y + 0.5f) / ScanHeight;
                for (int x = 0; x < ScanWidth; x++)
                {
                    float u = (x + 0.5f) / ScanWidth;
                    if (horizontalMirror) u = 1f - u;
                    float rx, ry;
                    switch (rotation)
                    {
                        case 90:  rx = 1f - v; ry = u; break;
                        case 180: rx = 1f - u; ry = 1f - v; break;
                        case 270: rx = v; ry = 1f - u; break;
                        default:  rx = u; ry = v; break;
                    }
                    if (verticalMirror) ry = 1f - ry;
                    int px = Mathf.Clamp((int)(rx * rawWidth), 0, rawWidth - 1);
                    int py = Mathf.Clamp((int)(ry * rawHeight), 0, rawHeight - 1);
                    int i = y * ScanWidth + x;
                    Color32 color = source[py * rawWidth + px];
                    _previewPixels[i] = color;
                    _redMask[i] = 0;
                    _blueMask[i] = 0;
                    int r = color.r, g = color.g, b = color.b;
                    int brightest = Math.Max(r, Math.Max(g, b));
                    int darkest = Math.Min(r, Math.Min(g, b));
                    int delta = brightest - darkest;
                    if (brightest < _brightnessThreshold || delta < brightest * _saturationThreshold || delta < 18)
                        continue;
                    float hue;
                    if (brightest == r) hue = 60f * (g - b) / delta;
                    else if (brightest == g) hue = 60f * ((b - r) / (float)delta + 2f);
                    else hue = 60f * ((r - g) / (float)delta + 4f);
                    if (hue < 0) hue += 360f;
                    if (hue <= 24f || hue >= 335f) _redMask[i] = 1;
                    else if (hue >= 185f && hue <= 275f) _blueMask[i] = 1;
                }
            }
        }

        private void UpdateHand(AirshiftDualCursorTracker tracker, HandId handId, byte[] mask,
            double now, Action onSourceDiscontinuity, out int detectedArea)
        {
            int index = (int)handId;
            Vector2 pixelPoint;
            if (!FindBestBlob(mask, _filteredValid[index], _lastSpot[index], out pixelPoint, out detectedArea))
            {
                if (_seen[index])
                {
                    tracker.MarkHandIdle(handId, now);
                    _seen[index] = false;
                    _filteredValid[index] = false;
                    onSourceDiscontinuity();
                }
                return;
            }

            if (!_filteredValid[index]) _filtered[index] = pixelPoint;
            else _filtered[index] = Vector2.Lerp(pixelPoint, _filtered[index], Smoothing);
            _lastSpot[index] = pixelPoint;
            _filteredValid[index] = true;

            // v0.4: virtual playfield stays aligned with notes and sensors, while
            // the body/camera workspace is reduced to 0.85x around camera center.
            // Same target position now requires 85% of the former camera displacement.
            float normalizedX = 0.5f + ((_filtered[index].x / ScanWidth) - 0.5f)
                / (0.70f * PhysicalReachScale);
            float normalizedY = 0.5f + ((_filtered[index].y / ScanHeight) - 0.5f)
                / (0.76f * PhysicalReachScale);
            Vector2 target = new Vector2(normalizedX, normalizedY);
            var state = tracker.GetHand(handId);
            double sampleTime = Math.Max(now, state.Timestamp + 0.001);
            if (!_seen[index] || Vector2.Distance(state.Position, target) > MaxJump)
            {
                tracker.SnapHandPosition(handId, target, sampleTime);
                _seen[index] = true;
                onSourceDiscontinuity();
                return;
            }
            tracker.ApplyHandPosition(handId, target, sampleTime, AirshiftInputSource.CameraTracking);
        }

        private bool FindBestBlob(byte[] mask, bool hasPrevious, Vector2 previous,
            out Vector2 bestPoint, out int bestArea)
        {
            Array.Clear(_visited, 0, _visited.Length);
            float bestScore = -1f;
            bestPoint = Vector2.zero;
            bestArea = 0;
            for (int i = 0; i < ScanPixels; i++)
            {
                if (mask[i] == 0 || _visited[i] != 0) continue;
                int head = 0, tail = 1;
                _visited[i] = 1;
                _queue[0] = i;
                float sumX = 0f, sumY = 0f;
                while (head < tail)
                {
                    int at = _queue[head++];
                    int px = at % ScanWidth;
                    int py = at / ScanWidth;
                    sumX += px + 0.5f;
                    sumY += py + 0.5f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = py + dy;
                        if (ny < 0 || ny >= ScanHeight) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = px + dx;
                            if ((dx == 0 && dy == 0) || nx < 0 || nx >= ScanWidth) continue;
                            int neighbor = ny * ScanWidth + nx;
                            if (mask[neighbor] == 0 || _visited[neighbor] != 0) continue;
                            _visited[neighbor] = 1;
                            _queue[tail++] = neighbor;
                        }
                    }
                }
                if (tail < _minArea) continue;
                Vector2 point = new Vector2(sumX / tail, sumY / tail);
                float distance = hasPrevious ? Vector2.Distance(point, previous) : 0f;
                float score = tail / (1f + distance / 45f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = point;
                    bestArea = tail;
                }
            }
            return bestScore >= 0f;
        }

        private void ClearTracking(AirshiftDualCursorTracker tracker, double now, Action onSourceDiscontinuity)
        {
            if (!_seen[0] && !_seen[1]) return;
            _seen[0] = _seen[1] = false;
            _filteredValid[0] = _filteredValid[1] = false;
            tracker.MarkHandIdle(HandId.Left, now);
            tracker.MarkHandIdle(HandId.Right, now);
            onSourceDiscontinuity();
        }

        // AIRSHIFT_ANDROID_FULL_V03: excludes lost colors from held-note contact queries.
        internal bool IsHandVisible(HandId hand)
        {
            return !_disposed && !_isPaused && _seen[(int)hand];
        }

        internal void OnPause(bool pause)
        {
            _isPaused = pause;
            if (pause) StopCamera();
            else { _requestSourceReset = true; _status = "Resuming camera"; }
        }

        private void StopCamera()
        {
            if (_camera != null)
            {
                try { if (_camera.isPlaying) _camera.Stop(); }
                catch (Exception) { /* app is shutting down */ }
                _camera = null;
            }
            _cameraPixels = null;
            _lastCameraFrame = 0;
            _nextStartAttempt = 0;
        }

        internal void DrawOverlay()
        {
            if (_disposed) return;
            float width = Mathf.Clamp(Screen.width * 0.24f, 225f, 470f);
            float previewHeight = width * ScanHeight / ScanWidth;
            float controlH = Mathf.Clamp(Screen.height * 0.048f, 44f, 75f);
            Rect box = new Rect(Screen.width - width - 16f, 12f, width, previewHeight + controlH * 2f + 90f);
            GUI.Box(box, "Android camera tracking (prototype)");
            Rect previewRect = new Rect(box.x + 8f, box.y + 28f, width - 16f, previewHeight - 22f);
            GUI.DrawTexture(previewRect, _preview, ScaleMode.StretchToFill);
            string blue = _seen[0] ? "BLUE FOUND (" + _blueArea + ")" : "BLUE LOST";
            string red = _seen[1] ? "RED FOUND (" + _redArea + ")" : "RED LOST";
            GUI.Label(new Rect(box.x + 8f, previewRect.yMax + 4f, width - 16f, 42f),
                blue + " | " + red + "\n" + _status);
            float y = previewRect.yMax + 50f;
            float half = (width - 24f) * 0.5f;
            if (GUI.Button(new Rect(box.x + 8f, y, half, controlH),
                _preferFrontCamera ? "SWITCH BACK" : "SWITCH FRONT"))
            {
                _preferFrontCamera = !_preferFrontCamera;
                _requestSourceReset = true;
                StopCamera();
            }
            if (GUI.Button(new Rect(box.x + 16f + half, y, half, controlH),
                _mirrorFront ? "MIRROR ON" : "MIRROR OFF"))
            {
                _mirrorFront = !_mirrorFront;
                _requestSourceReset = true;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                if (GUI.Button(new Rect(box.x + 8f, y + controlH + 4f, width - 16f, controlH), "ALLOW CAMERA"))
                    Permission.RequestUserPermission(Permission.Camera);
            }
            else
            {
                GUI.Label(new Rect(box.x + 8f, y + controlH + 4f, width - 16f, controlH),
                    "HSV bright>110 sat>37% | no calibration");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopCamera();
            UnityEngine.Object.Destroy(_preview);
        }
    }
}
#endif
