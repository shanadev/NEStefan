using System;
using SDL2;

namespace GameEngine;

public struct AudioConfig
{
	public int samplesPerSecond;
	public int bytesPerSample;
}

public partial class Engine
{
    // what 'close enough to zero' is
    public const float SMALL_FLOAT = 0.000001f;

    public Queue<float> audioBuffer = new Queue<float>();

    private AudioConfig _audioConfig;

    public uint samplesCount = 0;

    public delegate float RequestAudioSample();

    private RequestAudioSample _externalCallback;

    SDL2.SDL.SDL_AudioCallback _audioCallback;

    public void InitAudio(int sampleRate, byte channels, ushort samples, RequestAudioSample callback)
    {
        _audioCallback = this.AudioCallback;
        _externalCallback = callback;

        if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
        {
            Console.WriteLine("Audio could not be initialized.");
            return;
        }

        //int numDevices = SDL.SDL_GetNumAudioDevices(0);
        //for (int i = 0; i < numDevices; i++)
        //{
        //    Console.WriteLine($"index: {i} - {SDL.SDL_GetAudioDeviceName(i, 0)}");
        //}

        //Console.WriteLine();

        _audioConfig = new AudioConfig();
        _audioConfig.samplesPerSecond = sampleRate;
        _audioConfig.bytesPerSample = sizeof(float);

        SDL.SDL_AudioSpec audioSpec = new SDL.SDL_AudioSpec();
        audioSpec.freq = _audioConfig.samplesPerSecond;
        audioSpec.format = SDL.AUDIO_F32;
        audioSpec.channels = channels;
        audioSpec.samples = samples;
        audioSpec.callback = _audioCallback;
        audioSpec.userdata = IntPtr.Zero;

        SDL.SDL_AudioSpec obtainedSpec;
        SDL.SDL_OpenAudio(ref audioSpec, out obtainedSpec);
        //uint audiodev = SDL.SDL_OpenAudioDevice(SDL.SDL_GetAudioDeviceName(3,0), 0, ref audioSpec, out obtainedSpec, 0);
        SDL.SDL_PauseAudio(0);
        //SDL.SDL_PauseAudioDevice(audiodev, 0);

    }


    private unsafe void AudioCallback(IntPtr unused, IntPtr buffer, int len)
    {
        float* fstream = (float*)(buffer);
        float newMixedValue = 0f;
        int totaldoubleSamplesNeeded = len / 4;
        for (long i = 0; i < totaldoubleSamplesNeeded; i++)
        {
            float newValue = _externalCallback();
            //float newValue = (float)(Math.Sin((float)(2.0f * Math.PI * (samplesCount / 44100f) * 220f)));
            fstream[i] = newValue;
            //Console.WriteLine(newValue);
            this.audioBuffer.Enqueue(newValue);
            samplesCount++;
        }
    }



    public static double lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
}


public class Envelope
{
    private double _attackTime;
    private double _decayTime;
    private double _sustainVolume;
    private double _releaseTime;
    private bool _triggered;
    private int _currStage;
    private double _currTime;
    private double _currVolume;
    private double _volumeBeforeCurrStage;

    public Envelope()
    {
        _attackTime = 0f;
        _decayTime = 0f;
        _sustainVolume = 1f;
        _releaseTime = 0f;
        _triggered = false;
        _currStage = 4;
        _currTime = 0;
        _currVolume = 0;
        _volumeBeforeCurrStage = 0;
    }

    public double Evaluate()
    {
        return _currVolume;
    }

    public double GetAttack() { return _attackTime; }
    public double GetDecay() { return _decayTime; }
    public double GetSustain() { return _sustainVolume; }
    public double GetRelease() { return _releaseTime; }

    public void SetADSR(double a, double d, double s, double r)
    {
        _attackTime = a;
        _decayTime = d;
        _sustainVolume = s;
        _releaseTime = r;
    }

    public void Update(double seconds)
    {
        if (_currStage == 0)
        {
            _currVolume = Engine.lerp(_volumeBeforeCurrStage, 1f, _currTime / _attackTime);
            _currTime += seconds;
            if (_currTime > _attackTime)
            {
                _currVolume = 1f;
                _volumeBeforeCurrStage = _currVolume;
                _currStage++;
                _currTime = 0f;
            }
        }

        if (_currStage == 1)
        {
            _currVolume = Engine.lerp(_volumeBeforeCurrStage, _sustainVolume, _currTime / _decayTime);
            _currTime += seconds;
            if (_currTime > _decayTime)
            {
                _currStage++;
                _currTime = 0f;
            }
        }

        if (_currStage == 2)
        {
            _currVolume = _sustainVolume;
        }

        if (_currStage == 3)
        {
            _currVolume = Engine.lerp(_volumeBeforeCurrStage, 0f, _currTime / _releaseTime);
            _currTime += seconds;
            if (_currTime > _releaseTime)
            {
                _currStage++;
                _currTime = 0f;
                _currVolume = 0f;
            }
        }
    }

    public void Trigger()
    {
        if (_triggered) return;
        _triggered = true;
        _currTime = 0f;
        if (_attackTime > Engine.SMALL_FLOAT)
        {
            _volumeBeforeCurrStage = _currVolume;
            _currStage = 0;
        }
        else if (_decayTime > Engine.SMALL_FLOAT)
        {
            _volumeBeforeCurrStage = 1f;
            _currStage = 1;
        }
        else
        {
            _currStage = 2;
        }
    }

    public void Untrigger()
    {

        if (_triggered)
        {
            _triggered = false;
            if (_releaseTime > Engine.SMALL_FLOAT)
            {
                _volumeBeforeCurrStage = _currVolume;
                _currStage = 3;
                _currTime = 0f;
            }
            else
            {
                _currVolume = 0;
                _currStage = 4;
            }
        }
    }

    public int GetStage()
    {
        return _currStage;
    }
}
