using System;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using System.Diagnostics;
using CSCore.Streams;
using System.Threading;

namespace LiveSplit.SpyroTheDragonMusicPlayer.Music
{
    public class MusicPlayer
    {
        private ISoundOut _soundOut;
        private LinearFadeStrategy _linearFadeStrategy;
        private FadeInOut _fadeInOut;

        private ISoundOut _soundOutQueued;
        private LinearFadeStrategy _linearFadeStrategyQueued;
        private FadeInOut _fadeInOutQueued;

        private Thread _volumePollingThread;
        private CancellationTokenSource _threadCancellationSource;

        private bool _paused = false;
        private bool _reset = false;

        private const float DEFAULT_VOLUME = 1.0f;
        private const float FADE_OUT_VOLUME = 0.0f;
        private const double SET_VOLUME_MILLISECONDS = 1;
        private const double FADE_MILLISECONDS = 333;
        private const int FADE_VOLUME_POLLING_INTERVAL = 33;

        private const int MAX_VOLUME = 100;
        private const int MIN_VOLUME = 0;

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        // This sets the volume of the ISoundOut. This is independent
        // of the IFadeStrategy volume, which is not exposed
        public int Volume
        {
            set
            {
                if (_soundOut != null)
                {
                    int temp;
                    if (value > MAX_VOLUME)
                        temp = MAX_VOLUME;
                    else if (value < MIN_VOLUME)
                        temp = MIN_VOLUME;
                    else
                        temp = value;
                    _soundOut.Volume = temp / (float)MAX_VOLUME;
                }
            }
        }

        public MusicPlayer()
        {
            _threadCancellationSource = new CancellationTokenSource();
            var threadStart = new ThreadStart(() => PollVolume(_threadCancellationSource.Token));
            _volumePollingThread = new Thread(threadStart);
            _volumePollingThread.IsBackground = true;
            _volumePollingThread.Start();
        }

        public bool Open(string filename)
        {
            try
            {
                IWaveSource waveSource = CodecFactory.Instance.GetCodec(filename);
                waveSource = new LoopStream(waveSource) { EnableLoop = true };

                _fadeInOutQueued = waveSource.ToSampleSource().AppendSource(x => new FadeInOut(x));

                _linearFadeStrategyQueued = new LinearFadeStrategy { Channels = _fadeInOutQueued.WaveFormat.Channels, SampleRate = _fadeInOutQueued.WaveFormat.SampleRate };
                _fadeInOutQueued.FadeStrategy = _linearFadeStrategyQueued;

                _soundOutQueued = new WasapiOut() { Latency = 100 };
                _soundOutQueued.Initialize(_fadeInOutQueued.ToWaveSource());
                _soundOutQueued.Volume = DEFAULT_VOLUME;

                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                CleanupSecondary();

                return false;
            }
        }

        public void Play()
        {
            if (_fadeInOutQueued != null && _linearFadeStrategyQueued != null && _soundOutQueued != null)
            {
                Cleanup();

                _fadeInOut = _fadeInOutQueued;
                _fadeInOutQueued = null;

                _linearFadeStrategy = _linearFadeStrategyQueued;
                _linearFadeStrategyQueued = null;

                _soundOut = _soundOutQueued;
                _soundOutQueued = null;
            }

            if (_reset || PlaybackState == PlaybackState.Stopped)
            {
                ResetTrackPosition();
                PlaySoundOut();
                SetFadeVolume(DEFAULT_VOLUME);
            }
            else
            {
                PlaySoundOut();
                if (_linearFadeStrategy != null && _linearFadeStrategy.TargetVolume < DEFAULT_VOLUME)
                    FadeTo(DEFAULT_VOLUME);
            }

            _reset = false;
        }

        public void Pause()
        {
            if (_linearFadeStrategy != null && _linearFadeStrategy.TargetVolume > FADE_OUT_VOLUME)
                FadeTo(FADE_OUT_VOLUME);
        }

        public void PauseAndReset()
        {
            _reset = true;
            Pause();
        }

        public void Dispose()
        {
            Cleanup();
            CleanupSecondary();

            if (_volumePollingThread != null)
            {
                _threadCancellationSource.Cancel();
                _volumePollingThread = null;
                _threadCancellationSource = null;
            }
        }

        private void Cleanup()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }
            if (_fadeInOut != null)
            {
                _fadeInOut.Dispose();
                _fadeInOut = null;
            }
            _linearFadeStrategy = null;
        }

        private void CleanupSecondary()
        {
            if (_soundOutQueued != null)
            {
                _soundOutQueued.Dispose();
                _soundOutQueued = null;
            }
            if (_fadeInOutQueued != null)
            {
                _fadeInOutQueued.Dispose();
                _fadeInOutQueued = null;
            }
            _linearFadeStrategyQueued = null;
        }

        private void PlaySoundOut()
        {
            if (_soundOut != null && PlaybackState != PlaybackState.Playing)
            {
                _soundOut.Play();
            }
        }

        private void PauseSoundOut()
        {
            if (_soundOut != null && PlaybackState != PlaybackState.Paused)
            {
                _soundOut.Pause();
            }
        }

        private void SetFadeVolume(float volume)
        {
            if (_linearFadeStrategy != null)
            {
                _linearFadeStrategy.StartFading(volume, volume, SET_VOLUME_MILLISECONDS);
            }
        }

        private void FadeTo(float toVolume)
        {
            if (_linearFadeStrategy != null)
            {
                _linearFadeStrategy.StartFading(_linearFadeStrategy.CurrentVolume, toVolume, FADE_MILLISECONDS);
            }
        }

        private void ResetTrackPosition()
        {
            if (_soundOut != null)
            {
                _soundOut.WaveSource.Position = 0;
            }
        }

        // IFadeStrategy has a FadingFinished event, but we use this polling thread instead
        // due to issues with pausing the ISoundOut from the thread that handles this event.
        private void PollVolume(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // only pause once, when CurrentVolume == TargetVolume == FADE_OUT_VOLUME
                if (_linearFadeStrategy != null && _linearFadeStrategy.CurrentVolume <= FADE_OUT_VOLUME && _linearFadeStrategy.TargetVolume <= FADE_OUT_VOLUME && !_paused)
                {
                    PauseSoundOut();
                    _paused = true;
                }

                // release lock on pausing when TargetVolume goes above FADE_OUT_VOLUME
                else if (_linearFadeStrategy != null && _linearFadeStrategy.TargetVolume > FADE_OUT_VOLUME)
                {
                    _paused = false;
                }

                Thread.Sleep(FADE_VOLUME_POLLING_INTERVAL);
            }
        }
    }
}
