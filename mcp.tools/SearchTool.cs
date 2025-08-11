using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace mcp.tools
{
    [McpServerToolType]
    public static class SearchTool
    {
        [McpServerTool, Description("Searches for text content across multiple files in a directory.")]
        public static string SearchInFiles(string searchTerm, string directoryPath = ".", 
            string filePattern = "*", bool caseSensitive = false, bool useRegex = false)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var results = new List<SearchResult>();
            var searchOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            
            try
            {
                var files = Directory.GetFiles(directoryPath, filePattern, SearchOption.AllDirectories)
                    .Where(f => IsTextFile(f) && !IsInIgnoredDirectory(f))
                    .ToArray();

                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            bool matches = useRegex 
                                ? Regex.IsMatch(lines[i], searchTerm, searchOptions)
                                : lines[i].Contains(searchTerm, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

                            if (matches)
                            {
                                results.Add(new SearchResult
                                {
                                    FilePath = Path.GetRelativePath(directoryPath, file),
                                    LineNumber = i + 1,
                                    Content = lines[i].Trim()
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                return FormatSearchResults(results, searchTerm);
            }
            catch (Exception ex)
            {
                return $"Search error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Finds files by name pattern.")]
        public static string FindFiles(string namePattern, string directoryPath = ".")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            try
            {
                var files = Directory.GetFiles(directoryPath, namePattern, SearchOption.AllDirectories)
                    .Where(f => !IsInIgnoredDirectory(f))
                    .Select(f => Path.GetRelativePath(directoryPath, f))
                    .OrderBy(f => f)
                    .ToArray();

                return files.Length > 0 
                    ? $"Found {files.Length} files matching '{namePattern}':\n" + string.Join("\n", files)
                    : $"No files found matching '{namePattern}'";
            }
            catch (Exception ex)
            {
                return $"Find error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Searches for TODO, FIXME, HACK comments in code.")]
        public static string FindTodos(string directoryPath = ".")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var todoPatterns = new Dictionary<string, string>
            {
                ["TODO"] = @"(?://|#|/\*|\<!--)\s*TODO:?\s*(.*)(?:\*/|\-->)?",
                ["FIXME"] = @"(?://|#|/\*|\<!--)\s*FIXME:?\s*(.*)(?:\*/|\-->)?",
                ["HACK"] = @"(?://|#|/\*|\<!--)\s*HACK:?\s*(.*)(?:\*/|\-->)?",
                ["BUG"] = @"(?://|#|/\*|\<!--)\s*BUG:?\s*(.*)(?:\*/|\-->)?",
                ["NOTE"] = @"(?://|#|/\*|\<!--)\s*NOTE:?\s*(.*)(?:\*/|\-->)?",
                ["XXX"] = @"(?://|#|/\*|\<!--)\s*XXX:?\s*(.*)(?:\*/|\-->)?",
                ["OPTIMIZE"] = @"(?://|#|/\*|\<!--)\s*OPTIMIZE:?\s*(.*)(?:\*/|\-->)?",
                ["REFACTOR"] = @"(?://|#|/\*|\<!--)\s*REFACTOR:?\s*(.*)(?:\*/|\-->)?"
            };

            var allResults = new Dictionary<string, List<TodoItem>>();
            
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Where(f => IsTextFile(f) && !IsInIgnoredDirectory(f))
                    .ToArray();

                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        var relativePath = Path.GetRelativePath(directoryPath, file);
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            
                            foreach (var pattern in todoPatterns)
                            {
                                var regex = new Regex(pattern.Value, RegexOptions.IgnoreCase);
                                var match = regex.Match(line);
                                
                                if (match.Success)
                                {
                                    if (!allResults.ContainsKey(pattern.Key))
                                        allResults[pattern.Key] = new List<TodoItem>();
                                    
                                    var description = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : "";
                                    
                                    // Clean up the description
                                    description = description.TrimEnd('*', '/', '-', '>');
                                    
                                    allResults[pattern.Key].Add(new TodoItem
                                    {
                                        FilePath = relativePath,
                                        LineNumber = i + 1,
                                        Type = pattern.Key,
                                        Description = description,
                                        FullLine = line.Trim()
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                return FormatTodoResults(allResults);
            }
            catch (Exception ex)
            {
                return $"TODO search error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Finds functions or methods containing specific text.")]
        public static string FindFunctions(string functionNamePattern, string directoryPath = ".", string fileExtension = "*")
        {
            var results = new List<string>();
            var pattern = fileExtension == "*" ? "*" : $"*.{fileExtension.TrimStart('.')}";

            try
            {
                var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories)
                    .Where(f => IsCodeFile(f) && !IsInIgnoredDirectory(f));

                foreach (var file in files)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var ext = Path.GetExtension(file).ToLower();
                        
                        var functionPattern = ext switch
                        {
                            ".cs" => @$"(public|private|protected|internal)?\s*(static)?\s*\w+\s+{functionNamePattern}\s*\(",
                            ".js" or ".ts" => @$"(function\s+{functionNamePattern}|{functionNamePattern}\s*[:=]\s*function|{functionNamePattern}\s*\()",
                            ".py" => @$"def\s+{functionNamePattern}\s*\(",
                            ".java" => @$"(public|private|protected)?\s*(static)?\s*\w+\s+{functionNamePattern}\s*\(",
                            _ => @$"{functionNamePattern}\s*\("
                        };

                        var matches = Regex.Matches(content, functionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        foreach (Match match in matches)
                        {
                            var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                            results.Add($"{Path.GetRelativePath(directoryPath, file)}:{lineNumber} - {match.Value.Trim()}");
                        }
                    }
                    catch
                    {
                        // Skip files that can't be processed
                    }
                }

                return results.Count > 0 
                    ? $"Found {results.Count} functions matching '{functionNamePattern}':\n" + string.Join("\n", results)
                    : $"No functions found matching '{functionNamePattern}'";
            }
            catch (Exception ex)
            {
                return $"Function search error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Gets project statistics (file counts, lines of code, etc.).")]
        public static string GetProjectStats(string directoryPath = ".")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            try
            {
                var allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Where(f => !IsInIgnoredDirectory(f))
                    .ToArray();

                var codeFiles = allFiles.Where(IsCodeFile).ToArray();
                var totalLines = 0;
                var codeLines = 0;
                var commentLines = 0;
                var blankLines = 0;

                var fileTypeCounts = new Dictionary<string, int>();
                var fileTypeSizes = new Dictionary<string, long>();

                foreach (var file in codeFiles)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        var fileInfo = new FileInfo(file);
                        totalLines += lines.Length;
                        
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            
                            if (string.IsNullOrWhiteSpace(trimmedLine))
                                blankLines++;
                            else if (IsCommentLine(trimmedLine, Path.GetExtension(file)))
                                commentLines++;
                            else
                                codeLines++;
                        }

                        var ext = Path.GetExtension(file).ToLower();
                        if (string.IsNullOrEmpty(ext))
                            ext = "(no extension)";
                            
                        fileTypeCounts[ext] = fileTypeCounts.GetValueOrDefault(ext, 0) + 1;
                        fileTypeSizes[ext] = fileTypeSizes.GetValueOrDefault(ext, 0) + fileInfo.Length;
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                var totalSize = fileTypeSizes.Values.Sum();
                
                var stats = $"Project Statistics for: {Path.GetFullPath(directoryPath)}\n" +
                           $"{'=',-50}\n" +
                           $"Total files: {allFiles.Length:N0}\n" +
                           $"Code files: {codeFiles.Length:N0}\n" +
                           $"Total size: {FormatBytes(totalSize)}\n\n" +
                           $"Lines of Code Analysis:\n" +
                           $"  Total lines: {totalLines:N0}\n" +
                           $"  Code lines: {codeLines:N0} ({GetPercentage(codeLines, totalLines)}%)\n" +
                           $"  Comment lines: {commentLines:N0} ({GetPercentage(commentLines, totalLines)}%)\n" +
                           $"  Blank lines: {blankLines:N0} ({GetPercentage(blankLines, totalLines)}%)\n\n" +
                           "File types breakdown:\n" +
                           string.Join("\n", fileTypeCounts.OrderByDescending(kvp => kvp.Value)
                               .Select(kvp => $"  {kvp.Key}: {kvp.Value:N0} files ({FormatBytes(fileTypeSizes.GetValueOrDefault(kvp.Key, 0))})"));

                return stats;
            }
            catch (Exception ex)
            {
                return $"Stats error: {ex.Message}";
            }
        }

        private static bool IsTextFile(string filePath)
        {
            var textExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".txt", 
                ".md", ".json", ".xml", ".yml", ".yaml", ".css", ".html", ".php", ".rb", ".go", ".rs",
                ".sh", ".ps1", ".bat", ".sql", ".vue", ".jsx", ".tsx", ".scss", ".sass", ".less" };
            return textExtensions.Contains(Path.GetExtension(filePath).ToLower());
        }

        private static bool IsCodeFile(string filePath)
        {
            var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", 
                ".css", ".html", ".php", ".rb", ".go", ".rs", ".sql", ".jsx", ".tsx", ".vue",
                ".sh", ".ps1", ".bat", ".swift", ".kt", ".m", ".mm", ".scala", ".r", ".dart" };
            return codeExtensions.Contains(Path.GetExtension(filePath).ToLower());
        }

        private static bool IsInIgnoredDirectory(string filePath)
        {
            var ignoredDirs = new[] { "node_modules", "bin", "obj", ".git", ".vs", "packages", 
                "target", "dist", "build", "__pycache__", ".idea", ".vscode", "vendor",
                "bower_components", ".nuget", "TestResults", "coverage" };
            return ignoredDirs.Any(dir => filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}"));
        }

        private static bool IsCommentLine(string line, string fileExtension)
        {
            var ext = fileExtension.ToLower();
            
            // Single-line comments
            if (ext is ".cs" or ".js" or ".ts" or ".java" or ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".swift" or ".kt")
                return line.StartsWith("//");
            
            if (ext is ".py" or ".rb" or ".sh" or ".ps1" or ".yml" or ".yaml")
                return line.StartsWith("#");
            
            if (ext is ".sql")
                return line.StartsWith("--");
            
            if (ext is ".html" or ".xml")
                return line.StartsWith("<!--") && line.EndsWith("-->");
            
            if (ext is ".css" or ".scss" or ".sass" or ".less")
                return (line.StartsWith("/*") && line.EndsWith("*/")) || line.StartsWith("//");
            
            // Multi-line comment indicators (simplified - doesn't handle all cases)
            if (line.StartsWith("/*") || line.StartsWith("*") || line.EndsWith("*/"))
                return true;
            
            return false;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        private static int GetPercentage(int part, int total)
        {
            if (total == 0) return 0;
            return (int)Math.Round((double)part / total * 100);
        }

        private static string FormatSearchResults(List<SearchResult> results, string searchTerm)
        {
            if (results.Count == 0)
                return $"No matches found for '{searchTerm}'";

            var grouped = results.GroupBy(r => r.FilePath).OrderBy(g => g.Key);
            var output = $"Found {results.Count} matches for '{searchTerm}' in {grouped.Count()} files:\n\n";

            foreach (var fileGroup in grouped)
            {
                output += $"📄 {fileGroup.Key}:\n";
                foreach (var result in fileGroup.OrderBy(r => r.LineNumber))
                {
                    output += $"  Line {result.LineNumber}: {result.Content}\n";
                }
                output += "\n";
            }

            return output;
        }

        private static string FormatTodoResults(Dictionary<string, List<TodoItem>> results)
        {
            if (!results.Any() || results.All(r => r.Value.Count == 0))
                return "No TODO items found in the project";

            var totalCount = results.Sum(r => r.Value.Count);
            var output = $"Found {totalCount} TODO items in the project:\n";
            output += $"{'=',-60}\n\n";

            // Order by priority: BUG, FIXME, TODO, HACK, NOTE, others
            var priorityOrder = new[] { "BUG", "FIXME", "TODO", "HACK", "OPTIMIZE", "REFACTOR", "XXX", "NOTE" };
            var orderedResults = results
                .OrderBy(r => Array.IndexOf(priorityOrder, r.Key) == -1 ? int.MaxValue : Array.IndexOf(priorityOrder, r.Key));

            foreach (var category in orderedResults.Where(r => r.Value.Count > 0))
            {
                var emoji = category.Key switch
                {
                    "BUG" => "🐛",
                    "FIXME" => "🔧",
                    "TODO" => "📝",
                    "HACK" => "⚡",
                    "NOTE" => "📌",
                    "OPTIMIZE" => "🚀",
                    "REFACTOR" => "♻️",
                    "XXX" => "⚠️",
                    _ => "•"
                };

                output += $"{emoji} {category.Key} ({category.Value.Count} items):\n";
                output += $"{'-',-40}\n";
                
                // Group by file
                var fileGroups = category.Value.GroupBy(item => item.FilePath).OrderBy(g => g.Key);
                
                foreach (var fileGroup in fileGroups)
                {
                    output += $"\n  📁 {fileGroup.Key}:\n";
                    foreach (var item in fileGroup.OrderBy(i => i.LineNumber))
                    {
                        var description = string.IsNullOrWhiteSpace(item.Description) 
                            ? "(no description)" 
                            : item.Description;
                            
                        // Truncate long descriptions
                        if (description.Length > 80)
                            description = description.Substring(0, 77) + "...";
                            
                        output += $"    Line {item.LineNumber,4}: {description}\n";
                    }
                }
                
                output += "\n";
            }

            // Add summary
            output += $"{'=',-60}\n";
            output += "Summary by type:\n";
            foreach (var category in orderedResults.Where(r => r.Value.Count > 0))
            {
                output += $"  {category.Key}: {category.Value.Count}\n";
            }

            return output;
        }

        private class SearchResult
        {
            public string FilePath { get; set; } = "";
            public int LineNumber { get; set; }
            public string Content { get; set; } = "";
        }

        private class TodoItem
        {
            public string FilePath { get; set; } = "";
            public int LineNumber { get; set; }
            public string Type { get; set; } = "";
            public string Description { get; set; } = "";
            public string FullLine { get; set; } = "";
        }
    }
}
