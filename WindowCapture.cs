using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ConRender
{
    public static class WindowCapture
    {
        public static bool RenderMouseCursor = false;

        public static Image<Rgba32> CaptureWindow(string windowTitle)
        {
            IntPtr hwnd = WindowsWindowCapture.GetWindowByTitle(windowTitle);
            Bitmap bmp = WindowsWindowCapture.CaptureWindowAsBitmap(hwnd, RenderMouseCursor);

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            bmp.Dispose();
            return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
        }
    }
}
