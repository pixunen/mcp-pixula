using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace mcp.tools
{
    [McpServerToolType]
    public static class ConfigTool
    {
        [McpServerTool, Description("Gets environment variables (optionally filtered by pattern).")]
        public static string GetEnvironmentVariables(string pattern = "")
        {
            try
            {
                var envVars = Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(entry => string.IsNullOrEmpty(pattern) || 
                                   entry.Key.ToString()!.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(entry => entry.Key)
                    .ToArray();

                if (envVars.Length == 0)
                    return string.IsNullOrEmpty(pattern) ? "No environment variables found" : $"No environment variables matching '{pattern}'";

                var result = string.IsNullOrEmpty(pattern) 
                    ? $"Environment Variables ({envVars.Length}):\n" 
                    : $"Environment Variables matching '{pattern}' ({envVars.Length}):\n";

                foreach (var entry in envVars)
                {
                    var key = entry.Key.ToString();
                    var value = entry.Value?.ToString() ?? "";
                    
                    // Mask sensitive values
                    if (IsSensitiveVariable(key!))
                        value = "***MASKED***";
                    
                    result += $"{key}: {value}\n";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error reading environment variables: {ex.Message}";
            }
        }

        [McpServerTool, Description("Sets an environment variable for the current process.")]
        public static string SetEnvironmentVariable(string name, string value)
        {
            try
            {
                Environment.SetEnvironmentVariable(name, value);
                return $"Environment variable '{name}' set successfully";
            }
            catch (Exception ex)
            {
                return $"Error setting environment variable: {ex.Message}";
            }
        }

        [McpServerTool, Description("Reads and parses a JSON configuration file.")]
        public static string ReadJsonConfig(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Config file not found: {filePath}");

                var content = File.ReadAllText(filePath);
                
                // Validate JSON
                using var document = JsonDocument.Parse(content);
                
                // Pretty print JSON
                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(document.RootElement, options);
            }
            catch (Exception ex)
            {
                return $"Error reading config file: {ex.Message}";
            }
        }

        [McpServerTool, Description("Updates a value in a JSON configuration file.")]
        public static string UpdateJsonConfig(string filePath, string jsonPath, string newValue)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Config file not found: {filePath}");

                var content = File.ReadAllText(filePath);
                var updatedJson = UpdateJsonProperty(content, jsonPath, newValue);
                
                File.WriteAllText(filePath, updatedJson);
                return $"Updated '{jsonPath}' in {filePath}";
            }
            catch (Exception ex)
            {
                return $"Error updating config file: {ex.Message}";
            }
        }

        [McpServerTool, Description("Finds configuration files in the project.")]
        public static string FindConfigFiles(string directoryPath = ".")
        {
            try
            {
                var configPatterns = new[]
                {
                    "*.json", "*.yml", "*.yaml", "*.xml", "*.ini", "*.config",
                    ".env*", "*.toml", "*.properties", "appsettings*"
                };

                var configFiles = new List<string>();

                foreach (var pattern in configPatterns)
                {
                    var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories)
                        .Where(f => !IsInIgnoredDirectory(f))
                        .Select(f => Path.GetRelativePath(directoryPath, f))
                        .ToArray();
                    
                    configFiles.AddRange(files);
                }

                var uniqueFiles = configFiles.Distinct().OrderBy(f => f).ToArray();

                if (uniqueFiles.Length == 0)
                    return "No configuration files found";

                return $"Configuration files found ({uniqueFiles.Length}):\n" + 
                       string.Join("\n", uniqueFiles.Select(f => $"  {f}"));
            }
            catch (Exception ex)
            {
                return $"Error finding config files: {ex.Message}";
            }
        }

        [McpServerTool, Description("Validates a JSON file syntax.")]
        public static string ValidateJson(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var content = File.ReadAllText(filePath);
                using var document = JsonDocument.Parse(content);
                
                return $"✓ {filePath} is valid JSON";
            }
            catch (JsonException ex)
            {
                return $"✗ {filePath} has JSON syntax errors: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error validating JSON: {ex.Message}";
            }
        }

        [McpServerTool, Description("Gets system and runtime configuration information.")]
        public static string GetSystemConfig()
        {
            try
            {
                var info = $"System Configuration:\n" +
                          $"OS: {Environment.OSVersion}\n" +
                          $"Platform: {Environment.OSVersion.Platform}\n" +
                          $"Architecture: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}\n" +
                          $"Processor Count: {Environment.ProcessorCount}\n" +
                          $"Machine Name: {Environment.MachineName}\n" +
                          $"User Name: {Environment.UserName}\n" +
                          $"Domain: {Environment.UserDomainName}\n" +
                          $"Current Directory: {Environment.CurrentDirectory}\n" +
                          $"System Directory: {Environment.SystemDirectory}\n" +
                          $"Runtime Version: {Environment.Version}\n" +
                          $"Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\n" +
                          $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB\n" +
                          $"Available RAM: {GC.GetTotalMemory(false) / 1024 / 1024} MB used by process";

                return info;
            }
            catch (Exception ex)
            {
                return $"Error getting system config: {ex.Message}";
            }
        }

        [McpServerTool, Description("Backs up a configuration file with timestamp.")]
        public static string BackupConfigFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var directory = Path.GetDirectoryName(filePath) ?? ".";
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                var backupPath = Path.Combine(directory, $"{fileName}.backup.{timestamp}{extension}");
                File.Copy(filePath, backupPath);
                
                return $"Backup created: {backupPath}";
            }
            catch (Exception ex)
            {
                return $"Error creating backup: {ex.Message}";
            }
        }

        private static bool IsSensitiveVariable(string variableName)
        {
            var sensitivePatterns = new[] { "password", "secret", "key", "token", "api", "auth" };
            return sensitivePatterns.Any(pattern => 
                variableName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsInIgnoredDirectory(string filePath)
        {
            var ignoredDirs = new[] { "node_modules", "bin", "obj", ".git", ".vs", "packages" };
            return ignoredDirs.Any(dir => filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}"));
        }

        private static string UpdateJsonProperty(string json, string propertyPath, string newValue)
        {
            try
            {
                // Parse the JSON into a JsonNode for manipulation
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode == null)
                    throw new InvalidOperationException("Failed to parse JSON");

                // Split the property path (supports simple nested paths like "a.b.c")
                var pathSegments = propertyPath.Split('.');
                
                // Navigate to the target property
                JsonNode? currentNode = jsonNode;
                JsonNode? parentNode = null;
                string? lastSegment = null;
                
                for (int i = 0; i < pathSegments.Length - 1; i++)
                {
                    parentNode = currentNode;
                    var segment = pathSegments[i];
                    
                    // Handle array index notation like "items[0]"
                    if (segment.Contains('['))
                    {
                        var arrayName = segment.Substring(0, segment.IndexOf('['));
                        var indexStr = segment.Substring(segment.IndexOf('[') + 1, segment.IndexOf(']') - segment.IndexOf('[') - 1);
                        if (int.TryParse(indexStr, out int index))
                        {
                            currentNode = currentNode?[arrayName]?[index];
                        }
                    }
                    else
                    {
                        currentNode = currentNode?[segment];
                    }
                    
                    if (currentNode == null)
                        throw new InvalidOperationException($"Path segment '{segment}' not found");
                }
                
                lastSegment = pathSegments[pathSegments.Length - 1];
                parentNode = currentNode;
                
                if (parentNode == null || lastSegment == null)
                    throw new InvalidOperationException("Invalid property path");
                
                // Parse the new value to determine its type
                JsonNode? newValueNode;
                
                // Try to parse as JSON first
                try
                {
                    newValueNode = JsonNode.Parse(newValue);
                }
                catch
                {
                    // If it's not valid JSON, treat it as a string
                    // But check for special string values
                    if (newValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                        newValueNode = JsonValue.Create(true);
                    else if (newValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                        newValueNode = JsonValue.Create(false);
                    else if (newValue.Equals("null", StringComparison.OrdinalIgnoreCase))
                        newValueNode = null;
                    else if (int.TryParse(newValue, out int intValue))
                        newValueNode = JsonValue.Create(intValue);
                    else if (double.TryParse(newValue, out double doubleValue))
                        newValueNode = JsonValue.Create(doubleValue);
                    else
                        newValueNode = JsonValue.Create(newValue);
                }
                
                // Handle array index notation in the last segment
                if (lastSegment.Contains('['))
                {
                    var arrayName = lastSegment.Substring(0, lastSegment.IndexOf('['));
                    var indexStr = lastSegment.Substring(lastSegment.IndexOf('[') + 1, lastSegment.IndexOf(']') - lastSegment.IndexOf('[') - 1);
                    if (int.TryParse(indexStr, out int index))
                    {
                        var arrayNode = parentNode[arrayName] as JsonArray;
                        if (arrayNode != null && index < arrayNode.Count)
                        {
                            arrayNode[index] = newValueNode;
                        }
                    }
                }
                else
                {
                    // Set the property value
                    if (parentNode is JsonObject jsonObject)
                    {
                        if (newValueNode == null)
                            jsonObject[lastSegment] = null;
                        else
                            jsonObject[lastSegment] = newValueNode;
                    }
                    else if (parentNode is JsonArray jsonArray)
                    {
                        // If the parent is an array and the last segment is an index
                        if (int.TryParse(lastSegment, out int index) && index < jsonArray.Count)
                        {
                            jsonArray[index] = newValueNode;
                        }
                    }
                }
                
                // Return the updated JSON as a formatted string
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                return jsonNode.ToJsonString(options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update JSON property '{propertyPath}': {ex.Message}");
            }
        }
    }
}
