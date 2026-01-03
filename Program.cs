using System;
using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ConRender
{
    internal class Program
    {
        static void PrintHelp()
        {
            Console.WriteLine(
@"Usage:

  --help
      Show this help text

  --image <file> <color mode>
      Render an image file once

  --window <title> <color mode>
      Continuously render a window

  --cursor
      Enable mouse cursor rendering

  --no-info
      Disable info bar

  --default-print
      Disable optimized console writer

Available color mode values:");

            foreach (string name in Enum.GetNames<ColorMode>())
                Console.WriteLine($"  - {name}");
        }

        static void RenderWindow(string windowTitle, ColorMode colorMode)
        {
            Frame.ResizeFrame = true;
            Stopwatch sw = Stopwatch.StartNew();
            int frames = 0;
            double lastTime = 0;
            double fps = 0;
            while (true)
            {
                using var image = WindowCapture.CaptureWindow(windowTitle);
                Console.SetCursorPosition(0, 0);
                Frame.FastPrint(Frame.RenderImage(image, colorMode));

                if (Frame.RenderInfo)
                {
                    frames++;
                    double now = sw.Elapsed.TotalSeconds;

                    if (now - lastTime >= 1.0)
                    {
                        fps = frames / (now - lastTime);
                        frames = 0;
                        lastTime = now;

                        Frame.UpdateFps(fps);
                    }
                }
            }
        }

        static ColorMode ParseColorMode(string value)
        {
            if (!Enum.TryParse(value, true, out ColorMode mode))
                IO.ArgumentError($"Invalid color mode: {value}");
            return mode;
        }

        static void Main(string[] args)
        {
            bool showHelp = false;

            bool runImage = false;
            bool runWindow = false;

            string imageFile = string.Empty;
            string windowTitle = string.Empty;
            ColorMode mode = default;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "--help":
                        showHelp = true;
                        break;

                    case "--cursor":
                        WindowCapture.RenderMouseCursor = true;
                        break;

                    case "--image":
                        if (i + 2 >= args.Length)
                            IO.ArgumentError("Usage: --image <file> <color mode>");

                        imageFile = args[++i];
                        mode = ParseColorMode(args[++i]);
                        runImage = true;
                        break;

                    case "--no-info":
                        Frame.RenderInfo = false;
                        break;

                    case "--window":
                        if (i + 2 >= args.Length)
                            IO.ArgumentError("Usage: --window <title> <color mode>");
                        windowTitle = args[++i];
                        mode = ParseColorMode(args[++i]);
                        runWindow = true;
                        break;

                    case "--default-print":
                        Frame.DefaultPrint = true;
                        Console.OutputEncoding = new UTF8Encoding(false);
                        break;

                    default:
                        IO.ArgumentError($"Unknown argument: {arg}");
                        return;
                }
            }

            if (showHelp || (!runImage && !runWindow))
            {
                PrintHelp();
                return;
            }

            if (runImage)
            {
                Frame.FastPrint(Frame.RenderImageFile(imageFile, mode));
                return;
            }

            if (runWindow)
            {
                RenderWindow(windowTitle, mode);
                return;
            }
        }
    }
}
