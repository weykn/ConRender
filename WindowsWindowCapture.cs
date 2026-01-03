using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ConRender
{
    public static class WindowsWindowCapture
    {
        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIconEx(
            IntPtr hdc,
            int xLeft,
            int yTop,
            IntPtr hIcon,
            int cxWidth,
            int cyHeight,
            int istepIfAniCur,
            IntPtr hbrFlickerFreeDraw,
            int diFlags);

        const int CURSOR_SHOWING = 0x00000001;
        const int DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        public static IntPtr GetWindowByTitle(string windowTitle)
        {
            return FindWindow(null, windowTitle);
        }

        public static Bitmap CaptureWindowAsBitmap(IntPtr hwnd, bool drawCursor)
        {
            if (hwnd == IntPtr.Zero)
                throw new Exception("Window not found");

            if (!GetWindowRect(hwnd, out var rect))
                throw new Exception("GetWindowRect failed");

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                throw new Exception("Invalid window size");

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using var gfx = Graphics.FromImage(bmp);
            var hdc = gfx.GetHdc();

            PrintWindow(hwnd, hdc, 0);

            if (drawCursor)
                DrawCursor(hwnd, rect, hdc);

            gfx.ReleaseHdc(hdc);
            return bmp;
        }

        static void DrawCursor(IntPtr hwnd, RECT windowRect, IntPtr hdc)
        {
            var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
            if (!GetCursorInfo(out ci))
                return;

            if ((ci.flags & CURSOR_SHOWING) == 0)
                return;

            int x = ci.ptScreenPos.X - windowRect.Left;
            int y = ci.ptScreenPos.Y - windowRect.Top;

            DrawIconEx(
                hdc,
                x,
                y,
                ci.hCursor,
                0,
                0,
                0,
                IntPtr.Zero,
                DI_NORMAL);
        }
    }
}
