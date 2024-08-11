using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Parser.Wwise;

public class WwiseUtilities
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
            tasks.Add(Task.Run(() => ExecuteCommand(command.Argument, command.PathToExe)));
        }

        await Task.WhenAll(tasks);
    }

    public static void ExecuteCommand(string command, string exe)
    {
        try
        {
            ProcessStartInfo processInfo = new()
            {
                FileName = exe,
                Arguments = command,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(processInfo);

            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception while executing command '{command}': {ex.Message}");
        }
    }
}