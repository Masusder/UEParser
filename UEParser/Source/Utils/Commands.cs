using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UEParser.Utils;

public abstract class CommandUtils
{
    public class CommandModel
    {
        public required string Argument { get; set; }
        public required string PathToExe { get; set; }
    }

    public static async Task ExecuteCommandsAsync(List<CommandModel> commands)
    {
        List<Task> tasks = [];

        foreach (var command in commands)
        {
            tasks.Add(Task.Run(() => ExecuteCommand(command.Argument, command.PathToExe, GlobalVariables.RootDir)));
        }

        await Task.WhenAll(tasks);
    }

    public static void ExecuteCommand(string command, string exe, string workingDirectory)
    {
        try
        {
            ProcessStartInfo processInfo = new()
            {
                FileName = exe,
                Arguments = command,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo) ?? throw new Exception($"Failed to start the process.");

            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception while executing command '{command}': {ex.Message}");
        }
    }
}