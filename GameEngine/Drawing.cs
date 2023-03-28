using System;
using SDL2;
//using Serilog;

namespace GameEngine
{
    public partial class Engine
    {
        public void ClearScreen(IntPtr renderer, ScreenColor c)
        {
            SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
            SDL.SDL_RenderClear(renderer);
        }

        public void DrawLine(IntPtr renderer, int x1, int y1, int x2, int y2, ScreenColor c)
        {
            SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
            SDL.SDL_RenderDrawLine(renderer, x1, y1, x2, y2);
        }

        private SDL.SDL_Rect PrepQuad(int x1, int y1, int x2, int y2)
        {
            int tlx = 0, tly = 0, brx = 0, bry = 0;

            if (x1 > x2)
            {
                tlx = x2;
                brx = x1;
            }
            else
            {
                tlx = x1;
                brx = x2;
            }

            if (y1 > y2)
            {
                tly = y2;
                bry = y1;
            }
            else
            {
                tly = y1;
                bry = y2;
            }

            SDL.SDL_Rect rectum;
            rectum.x = tlx;
            rectum.y = tly;
            rectum.w = brx - tlx;
            rectum.h = bry - tly;

            return rectum;
        }

        public void DrawQuad(IntPtr renderer, int x1, int y1, int x2, int y2, ScreenColor c)
        {
            SDL.SDL_Rect rectum = PrepQuad(x1, y1, x2, y2);
            SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
            SDL.SDL_RenderDrawRect(renderer, ref rectum);
        }

        public void DrawQuadFilled(IntPtr renderer, int x1, int y1, int x2, int y2, ScreenColor c)
        {
            SDL.SDL_Rect rectum = PrepQuad(x1, y1, x2, y2);
            SDL.SDL_SetRenderDrawColor(renderer, c.red, c.green, c.blue, c.alpha);
            SDL.SDL_RenderFillRect(renderer, ref rectum);
        }

        public void DrawCircle(IntPtr renderer, int x, int y, int radius, ScreenColor c)
        {
            for (double i = 0; i <= 360; i += 0.1)
            {
                double angle = i;
                double x1 = radius * Math.Cos(angle * Math.PI / 180.0);
                double y1 = radius * Math.Sin(angle * Math.PI / 180.0);

                DrawPixel(renderer, x + ((int)Math.Round(x1)), y + ((int)Math.Round(y1)), c);
            }
        }

        public void DrawCircleFilled_Scanning(IntPtr renderer, int x, int y, int radius, ScreenColor c)
        {
            for (double i = 0; i <= 360; i += 0.1)
            {
                double angle = i;
                double x1 = radius * Math.Cos(angle * Math.PI / 180.0);
                double y1 = radius * Math.Sin(angle * Math.PI / 180.0);

                DrawLine(renderer, x - ((int)Math.Round(x1)), y + ((int)Math.Round(y1)), x + ((int)Math.Round(x1)), y + ((int)Math.Round(y1)), c);
            }
        }

        public void DrawPixel(IntPtr renderer, int x, int y, ScreenColor col)
        {
            SDL.SDL_SetRenderDrawColor(renderer, col.red, col.green, col.blue, col.alpha);
            SDL.SDL_RenderDrawPoint(renderer, x, y);
        }

        public void DrawSprite(IntPtr renderer, Sprite spr, int x, int y, Flip flip)
        {
            int fxs = 0, fxm = 1, fx = 0;
            int fys = 0, fym = 1, fy = 0;

            if (flip == Flip.HORIZ) { fxs = spr.Width - 1; fxm = -1; }
            if (flip == Flip.VERT) { fys = spr.Width - 1; fym = -1; }

            fx = fxs;
            for (int i = 0; i < spr.Width; i++, fx += fxm)
            {
                fy = fys;
                for (int j = 0; j < spr.Height; j++, fy += fym)
                {
                    ScreenColor px = spr.GetPixel(fx, fy);
                    DrawPixel(renderer, x + i, y + j, px);

                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="x">top left corner</param>
        /// <param name="y">top left corner</param>
        /// <param name="width">This should match the buffer size</param>
        /// <param name="height">waveform will stretch to the edges (+1, -1)</param>
        /// <param name="buffer">The size of this buffer should match the width</param>
        public void DrawAudioBuffer(IntPtr renderer, int tlx, int tly, int width, int height, ScreenColor color, float[] buffer, float amp)
        {
            //float max = Math.Abs(buffer.Max());
            DrawQuad(renderer, tlx, tly, tlx + width, tly + height, color);
            int lastPlot = 0;
            for (int x = 0; x < width; x++)
            {
                //if (buffer[x] == float.NaN) continue;
                float plot = (buffer[x] * (height / 2));
                if (amp > 0) plot = plot * amp;
                int iPlot = Convert.ToInt32(plot);
                int halfHeight = (height / 2);
                if (iPlot > halfHeight) iPlot = halfHeight;
                if (iPlot < -halfHeight) iPlot = -halfHeight;
                DrawLine(renderer, x + tlx - 1, -lastPlot + tly + halfHeight, x + tlx, -iPlot + tly + halfHeight, new ScreenColor(255, 0, 0, 255));
                lastPlot = iPlot;
                //Log.Debug(iPlot.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferToFill">Should contian all past values that will be shifted by new information</param>
        /// <param name="newBufferInformation">New buffer data to shift what is in buffertofill</param>
        /// <returns></returns>
        public float[] FillBuffer(float[] bufferToFill, float[] newBufferInformation)
        {
            int newBufferSize = newBufferInformation.Count();
            int fillBufferSize = bufferToFill.Count();
            float[] outputBuffer = new float[fillBufferSize];
            if (newBufferSize >= fillBufferSize)
            {
                Array.Copy(newBufferInformation, outputBuffer, fillBufferSize);
            }
            else
            {
                Array.Copy(newBufferInformation, outputBuffer, newBufferSize);
                Array.Copy(bufferToFill, 0, outputBuffer, newBufferSize, fillBufferSize - newBufferSize);
            }
            return outputBuffer;
        }

        public static (int, int) GamePixelToScreenPixel(int x, int y, int pixelSize)
        {
            (int x, int y) output = (x * pixelSize, y * pixelSize);
            return output;
        }

        public static int GetScreenPixelIndex(int x, int y, int screenWidth)
        {
            int output = (y * screenWidth) + x;
            return output;
        }

        public static bool PointInRadius(int objX, int objY, int objRad, int checkX, int checkY)
        {
            bool output = false;

            if (GetDistance(objX, objY, checkX, checkY) <= objRad)
            {
                output = true;
            }

            return output;
        }

        public static double GetDistance(int x1, int y1, int x2, int y2)
        {
            // Pathagyrus
            double part1 = (x2 - x1) * (x2 - x1);
            double part2 = (y2 - y1) * (y2 - y1);

            return Math.Sqrt(part1 + part2);
        }

    }
}

