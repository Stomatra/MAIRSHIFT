using Cysharp.Threading.Tasks;
using MajdataPlay.Extensions;
using MajdataPlay.Numerics;
using MajdataPlay.Utils;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#nullable enable
namespace MajdataPlay.IO
{
    public class UnityAudioSample : AudioSampleWrap
    {
        private AudioClip? _audioClip;
        private AudioSource? _audioSource;
        private GameObject _gameObject;
        public override bool IsLoop
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource!.loop;
            }
            set 
            {
                ThrowIfDisposed();
                _audioSource!.loop = value; 
            }
        }
        public override bool IsEmpty
        {
            get
            {
                return false;
            }
        }
        public override double CurrentSec
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfCanSeekNotSupported();
                return _audioSource!.time;
            }
            set
            {
                ThrowIfDisposed();
                _audioSource!.time = (float)value;
            }
        }
        public override float Volume
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource!.volume;
            }
            set
            {
                ThrowIfDisposed();
                var volume = value.Clamp(0, 1) * MajEnv.Settings.Audio.Volume.Global.Clamp(0, 1);
                _audioSource!.volume = volume;
            }
        }
        public override float Speed
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource!.pitch;
            }
            set
            {
                ThrowIfDisposed();
                _audioSource!.pitch = value;
            }
        }// this is not perfect and will change the pitch. FUCK YOU UNITY
        public override TimeSpan Length
        {
            get
            {
                return TimeSpan.FromSeconds(_audioClip!.length);
            }
        }
        public override bool IsPlaying
        {
            get
            {
                ThrowIfDisposed();
                return _audioSource!.isPlaying;
            }
        }
        public UnityAudioSample(AudioClip audioClip, GameObject gameObject)
        {
            if (audioClip == null)
                throw new ArgumentNullException(nameof(audioClip), "Unity audio request returned no decoded AudioClip.");
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));
            this._audioClip = audioClip;
            this._gameObject = gameObject;
            this._audioClip.LoadAudioData();
            _audioSource = this._gameObject.AddComponent<AudioSource>();
            _audioSource.clip = audioClip;
            _audioSource.loop = false;
            _audioSource.bypassEffects = true;
        }
        ~UnityAudioSample()
        {
            Dispose();
        }

        public override void PlayOneShot()
        {
            ThrowIfDisposed();
            _audioSource!.time = 0;
            _audioSource.Play();
        }
        public override void SetVolume(float volume)
        {
            ThrowIfDisposed();
            Volume = volume;
        }
        public override void Play()
        {
            ThrowIfDisposed();
            _audioSource!.Play();
        }
        public override void Pause()
        {
            ThrowIfDisposed();
            _audioSource!.Pause();
        }
        public override void Stop()
        {
            ThrowIfDisposed();
            _audioSource!.Stop();
            _audioSource.time = 0;
        }
        public override void Dispose()
        {
            if(_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            // An unsuccessful constructor can still be finalized; never
            // dereference absent Unity objects while cleaning up.
            var source = _audioSource;
            var clip = _audioClip;
            UniTask.Post(() =>
            {
                if (source != null)
                {
                    source.Stop();
                    UnityEngine.Object.Destroy(source);
                }
                if (clip != null)
                {
                    clip.UnloadAudioData();
                    UnityEngine.Object.DestroyImmediate(clip, true);
                }
            });
        }
        public override async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            await using (UniTask.ReturnToCurrentSynchronizationContext())
            {
                await UniTask.SwitchToMainThread();
                if (_audioSource != null)
                {
                    _audioSource.Stop();
                    UnityEngine.Object.Destroy(_audioSource);
                }
                if (_audioClip != null)
                {
                    _audioClip.UnloadAudioData();
                    UnityEngine.Object.DestroyImmediate(_audioClip, true);
                }

                _audioClip = null;
                _audioSource = null;
            }
        }
        private static Uri ToAudioUri(string filePath)
        {
            if (!Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
                throw new ArgumentException("Unity audio requires an absolute URI: " + filePath, nameof(filePath));
            return uri;
        }

        private static AudioClip GetDecodedClip(UnityWebRequest request, Uri uri)
        {
            if (request.result != UnityWebRequest.Result.Success)
                throw new System.IO.IOException(
                    "Unity audio request failed for '" + uri + "': " + request.result +
                    " - " + request.error);

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
                throw new System.IO.InvalidDataException(
                    "Unity audio decoder returned no AudioClip for '" + uri +
                    "'. Verify that the audio file is valid and encoded in a supported format.");
            return clip;
        }

        public static UnityAudioSample Create(string filePath, GameObject gameObject)
        {
            var uri = ToAudioUri(filePath);
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, AudioType.UNKNOWN))
            {
                // Unity's string URL normalization may treat '+' as a space
                // when a file path also contains spaces (UUM-57122).
                // The Uri setter bypasses that string normalization.
                if (uri.IsFile)
                {
                    www.uri = uri;
                }
                www.SetRequestHeader("User-Agent", MajEnv.HTTP_USER_AGENT);
                www.SendWebRequest();
                while (!www.isDone) ;
                var myClip = GetDecodedClip(www, uri);
                return new UnityAudioSample(myClip, gameObject)
                {
                    CanSeek = uri.IsFile,
                };
            }
        }
        public static async UniTask<UnityAudioSample> CreateAsync(string filePath, GameObject gameObject)
        {
            var uri = ToAudioUri(filePath);
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, AudioType.UNKNOWN))
            {
                // Unity's string URL normalization may treat '+' as a space
                // when a file path also contains spaces (UUM-57122).
                // The Uri setter bypasses that string normalization.
                if (uri.IsFile)
                {
                    www.uri = uri;
                }
                www.SetRequestHeader("User-Agent", MajEnv.HTTP_USER_AGENT);
                try
                {
                    await www.SendWebRequest();
                }
                catch (UnityWebRequestException exception)
                {
                    throw new System.IO.IOException(
                        "Unity audio request failed for '" + uri.AbsoluteUri +
                        "' (actual request URI: '" + www.url + "'): " +
                        www.result + " - " + www.error, exception);
                }
                var myClip = GetDecodedClip(www, uri);
                return new UnityAudioSample(myClip, gameObject)
                {
                    CanSeek = uri.IsFile
                };
            }
        }
    }
}