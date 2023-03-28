using System;
namespace GameEngine.GUIControls;

public class GUI
{
    private Engine engine;
    private GameWindow parent;

    public Dictionary<string, IControl> Controls = new Dictionary<string, IControl>();

    private string focusedControlKey { get; set; } = string.Empty;
    private string clickedControlKey { get; set; } = string.Empty;

    public bool Clicked { get; set; } = false;

    public GUI(Engine engine, GameWindow parentWindow)
    {
        this.engine = engine;
        this.parent = parentWindow;
    }

    public void AddControl(IControl control)
    {
        Controls.Add(control.ControlName, control);
    }

    public void RemoveControl(string controlKey)
    {
        Controls.Remove(controlKey);
    }

    public void Draw()
    {
        if (Controls.Count > 0)
        {
            foreach (var control in Controls)
            {
                control.Value.Draw();
            }
        }
    }

    public void SetFocus(string controlKey)
    {
        focusedControlKey = controlKey;
        Controls[controlKey].HasFocus = true;
    }

    public string CheckForFocus(Coord pos)
    {
        string output = string.Empty;

        foreach (var control in Controls)
        {
            if (control.Value.IsPointInControl(pos))
            {
                SetFocus(control.Value.ControlName);
                output = control.Value.ControlName;
            }
        }

        return output;
    }

    public void Update()
    {
        if (this.parent.hasMouseFocus())
        {

            foreach (var control in Controls)
            {
                control.Value.Update();
            }

            Coord mousePos = engine.mouse.GetMousePos();
            bool mouseClicked = engine.mouse.Button1Clicked;

            if (!Clicked)
            {
                if (focusedControlKey != string.Empty)
                {
                    bool isStillInFocus = Controls[focusedControlKey].IsPointInControl(mousePos);

                    if (!isStillInFocus)
                    {
                        Controls[focusedControlKey].HasFocus = false;
                        focusedControlKey = CheckForFocus(mousePos);
                    }
                }
                else
                {
                    focusedControlKey = CheckForFocus(mousePos);
                }
            }

            if (mouseClicked && focusedControlKey != "")
            {
                Click(mousePos);
            }
            else if (!mouseClicked && Clicked)
            {
                MouseUp();
            }
        }
    }


    public void Click(Coord mousePos)
    {
        clickedControlKey = focusedControlKey;
        Controls[clickedControlKey].Click(mousePos);
        Clicked = true;
    }

    public void MouseUp()
    {
        if (Clicked)
        {
            Controls[clickedControlKey].MouseUp();
            clickedControlKey = string.Empty;
            Clicked = false;
        }
    }
}

