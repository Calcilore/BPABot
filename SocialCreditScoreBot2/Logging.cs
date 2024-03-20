using System.Collections.Concurrent;
using System.Text;

namespace SocialCreditScoreBot2;

public static class Logging {
    private static ConcurrentQueue<(LogLevel, string)> logQueue = new();
    private static Thread logWorker = new(LogWorker);
    private static CancellationTokenSource cts = new();
    private static string fileName = "log.txt";

    public static void Init() {
        // Generate a filename with the current datetime
        Directory.CreateDirectory("logs");
        fileName = $"./logs/log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        
        Info("Logging to " + fileName);
        logWorker.Start();
    }

    public static void Stop() {
        cts.Cancel();
        logWorker.Join();
    }
    
    private static void LogWorker() {
        FileStream file = File.OpenWrite(fileName);
        
        while (!cts.IsCancellationRequested) {
            if (logQueue.TryDequeue(out (LogLevel, string) log)) {
                string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string text = $"[{datetime}] [{log.Item1}] {log.Item2}\n";
                
                // Print it
                ConsoleColor originalColor = Console.ForegroundColor;
                ConsoleColor color = log.Item1 switch {
                    LogLevel.Verbose => ConsoleColor.Gray,
                    LogLevel.Debug => ConsoleColor.White,
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };
                Console.ForegroundColor = color;
                Console.Write(text);
                Console.ForegroundColor = originalColor;
                
                // Write to file
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                file.Write(bytes);
            }
            else {
                Thread.Yield();
            }
        }
        
        file.Flush();
        file.Close();
    }
    
    // LOG METHODS
    public static void Log(LogLevel level, string message) => logQueue.Enqueue((level, message));
    public static void Verbose(string message) => Log(LogLevel.Verbose, message);
    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warning(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);
    
}

public enum LogLevel {
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}