using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace mcp.tools
{
    [McpServerToolType]
    public static class ProcessTool
    {
        [McpServerTool, Description("Runs tests in the current directory (detects test framework automatically).")]
        public static string RunTests(string projectPath = ".", string testFilter = "")
        {
            var testCommands = new Dictionary<string, string>
            {
                ["*.csproj"] = string.IsNullOrEmpty(testFilter) ? "dotnet test" : $"dotnet test --filter {testFilter}",
                ["package.json"] = "npm test",
                ["requirements.txt"] = "python -m pytest",
                ["Cargo.toml"] = "cargo test",
                ["pom.xml"] = "mvn test"
            };

            foreach (var (pattern, command) in testCommands)
            {
                if (Directory.GetFiles(projectPath, pattern).Any())
                {
                    return ExecuteWithTimeout(command.Split(' ')[0], string.Join(" ", command.Split(' ').Skip(1)), projectPath, 120);
                }
            }

            return "No recognized test framework found";
        }

        [McpServerTool, Description("Builds the project (detects build system automatically).")]
        public static string BuildProject(string projectPath = ".")
        {
            var buildCommands = new Dictionary<string, string>
            {
                ["*.csproj"] = "dotnet build",
                ["*.sln"] = "dotnet build",
                ["package.json"] = "npm run build",
                ["Makefile"] = "make",
                ["Cargo.toml"] = "cargo build",
                ["pom.xml"] = "mvn compile"
            };

            foreach (var (pattern, command) in buildCommands)
            {
                if (Directory.GetFiles(projectPath, pattern).Any())
                {
                    return ExecuteWithTimeout(command.Split(' ')[0], string.Join(" ", command.Split(' ').Skip(1)), projectPath, 300);
                }
            }

            return "No recognized build system found";
        }

        [McpServerTool, Description("Installs project dependencies.")]
        public static string InstallDependencies(string projectPath = ".")
        {
            var installCommands = new Dictionary<string, string>
            {
                ["*.csproj"] = "dotnet restore",
                ["package.json"] = "npm install",
                ["requirements.txt"] = "pip install -r requirements.txt",
                ["Cargo.toml"] = "cargo fetch",
                ["pom.xml"] = "mvn dependency:resolve"
            };

            foreach (var (pattern, command) in installCommands)
            {
                if (Directory.GetFiles(projectPath, pattern).Any())
                {
                    return ExecuteWithTimeout(command.Split(' ')[0], string.Join(" ", command.Split(' ').Skip(1)), projectPath, 600);
                }
            }

            return "No recognized dependency file found";
        }

        [McpServerTool, Description("Runs linting/formatting tools on the project.")]
        public static string LintProject(string projectPath = ".", bool fix = false)
        {
            var results = new List<string>();

            // C# formatting
            if (Directory.GetFiles(projectPath, "*.csproj").Any())
            {
                var command = fix ? "dotnet format" : "dotnet format --verify-no-changes";
                results.Add($"C# Format: {ExecuteWithTimeout("dotnet", command.Replace("dotnet ", ""), projectPath, 60)}");
            }

            // JavaScript/TypeScript
            if (File.Exists(Path.Combine(projectPath, "package.json")))
            {
                var eslintArgs = fix ? "--fix ." : ".";
                if (File.Exists(Path.Combine(projectPath, ".eslintrc.js")) || File.Exists(Path.Combine(projectPath, ".eslintrc.json")))
                {
                    results.Add($"ESLint: {ExecuteWithTimeout("npx", $"eslint {eslintArgs}", projectPath, 60)}");
                }
                
                if (File.Exists(Path.Combine(projectPath, ".prettierrc")))
                {
                    var prettierArgs = fix ? "--write ." : "--check .";
                    results.Add($"Prettier: {ExecuteWithTimeout("npx", $"prettier {prettierArgs}", projectPath, 60)}");
                }
            }

            // Python
            if (File.Exists(Path.Combine(projectPath, "requirements.txt")))
            {
                results.Add($"Black: {ExecuteWithTimeout("black", fix ? "." : "--check .", projectPath, 60)}");
                results.Add($"Flake8: {ExecuteWithTimeout("flake8", ".", projectPath, 60)}");
            }

            return results.Count > 0 ? string.Join("\n\n", results) : "No linting tools configured";
        }

        [McpServerTool, Description("Gets running processes related to development.")]
        public static string GetDevelopmentProcesses()
        {
            var devProcesses = new[] { "dotnet", "node", "npm", "yarn", "python", "java", "cargo", "make", "git" };
            var results = new List<string>();

            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => devProcesses.Any(dev => p.ProcessName.Contains(dev, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(p => p.ProcessName)
                    .ToArray();

                foreach (var process in processes)
                {
                    try
                    {
                        var info = $"{process.ProcessName} (PID: {process.Id})";
                        if (!string.IsNullOrEmpty(process.MainWindowTitle))
                            info += $" - {process.MainWindowTitle}";
                        
                        results.Add(info);
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                }

                return results.Count > 0 
                    ? $"Development processes ({results.Count}):\n" + string.Join("\n", results)
                    : "No development processes found";
            }
            catch (Exception ex)
            {
                return $"Error getting processes: {ex.Message}";
            }
        }

        [McpServerTool, Description("Kills processes by name.")]
        public static string KillProcessesByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                    return $"No processes found with name '{processName}'";

                var killed = 0;
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        killed++;
                    }
                    catch
                    {
                        // Skip processes we can't kill
                    }
                }

                return $"Killed {killed} of {processes.Length} processes named '{processName}'";
            }
            catch (Exception ex)
            {
                return $"Error killing processes: {ex.Message}";
            }
        }

        [McpServerTool, Description("Monitors a process and waits for completion.")]
        public static string MonitorProcess(string command, string arguments = "", string workingDirectory = ".", int timeoutSeconds = 300)
        {
            return ExecuteWithTimeout(command, arguments, workingDirectory, timeoutSeconds);
        }

        private static string ExecuteWithTimeout(string command, string arguments, string workingDirectory, int timeoutSeconds)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetFullPath(workingDirectory)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start process");

                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        errorBuilder.AppendLine(e.Data);
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completed = process.WaitForExit(timeoutSeconds * 1000);
                
                if (!completed)
                {
                    process.Kill();
                    return $"Process timed out after {timeoutSeconds} seconds";
                }

                var output = outputBuilder.ToString();
                var error = errorBuilder.ToString();

                var result = $"Exit code: {process.ExitCode}\n";
                if (!string.IsNullOrEmpty(output))
                    result += $"Output:\n{output}\n";
                if (!string.IsNullOrEmpty(error))
                    result += $"Error:\n{error}";

                return result;
            }
            catch (Exception ex)
            {
                return $"Error executing command: {ex.Message}";
            }
        }
    }
}