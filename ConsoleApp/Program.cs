Console.OutputEncoding = System.Text.Encoding.UTF8;

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.WriteLine("Run MineEscape in an interactive terminal.");
    return;
}

GameEngine.Run(Array.Exists(args, arg => arg == "--ascii"));
