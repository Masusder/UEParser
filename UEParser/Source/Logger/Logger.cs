using System;
using System.IO;

namespace UEParser;

public class Logger
{
    private static readonly string LogDirectoryPath = Path.Combine(GlobalVariables.RootDir, "Output", "Logs");
    private static readonly object LogLock = new();
    private static readonly string LogFilePath;

    public enum LogTags
    {
        Success,
        Info,
        Warning,
        Error,
        Exit,
        Debug
    }

    public enum ELogExtraTag
    {
        None,
        Addons,
        Bundles,
        CharacterClasses,
        Characters,
        Collections,
        Cosmetics,
        DLC,
        Items,
        Journals,
        Maps,
        Offerings,
        Perks,
        Rifts,
        SpecialEvents,
        Tomes
    }

    static Logger()
    {
        // Generate log file path with current date and time
        string logFileName = $"UEParser-Logs-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
        LogFilePath = Path.Combine(LogDirectoryPath, logFileName);

        if (!Directory.Exists(LogDirectoryPath)) Directory.CreateDirectory(LogDirectoryPath);
    }

    public static void OnProcessExit(object? sender, EventArgs e)
    {
        SaveLog("UEParser exit. Logging finished.", LogTags.Exit, ELogExtraTag.None);
    }

    public static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        SaveLog("UEParser terminated. Logging finished.", LogTags.Exit, ELogExtraTag.None);
    }

    public static void SaveLog(string logMessage, LogTags logTag, ELogExtraTag extraTag = ELogExtraTag.None)
    {
        lock (LogLock)
        {
            try
            {
                if (LogFilePath != null)
                {
                    // Append the log message to the log file
                    using StreamWriter writer = File.AppendText(LogFilePath);

                    string formattedLogMessage = $"[{DateTime.Now}] [{logTag}]";

                    if (extraTag != ELogExtraTag.None)
                    {
                        formattedLogMessage += $" [{extraTag}]";
                    }

                    formattedLogMessage += $" {logMessage}";
                    writer.WriteLine(formattedLogMessage);
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}