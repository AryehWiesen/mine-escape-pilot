using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

static partial class GameEngine
{
    private const int StatusWidth = 61;
    private const int InterfaceRows = 5;

    // Use symbols that typically occupy two terminal columns in emoji mode.
    private static readonly string[] Emoji = { "⬛", "🧱", "💎", "🚪", "🧑", "😈", "⛏️", "✨", "💥" };
    private static readonly string[] Ascii = { " .", "##", " *", " E", " @", " C", " P", " +", " !" };

    internal static void Run(bool ascii)
    {
        int level = 1;
        int unlocked = 1;
        var round = new Round(LevelMap(level), level);
        if (OperatingSystem.IsWindows())
            ClassicConsole.Fit(RequiredWidth(round), round.Board.GetLength(0) + InterfaceRows);
        Console.Clear();

        try
        {
            Draw(round, ascii, level, unlocked, Spark);
            Thread.Sleep(180);
            while (true)
            {
                bool frameFits = Draw(round, ascii, level, unlocked);
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Escape)
                    break;

                if (key == ConsoleKey.F2)
                {
                    ascii = !ascii;
                    Console.Clear();
                    continue;
                }

                int requestedLevel = key switch
                {
                    ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
                    ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
                    ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
                    _ => 0
                };
                if (requestedLevel > 0)
                {
                    if (requestedLevel > unlocked)
                        round.Message = $"Finish level {requestedLevel - 1} to unlock level {requestedLevel}.";
                    else
                    {
                        level = requestedLevel;
                        round = new Round(LevelMap(level), level);
                        Console.Clear();
                    }
                    continue;
                }

                if (key == ConsoleKey.R)
                {
                    if (round.Finished)
                        Console.Clear();
                    round.Reset();
                    continue;
                }

                if (!frameFits || round.Finished)
                    continue;

                var (deltaRow, deltaCol) = key switch
                {
                    ConsoleKey.W or ConsoleKey.UpArrow => (-1, 0),
                    ConsoleKey.A or ConsoleKey.LeftArrow => (0, -1),
                    ConsoleKey.S or ConsoleKey.DownArrow => (1, 0),
                    ConsoleKey.D or ConsoleKey.RightArrow => (0, 1),
                    _ => (0, 0)
                };
                if (deltaRow == 0 && deltaCol == 0)
                    continue;

                int effect = round.Move(deltaRow, deltaCol);
                if (effect != 0)
                {
                    Draw(round, ascii, level, unlocked, effect);
                    Thread.Sleep(effect == Burst ? 220 : 130);
                }
                if (round.Won && level < FinalLevel)
                {
                    Draw(round, ascii, level, unlocked);
                    Thread.Sleep(1800);
                    level++;
                    unlocked = Math.Max(level, unlocked);
                    round = new Round(LevelMap(level), level);
                    Console.Clear();
                }
            }
        }
        finally
        {
            Console.Clear();
        }
    }

    private static int RequiredWidth(Round round) =>
        Math.Max(StatusWidth, 2 * round.Board.GetLength(1)) + 1;

    private static bool Draw(Round round, bool ascii, int level, int unlocked, int effect = 0)
    {
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;
        int rows = round.Board.GetLength(0) + InterfaceRows;
        int columns = RequiredWidth(round);
        if (width > 0 && height > 0 && (width < columns || height < rows))
        {
            Console.Clear();
            string hint = $"Resize or zoom out: {columns} columns x {rows} rows. Esc quits.";
            Console.Write(hint[..Math.Min(hint.Length, width - 1)]);
            return false;
        }
        if (round.Finished && effect == 0)
        {
            DrawResult(round.Won, ascii, level, rows, width > 0 ? width : columns);
            return true;
        }

        var symbols = ascii ? Ascii : Emoji;
        string creeper = ascii ? Ascii[Creeper] : level switch
        {
            1 => Emoji[Creeper],
            2 => "👺",
            _ => "👹"
        };
        var frame = new StringBuilder();
        string mode = level switch { 1 => "patrol", 2 => "chase", _ => "chase x1.5" };
        frame.AppendLine(($"MINE ESCAPE | Level {level} | Creeper: {mode}").PadRight(StatusWidth));
        frame.AppendLine($"Diamonds left: {round.Diamonds} | Pickaxe uses: {round.PickaxeUses}");

        for (int row = 0; row < round.Board.GetLength(0); row++)
        {
            for (int col = 0; col < round.Board.GetLength(1); col++)
            {
                int tile = round.Board[row, col];
                if (row == round.CreeperRow && col == round.CreeperCol)
                    tile = Creeper;
                if (row == round.PlayerRow && col == round.PlayerCol)
                    tile = effect == 0 ? Player : effect;

                frame.Append(tile == Creeper ? creeper : symbols[tile]);
            }
            frame.AppendLine();
        }

        frame.AppendLine(round.Message.PadRight(StatusWidth));
        frame.AppendLine("WASD/arrows Move | R Retry | F2 Symbols | Esc Quit");
        frame.Append($"Replay unlocked levels: {(unlocked == 1 ? "1" : $"1-{unlocked}")}");
        Console.SetCursorPosition(0, 0);
        Console.Write(frame.ToString());
        return true;
    }

    private static void DrawResult(bool won, bool ascii, int level, int rows, int width)
    {
        const int innerWidth = 46;
        int left = (width - innerWidth - 2) / 2;
        int top = (rows - 7) / 2;
        string margin = new(' ', left);
        char[] border = ascii ? "+-+|++".ToCharArray() : "╔═╗║╚╝".ToCharArray();

        ConsoleColor accent;
        string title;
        string subtitle;
        string footer;
        int titleWidth;
        if (!won)
        {
            accent = ConsoleColor.Red;
            title = "GAME OVER";
            titleWidth = title.Length + (ascii ? 0 : 4);
            if (!ascii)
                title = $"☠️  {title}";
            subtitle = "The creeper caught you.";
            footer = "R  Retry   |   Esc  Quit";
        }
        else if (level == FinalLevel)
        {
            accent = ConsoleColor.Yellow;
            title = "VICTORY!";
            titleWidth = title.Length + (ascii ? 0 : 8);
            if (!ascii)
                title = $"🔥  {title}  👑";
            subtitle = "You escaped all three levels!";
            footer = "R  Replay   |   1-3  Select level   |   Esc";
        }
        else
        {
            accent = ConsoleColor.Cyan;
            title = "LEVEL PASSED!";
            titleWidth = title.Length;
            subtitle = "Prepare for next challenge";
            footer = $"Next up: Level {level + 1}";
        }

        void WriteRow(string text, ConsoleColor color, int visibleWidth)
        {
            int padding = innerWidth - visibleWidth;
            Console.ForegroundColor = accent;
            Console.Write($"{margin}{border[3]}{new string(' ', padding / 2)}");
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = accent;
            Console.WriteLine($"{new string(' ', padding - padding / 2)}{border[3]}");
        }

        ConsoleColor previous = Console.ForegroundColor;
        try
        {
            Console.Clear();
            Console.SetCursorPosition(0, top);
            string rule = new(border[1], innerWidth);
            Console.ForegroundColor = accent;
            Console.WriteLine($"{margin}{border[0]}{rule}{border[2]}");
            WriteRow("", accent, 0);

            WriteRow(title, accent, titleWidth);
            WriteRow(subtitle, ConsoleColor.White, subtitle.Length);
            WriteRow("", accent, 0);
            WriteRow(footer, ConsoleColor.Gray, footer.Length);
            Console.WriteLine($"{margin}{border[4]}{rule}{border[5]}");
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }

    // This Win32 code only applies to a visible, classic Windows Console Host.
    [SupportedOSPlatform("windows")]
    private static class ClassicConsole
    {
        internal static void Fit(int columns, int rows)
        {
            IntPtr window = GetConsoleWindow();
            if (window == IntPtr.Zero || !IsWindowVisible(window) || IsZoomed(window))
                return;

            IntPtr output = GetStdHandle(-11); // STD_OUTPUT_HANDLE
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            var original = new ConsoleFont { Size = Marshal.SizeOf<ConsoleFont>() };
            if (!GetMonitorInfo(MonitorFromWindow(window, 2), ref info) ||
                !GetWindowRect(window, out Rect outer) ||
                !GetClientRect(window, out Rect client) ||
                !GetCurrentConsoleFontEx(output, false, ref original))
                return;

            Rect work = info.Work;
            int frameWidth = outer.Width - client.Width;
            int frameHeight = outer.Height - client.Height;

            // Test requested sizes against the actual cell dimensions selected by the host.
            // A small margin leaves room for window borders and a possible scrollbar.
            for (int height = Math.Max(36, (int)original.CellHeight); height >= 8; height--)
            {
                var font = original;
                font.CellWidth = 0;
                font.CellHeight = (short)height;
                if (!SetCurrentConsoleFontEx(output, false, ref font) ||
                    !GetCurrentConsoleFontEx(output, false, ref font) ||
                    font.CellWidth <= 0 || font.CellHeight <= 0)
                    continue;

                int availableWidth = (work.Width - frameWidth - 8) / font.CellWidth;
                int availableHeight = (work.Height - frameHeight - 4) / font.CellHeight;
                Coord limit = GetLargestConsoleWindowSize(output);
                if (Math.Min(availableWidth, limit.X) < columns ||
                    Math.Min(availableHeight, limit.Y) < rows)
                    continue;

                // Move and resize the normal window within the monitor's work area.
                if (MoveWindow(window, work.Left, work.Top, work.Width, work.Height, true) &&
                    Console.WindowWidth >= columns && Console.WindowHeight >= rows)
                    return;
            }

            SetCurrentConsoleFontEx(output, false, ref original);
            MoveWindow(window, outer.Left, outer.Top, outer.Width, outer.Height, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Coord { public short X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left, Top, Right, Bottom;
            public readonly int Width => Right - Left;
            public readonly int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public Rect Monitor, Work;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ConsoleFont
        {
            public int Size;
            public uint Index;
            public short CellWidth, CellHeight;
            public int Family, Weight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll")]
        private static extern bool GetCurrentConsoleFontEx(IntPtr output, bool maximum, ref ConsoleFont font);

        [DllImport("kernel32.dll")]
        private static extern bool SetCurrentConsoleFontEx(IntPtr output, bool maximum, ref ConsoleFont font);

        [DllImport("kernel32.dll")]
        private static extern Coord GetLargestConsoleWindowSize(IntPtr output);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr window, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr window, out Rect rect);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
    }

}
