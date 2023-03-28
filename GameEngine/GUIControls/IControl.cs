using System;
using System.Drawing;

namespace GameEngine.GUIControls;


public interface IControl
{
    public GameWindow Parent { get; set; }
    public string ControlName { get; set; }
    public Rect ControlRect { get; }
    public int RawValue { get; }
    public float Value { get; }
    public float ValueMin { get; set; }
    public float ValueMax { get; set; }
    public bool HasFocus { get; set; }
    public bool Enabled { get; set; }
    public bool IsDragging { get; set; }

    public void Draw();
    public void Click(Coord mousePos);
    public void MouseUp();
    public void Update();

    public bool IsPointInControl(Coord pos)
    {
        bool output = false;

        if (ControlRect.IsPointInRect(pos))
        {
            output = true;
        }

        return output;
    }

    public void SetFocus(bool to)
    {
        HasFocus = to;
        return;
    }
}


