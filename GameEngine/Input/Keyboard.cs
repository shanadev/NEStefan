using System;
using SDL2;
//using GameEngine;

namespace GameEngine.Input;

public class Keyboard
{
    Engine engine;

    private const int _maxKeys = 128;
    private int[] _keyPressed = new int[_maxKeys];
    private bool[] _keyDown = new bool[_maxKeys];


    public Keyboard(Engine eng)
    {
        engine = eng;
        for (int i = 0; i < _maxKeys; i++)
        {
            _keyPressed[i] = 0;
            _keyDown[i] = false;
        }
    }


    public void HandleEvents(SDL.SDL_Event eventToHandle)
    {
        int keyCode = ((int)eventToHandle.key.keysym.sym);
        //Console.WriteLine($"{keyCode}");
        switch (eventToHandle.type)
        {

            case SDL.SDL_EventType.SDL_KEYDOWN:

                //Console.WriteLine($"{keyCode}");
                if (keyCode < _maxKeys)
                {
                    _keyDown[keyCode] = true;
                    if (_keyPressed[keyCode] == 0)
                    {
                        _keyPressed[keyCode] = 1;
                    }
                    else
                    {
                        _keyPressed[keyCode] = 2;
                    }
                }

                break;
            case SDL.SDL_EventType.SDL_KEYUP:
                if (keyCode < _maxKeys)
                {
                    _keyDown[keyCode] = false;
                    _keyPressed[keyCode] = 0;
                }


                break;


        }


    }

    public bool WasKeyPressed(SDL.SDL_Keycode key)
    {
        bool output = false;
        if ((int)key < 128)
        {
            if (_keyPressed[(int)key] == 1)
            {
                _keyPressed[(int)key] = 2;
                output = true;
            }
        }
        return output;
    }

    public bool IsKeyDown(SDL.SDL_Keycode key)
    {
        bool output = false;

        if ((int)key < 128)
        {
            output = _keyDown[(int)key];
        }
        return output;
    }

    public bool IsKeyUp(SDL.SDL_Keycode key)
    {
        bool output = true;

        if ((int)key < 128)
        {
            output = !_keyDown[(int)key];
        }
        return output;
    }

}

