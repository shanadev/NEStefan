using System;
using SDL2;

namespace GameEngine.Input;

public class Mouse
{
    Engine engine;

    private bool _button1Clicked = false;
    private bool _button2Clicked = false;
    private bool _button3Clicked = false;

    private int _scrollAmount = 0;

    public bool Button1Clicked { get { return _button1Clicked; } }
    public bool Button2Clicked { get { return _button2Clicked; } }
    public bool Button3Clicked { get { return _button3Clicked; } }

    public int ScrollAmount { get { return _scrollAmount; } set { _scrollAmount = value; } }

    public Mouse(Engine eng)
    {
        engine = eng;
    }


    public void HandleEvents(SDL.SDL_Event eventToHandle)
    {
        //Console.WriteLine($"event: {eventToHandle.type}");
        switch (eventToHandle.type)
        {
            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                //Console.WriteLine($"button: {eventToHandle.button.button}");
                switch (eventToHandle.button.button)
                {
                    case 1:
                        _button1Clicked = true;
                        break;
                    case 2:
                        _button2Clicked = true;
                        break;
                    case 3:
                        _button3Clicked = true;
                        break;
                }
                break;
            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                switch (eventToHandle.button.button)
                {
                    case 1:
                        _button1Clicked = false;
                        break;
                    case 2:
                        _button2Clicked = false;
                        break;
                    case 3:
                        _button3Clicked = false;
                        break;
                }
                break;
            case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                if (eventToHandle.wheel.y > 0) // scroll up
                {
                    _scrollAmount = eventToHandle.wheel.y;
                }
                else if (eventToHandle.wheel.y < 0) // scroll down
                {
                    _scrollAmount = eventToHandle.wheel.y;
                }
                break;
        }
    }



    //public (int, int) GetMousePos()
    //{
    //    int x, y;
    //    uint btn = SDL.SDL_GetMouseState(out x, out y);
    //    return (x / windowSize.PixelSize, y / windowSize.PixelSize);
    //}

    public IntPtr GetMouseFocus()
    {
        IntPtr output = IntPtr.Zero;

        output = SDL.SDL_GetMouseFocus();

        return output;
    }


    public Coord GetMousePos()
    {
        Coord output;

        int x, y;
        
        uint btn = SDL.SDL_GetMouseState(out x, out y);

        output.x = x;// / engine.windowSize.PixelSize;
        output.y = y;// / engine.windowSize.PixelSize;

        return output;
    }
}

