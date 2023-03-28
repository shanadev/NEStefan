using System;
using SDL2;

namespace GameEngine;

public class Time
{
    private ulong _lastTime = SDL.SDL_GetTicks64();
    private ulong _currentTime = SDL.SDL_GetTicks64();
    private float _fps = 0.0f;
    private ulong _deltaTime = 0;
    private ulong _totalFrames = 0;
    private float _totalTime = 0.0f;

    public float FPS { get { return _fps; } }
    public float DeltaTime { get { return (float)(_deltaTime) / 1000.0f; } }
    public ulong TotalFrames { get { return _totalFrames; } }
    public float TotalTime { get { return _totalTime; } }

    public void Tick()
    {
        _totalFrames++;
        _currentTime = SDL.SDL_GetTicks64();
        _deltaTime = _currentTime - _lastTime;
        _totalTime += _deltaTime;
        _fps = 1.0f / _deltaTime;
        _lastTime = _currentTime;
    }



    public Time()
    {
    }
}

