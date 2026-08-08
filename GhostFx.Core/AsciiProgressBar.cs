using System;

namespace GhostFx.Core;

public static class AsciiProgressBar
{
    public static string GenerateBar(int current, int total, int width = 30)
    {
        if (total <= 0) return $"[{new string('=', width)}] 100%";

        double percentage = Math.Clamp((double)current / total, 0.0, 1.0);
        int percentInt = (int)(percentage * 100);
        int filledWidth = (int)Math.Round(percentage * width);

        string filled;
        if (filledWidth == 0)
        {
            filled = new string('.', width);
        }
        else if (filledWidth >= width)
        {
            filled = new string('=', width);
        }
        else
        {
            filled = new string('=', filledWidth - 1) + ">" + new string('.', width - filledWidth);
        }

        return $"[{filled}] {percentInt,3}% ({current}/{total})";
    }

    public static void Draw(int current, int total, string currentItem = "", int width = 30)
    {
        string bar = GenerateBar(current, total, width);
        string displayItem = string.IsNullOrWhiteSpace(currentItem) ? "" : $" - {currentItem}";

        if (Console.IsOutputRedirected)
        {
            Console.WriteLine($"[PROGRESS] {bar}{displayItem}");
        }
        else
        {
            string line = $"{bar}{displayItem}";
            int pad = 80;
            try
            {
                if (Console.WindowWidth > 1) pad = Console.WindowWidth - 1;
            }
            catch
            {
                // Fallback if window width is unavailable
            }

            Console.Write($"\r{line.PadRight(Math.Min(pad, 120))}");
            if (current >= total)
            {
                Console.WriteLine();
            }
        }
    }
}
