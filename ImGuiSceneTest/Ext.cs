using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImGuiSceneTest {
    public static class EXT {
        public static uint ToImgui(this Color c) {
            int value = c.R;
            value |= c.G << 8;
            value |= c.B << 16;
            value |= c.A << 24;

            return (uint)value;
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        public static Rectangle GetWindowRectangle(IntPtr ptr) {
            RECT rect;
            GetWindowRect(ptr, out rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }

        public static float GetScreenScalingFactor() {
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

            return ScreenScalingFactor; // 1.25 = 125%
        }
    }

   
}
