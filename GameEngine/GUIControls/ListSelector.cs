using System;
namespace GameEngine.GUIControls
{
	public class ListSelector : IControl
	{
        private Engine _engine;
        private GameWindow _parent;
        private string _name;
        private Rect _rect;

        private List<string> _list;
        private ScreenColor _color;
        public ListSelector(Engine eng, GameWindow win, string name, Rect rect, List<string>? list, ScreenColor color)
		{
            _engine = eng;
            _parent = win;
            _name = name;
            _rect = rect;
            if (list != null) _list = list;
            else _list = new List<string>();
            _color = color;
		}

        public GameWindow Parent { get { return this._parent; } set { this._parent = value; } }
        public string ControlName { get { return this._name; } set { this._name = value; } }

        public Rect ControlRect { get { return this._rect; } set { this._rect = value; } }

        public int RawValue { get; set; } = 0;

        public float Value { get; set; }
        public string SValue { get; set; } = string.Empty;

        public float ValueMin { get; set; } = 0;
        public float ValueMax { get { return _list.Count() - 1; } set { return; } }
        public bool HasFocus { get; set; } = false;
        public bool Enabled { get; set; } = true;
        public bool IsDragging { get; set; } = false;


        private int _displayIndex = 0;
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;

        public void Click(Coord mousePos)
        {
            // select the item the mouse is over
            if (_hoverIndex > 0)
            {
                _selectedIndex = _hoverIndex;
                this.RawValue = _selectedIndex;
                this.SValue = _list[_selectedIndex];
            }
        }

        public void Draw()
        {
            // draw the items starting at the displayindex
            // draw the rectangle first
            _engine.DrawQuad(_parent.Renderer, _rect.topLeft.x, _rect.topLeft.y, _rect.bottomRight.x, _rect.bottomRight.y, _color);

            // Now list the list contents starting at the display index]]
            Coord mousePos = _engine.mouse.GetMousePos();

            int linesize = 12;
            int margin = 3;
            int ypos = _rect.topLeft.y + margin;
            int listindex = _displayIndex;
            _hoverIndex = -1;

            while (ypos < (_rect.topLeft.y + _rect.GetHeight()) - margin && listindex < _list.Count())
            {
            
                if (_list[listindex].Length * 8 > _rect.bottomRight.x - _rect.topLeft.x)
                {
                    int charcount = ((_rect.bottomRight.x - _rect.topLeft.x) / 8) - 1;
                    _list[listindex] = _list[listindex].Substring(0, charcount);
                }

                
                Rect singleLineRect = new Rect(_rect.topLeft.x, ypos, _rect.GetWidth(), linesize-1);
                ScreenColor useThisColor;
                string map = _list[listindex].Substring(0, 3);

                if (listindex == _selectedIndex)
                {
                    useThisColor = new ScreenColor(255, 0, 0, 255);
                }
                else if (singleLineRect.IsPointInRect(mousePos))
                {
                    useThisColor = new ScreenColor(255, 255, 0, 255);
                    _hoverIndex = listindex;
                }
                else if (map == "0  " ||
                    map == "1  " ||
                    map == "2  " ||
                    map == "3  " ||
                    map == "4  ")
                {
                    useThisColor = _color;
                }

                else
                {
                    useThisColor = new ScreenColor(100,100,100,255);
                }


                //ScreenColor useThisColor = singleLineRect.IsPointInRect(mousePos) ? new ScreenColor(255, 255, 0, 255) : _color;
                _engine.DrawText(_parent.Renderer, _rect.topLeft.x + margin, ypos, _list[listindex], useThisColor);
                ypos += linesize;
                listindex++;
            }

            if (SValue != string.Empty)
            {
                _engine.DrawText(_parent.Renderer, _rect.topLeft.x, _rect.bottomRight.y + 10, $"Selected: {SValue}", _color);
            }

        }

        public void Update()
        {
            //Coord mousePos = _engine.mouse.GetMousePos();
            int scrollamt = _engine.mouse.ScrollAmount;
            if (this.HasFocus && scrollamt != 0)
            {
                if (scrollamt > 0) // up
                {
                    _displayIndex -= Math.Abs(scrollamt);
                    if (_displayIndex < 0) _displayIndex = 0;
                }
                else if (scrollamt < 0) // down
                {
                    _displayIndex += Math.Abs(scrollamt);
                    if (_displayIndex >= _list.Count()) _displayIndex = _list.Count() - 1;
                }
            }
            _engine.mouse.ScrollAmount = 0;

            if (_engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_PAGEDOWN))
            {
                _displayIndex += 15;
                if (_displayIndex >= _list.Count()) _displayIndex = _list.Count() - 1;
            }

            if (_engine.keyboard.WasKeyPressed(SDL2.SDL.SDL_Keycode.SDLK_PAGEUP))
            {
                _displayIndex -= 15;
                if (_displayIndex < 0) _displayIndex = 0;
            }

        }

        public void MouseUp()
        {
            // stop dragging
        }

        public void KeyScrollUp()
        {
            // for manual scrolling
        }

        public void KeyScrollDown()
        {
            // for manual scrolling
        }


    }
}

