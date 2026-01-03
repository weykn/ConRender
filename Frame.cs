using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ConRender
{
    public enum ColorMode
    {
        TrueColor,
        Ansi256,
        Ansi16
    }

    public static class Frame
    {
        static readonly Stream Stdout = Console.OpenStandardOutput();
        static readonly Encoding Utf8 = Encoding.UTF8;

        public static bool RenderInfo = true;
        public static bool ResizeFrame = false;
        public static bool DefaultPrint = false;

        static double LastFps;
        static double AvgFps;
        static bool AvgFpsInit;
        const double AvgFpsAlpha = 0.1;

        static double LastRenderMs;
        static double LastPrintMs;
        static double RenderOnlyFps;

        static int LastSrcW, LastSrcH;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte RgbToAnsi256(byte r, byte g, byte b)
        {
            r /= 51;
            g /= 51;
            b /= 51;
            return (byte)(16 + 36 * r + 6 * g + b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte RgbToAnsi16(byte r, byte g, byte b)
        {
            int idx = 0;
            if (r > 128) idx |= 1;
            if (g > 128) idx |= 2;
            if (b > 128) idx |= 4;
            if (r + g + b > 384) idx |= 8;
            return (byte)idx;
        }

        public static Image<Rgba32> ResizeImage(Image<Rgba32> source, int width, int height)
        {
            var img = source.Clone();
            img.Mutate(c =>
                c.Resize(new ResizeOptions
                {
                    Size = new Size(width, height * 2),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.NearestNeighbor
                }));
            return img;
        }

        static void AutoResize(ref Image<Rgba32> map)
        {
            int h = Console.BufferHeight - (RenderInfo ? 1 : 0);
            map = ResizeImage(map, Console.BufferWidth, h);
        }

        public static void FastPrint(string data)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (DefaultPrint)
            {
                Console.Write(data);
            }
            else
            {
                byte[] bytes = Utf8.GetBytes(data);
                Stdout.Write(bytes, 0, bytes.Length);
            }

            sw.Stop();
            LastPrintMs = sw.Elapsed.TotalMilliseconds;
        }

        public static string RenderImage(Image<Rgba32> map, ColorMode mode)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (ResizeFrame)
                AutoResize(ref map);

            LastSrcW = map.Width;
            LastSrcH = map.Height;

            int infoRows = RenderInfo ? 1 : 0;
            int maxPixelHeight = (Console.BufferHeight - infoRows) * 2;
            int renderHeight = Math.Min(map.Height, maxPixelHeight);

            var sb = new StringBuilder(map.Width * renderHeight * 6);

            if (RenderInfo)
            {
                sb.Append("\x1b[0m\x1b[41m\x1b[30m");
                string info =
                    $" FPS: {LastFps:0.0} | AVG: {AvgFps:0.0} | RENDER FPS: {RenderOnlyFps:0.0} | RENDER TIME: {LastRenderMs:0.00}ms | WRITE TIME: {LastPrintMs:0.00}ms | RES: {LastSrcW}x{LastSrcH} | MODE: {mode} ";
                sb.Append(info.PadRight(Console.BufferWidth));
                sb.Append("\x1b[0m\n");
            }

            for (int y = 0; y + 1 < renderHeight; y += 2)
            {
                int lastFg = -1, lastBg = -1, run = 0;

                for (int x = 0; x < map.Width; x++)
                {
                    Rgba32 t = map[x, y];
                    Rgba32 b = map[x, y + 1];

                    int fg = mode == ColorMode.TrueColor
                        ? (t.R << 16) | (t.G << 8) | t.B
                        : mode == ColorMode.Ansi256
                            ? RgbToAnsi256(t.R, t.G, t.B)
                            : RgbToAnsi16(t.R, t.G, t.B);

                    int bg = mode == ColorMode.TrueColor
                        ? (b.R << 16) | (b.G << 8) | b.B
                        : mode == ColorMode.Ansi256
                            ? RgbToAnsi256(b.R, b.G, b.B)
                            : RgbToAnsi16(b.R, b.G, b.B);

                    if (fg == lastFg && bg == lastBg)
                    {
                        run++;
                        continue;
                    }

                    if (run > 0)
                        sb.Append('▀', run);

                    if (mode == ColorMode.TrueColor)
                    {
                        sb.Append("\x1b[38;2;")
                          .Append(t.R).Append(';').Append(t.G).Append(';').Append(t.B)
                          .Append("m\x1b[48;2;")
                          .Append(b.R).Append(';').Append(b.G).Append(';').Append(b.B)
                          .Append('m');
                    }
                    else if (mode == ColorMode.Ansi256)
                    {
                        sb.Append("\x1b[38;5;").Append(fg)
                          .Append("m\x1b[48;5;").Append(bg).Append('m');
                    }
                    else
                    {
                        sb.Append("\x1b[").Append(30 + (fg & 7));
                        if ((fg & 8) != 0) sb.Append(";1");
                        sb.Append("m\x1b[").Append(40 + (bg & 7));
                        if ((bg & 8) != 0) sb.Append(";1");
                        sb.Append('m');
                    }

                    lastFg = fg;
                    lastBg = bg;
                    run = 1;
                }

                if (run > 0)
                    sb.Append('▀', run);

                if (!ResizeFrame)
                    sb.Append('\n');
            }

            sb.Append("\x1b[0m");

            sw.Stop();
            LastRenderMs = sw.Elapsed.TotalMilliseconds;
            RenderOnlyFps = LastRenderMs > 0 ? 1000.0 / LastRenderMs : 0;

            return sb.ToString();
        }

        public static void UpdateFps(double fps)
        {
            LastFps = fps;

            if (!AvgFpsInit)
            {
                AvgFps = fps;
                AvgFpsInit = true;
            }
            else
            {
                AvgFps = AvgFps + (fps - AvgFps) * AvgFpsAlpha;
            }
        }

        public static string RenderImageFile(string fileName, ColorMode mode)
        {
            using var map = Image.Load<Rgba32>(fileName);
            return RenderImage(map, mode);
        }
    }
}
