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

        [McpServerTool, Description("Analyzes changed files and generates commit title and message based on their content.")]
        public static string GenerateCommitMessage(string repositoryPath = ".")
        {
            try
            {
                // Get changed files using git status --porcelain
                var statusOutput = ExecuteGitCommand("status --porcelain", repositoryPath);
                
                if (string.IsNullOrWhiteSpace(statusOutput))
                {
                    return "No changes detected in the repository.";
                }

                var changedFiles = ParseGitStatus(statusOutput);
                if (changedFiles.Count == 0)
                {
                    return "No staged or unstaged changes found.";
                }

                // Read content of changed files
                var fileContents = new List<(string filePath, string status, string content)>();
                
                foreach (var (filePath, status) in changedFiles)
                {
                    try
                    {
                        var fullPath = Path.Combine(Path.GetFullPath(repositoryPath), filePath);
                        if (File.Exists(fullPath))
                        {
                            var content = File.ReadAllText(fullPath);
                            fileContents.Add((filePath, status, content));
                        }
                        else if (status.Contains("D")) // Deleted file
                        {
                            fileContents.Add((filePath, status, "[File deleted]"));
                        }
                    }
                    catch (Exception ex)
                    {
                        fileContents.Add((filePath, status, $"[Error reading file: {ex.Message}]"));
                    }
                }

                // Generate commit message
                return GenerateCommitMessageFromChanges(fileContents);
            }
            catch (Exception ex)
            {
                return $"Error generating commit message: {ex.Message}";
            }
        }

        private static List<(string filePath, string status)> ParseGitStatus(string statusOutput)
        {
            var changedFiles = new List<(string filePath, string status)>();
            var lines = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Length >= 3)
                {
                    var status = line.Substring(0, 2);
                    var filePath = line.Substring(3);
                    changedFiles.Add((filePath, status));
                }
            }

            return changedFiles;
        }

        private static string GenerateCommitMessageFromChanges(List<(string filePath, string status, string content)> fileContents)
        {
            var commitTitle = "";
            var commitMessage = "";
            
            // Analyze changes to determine commit type
            var hasNewFiles = fileContents.Any(f => f.status.StartsWith("A"));
            var hasModifiedFiles = fileContents.Any(f => f.status.StartsWith("M") || f.status.Contains("M"));
            var hasDeletedFiles = fileContents.Any(f => f.status.StartsWith("D") || f.status.Contains("D"));
            var hasRenamedFiles = fileContents.Any(f => f.status.StartsWith("R"));

            // Generate title based on change patterns
            if (hasNewFiles && !hasModifiedFiles && !hasDeletedFiles)
            {
                commitTitle = fileContents.Count == 1 ? 
                    $"Add {Path.GetFileName(fileContents[0].filePath)}" :
                    $"Add {fileContents.Count} new files";
            }
            else if (hasDeletedFiles && !hasModifiedFiles && !hasNewFiles)
            {
                commitTitle = fileContents.Count == 1 ? 
                    $"Remove {Path.GetFileName(fileContents[0].filePath)}" :
                    $"Remove {fileContents.Count} files";
            }
            else if (hasRenamedFiles)
            {
                commitTitle = fileContents.Count == 1 ? 
                    $"Rename {Path.GetFileName(fileContents[0].filePath)}" :
                    $"Rename {fileContents.Count} files";
            }
            else if (hasModifiedFiles && !hasNewFiles && !hasDeletedFiles)
            {
                commitTitle = fileContents.Count == 1 ? 
                    $"Update {Path.GetFileName(fileContents[0].filePath)}" :
                    $"Update {fileContents.Count} files";
            }
            else
            {
                // Mixed changes
                var changeTypes = new List<string>();
                if (hasNewFiles) changeTypes.Add($"{fileContents.Count(f => f.status.StartsWith("A"))} added");
                if (hasModifiedFiles) changeTypes.Add($"{fileContents.Count(f => f.status.StartsWith("M") || f.status.Contains("M"))} modified");
                if (hasDeletedFiles) changeTypes.Add($"{fileContents.Count(f => f.status.StartsWith("D") || f.status.Contains("D"))} deleted");
                
                commitTitle = $"Update project: {string.Join(", ", changeTypes)} files";
            }

            // Generate detailed message
            commitMessage = "Changes:\n";
            foreach (var (filePath, status, content) in fileContents.Take(10)) // Limit to first 10 files
            {
                var statusDescription = GetStatusDescription(status);
                commitMessage += $"- {statusDescription}: {filePath}\n";
                
                // Add brief content summary for modified files
                if (status.StartsWith("M") || status.Contains("M"))
                {
                    var lines = content.Split('\n');
                    if (lines.Length > 5)
                    {
                        commitMessage += $"  ({lines.Length} lines)\n";
                    }
                }
            }

            if (fileContents.Count > 10)
            {
                commitMessage += $"... and {fileContents.Count - 10} more files\n";
            }

            return $"Title: {commitTitle}\n\nMessage:\n{commitMessage}";
        }

        private static string GetStatusDescription(string status)
        {
            return status switch
            {
                "A " => "Added",
                " A" => "Added",
                "M " => "Modified",
                " M" => "Modified",
                "D " => "Deleted",
                " D" => "Deleted",
                "R " => "Renamed",
                " R" => "Renamed",
                "C " => "Copied",
                " C" => "Copied",
                "??" => "Untracked",
                _ => $"Changed ({status.Trim()})"
            };
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