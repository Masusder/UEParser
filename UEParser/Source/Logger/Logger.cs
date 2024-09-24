using System;
using System.IO;

namespace UEParser;

public class Logger
{
    private static readonly string logDirectoryPath = Path.Combine(GlobalVariables.rootDir, "Output", "Logs");
    private static readonly string logFilePath;

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
        logFilePath = Path.Combine(logDirectoryPath, logFileName);

        if (!Directory.Exists(logDirectoryPath))
        {
            Directory.CreateDirectory(logDirectoryPath);
        }
    }

    public static void OnProcessExit(object? sender, EventArgs e)
    {
        SaveLog("UEParser exit. Logging finished.", LogTags.Exit, ELogExtraTag.None);
    }

    public static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        SaveLog("UEParser terminated. Logging finished.", LogTags.Exit, ELogExtraTag.None);
    }

    public static void SaveLog(string logMessage, LogTags logTag, ELogExtraTag extraTag)
    {
        try
        {
            if (logFilePath != null)
            {

                // Append the log message to the log file
                using StreamWriter writer = File.AppendText(logFilePath);

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