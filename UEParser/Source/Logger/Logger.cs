using Microsoft.VisualBasic;
using System.IO;
using System;
using System.Reflection;
using UEParser.ViewModels;

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
        SaveLog("UEParser exit. Logging finished.", LogTags.Exit);
    }

    public static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        SaveLog("UEParser terminated. Logging finished.", LogTags.Exit);
    }

    public static void SaveLog(string logMessage, LogTags logTag)
    {
        try
        {
            if (logFilePath != null)
            {
                // Append the log message to the log file
                using StreamWriter writer = File.AppendText(logFilePath);
                writer.WriteLine($"[{DateTime.Now}] [{logTag}] {logMessage}");
            }
        }
        catch 
        {
            // do nothing
        }
    }
}