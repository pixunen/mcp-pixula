using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace mcp.tools
{
    [McpServerToolType]
    public static class GitTool
    {
        [McpServerTool, Description("Gets git status of the repository.")]
        public static string GetGitStatus(string repositoryPath = ".")
        {
            return ExecuteGitCommand("status --porcelain", repositoryPath);
        }

        [McpServerTool, Description("Gets recent git commits.")]
        public static string GetGitLog(string repositoryPath = ".", int count = 10)
        {
            return ExecuteGitCommand($"log --oneline -{count}", repositoryPath);
        }

        [McpServerTool, Description("Gets current git branch.")]
        public static string GetCurrentBranch(string repositoryPath = ".")
        {
            return ExecuteGitCommand("branch --show-current", repositoryPath);
        }

        [McpServerTool, Description("Gets git diff for uncommitted changes.")]
        public static string GetGitDiff(string repositoryPath = ".", string filePath = "")
        {
            var command = string.IsNullOrEmpty(filePath) ? "diff" : $"diff {filePath}";
            return ExecuteGitCommand(command, repositoryPath);
        }

        [McpServerTool, Description("Shows blame information for a file.")]
        public static string GetGitBlame(string filePath, string repositoryPath = ".")
        {
            return ExecuteGitCommand($"blame {filePath}", repositoryPath);
        }

        [McpServerTool, Description("Lists all git-tracked files.")]
        public static string[] GetGitTrackedFiles(string repositoryPath = ".")
        {
            var output = ExecuteGitCommand("ls-files", repositoryPath);
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        [McpServerTool, Description("Shows commit details.")]
        public static string ShowCommit(string commitHash, string repositoryPath = ".")
        {
            return ExecuteGitCommand($"show {commitHash}", repositoryPath);
        }

        [McpServerTool, Description("Gets file history for a specific file.")]
        public static string GetFileHistory(string filePath, string repositoryPath = ".", int count = 10)
        {
            return ExecuteGitCommand($"log --oneline -{count} -- {filePath}", repositoryPath);
        }

        private static string ExecuteGitCommand(string gitArgs, string repositoryPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = gitArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetFullPath(repositoryPath)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start git process");

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"Git command failed: {error}");

                return output.Trim();
            }
            catch (Exception ex)
            {
                return $"Error executing git command: {ex.Message}";
            }
        }
    }
}