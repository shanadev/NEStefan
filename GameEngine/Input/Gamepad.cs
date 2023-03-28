using System;
using SDL2;

namespace GameEngine.Input;


public enum ControllerButton
{
    A = 0, B = 2, Start = 6, Select = 4, Up = 11, Down = 12, Left = 13, Right = 14
}



public class Gamepad
{
    Engine _engine;

    public bool noGamepad = true;


    public bool a_pressed = false;

    public bool b_pressed = false;

    public bool sel_pressed = false;

    public bool st_pressed = false;

    public bool up_pressed = false;

    public bool down_pressed = false;

    public bool left_pressed = false;

    public bool right_pressed = false;


	public Gamepad(Engine eng)
	{
        _engine = eng;
	}

    public void HandleEvents(SDL.SDL_Event eventToHandle)
    {
        if (!noGamepad)
        {
            switch (eventToHandle.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    //Console.WriteLine((ControllerButton)eventToHandle.cbutton.button);
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.A) a_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.B) b_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Select) sel_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Start) st_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Up) up_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Down) down_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Left) left_pressed = true;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Right) right_pressed = true;
                    break;
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.A) a_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.B) b_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Select) sel_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Start) st_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Up) up_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Down) down_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Left) left_pressed = false;
                    if ((ControllerButton)eventToHandle.cbutton.button == ControllerButton.Right) right_pressed = false;
                    break;
            }
        }
    }
}

