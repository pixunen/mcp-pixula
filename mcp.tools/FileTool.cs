using ModelContextProtocol.Server;
using System.ComponentModel;

namespace mcp.tools
{
    [McpServerToolType]
    public static class FileTool
    {
        [McpServerTool, Description("Reads the content of a local file.")]
        public static string ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return File.ReadAllText(filePath);
        }

        [McpServerTool, Description("Writes content to a local file.")]
        public static void WriteFile(string filePath, string content)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, content);
        }

        [McpServerTool, Description("Lists files in a directory.")]
        public static string[] ListFiles(string directoryPath, string searchPattern = "*")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            return Directory.GetFiles(directoryPath, searchPattern);
        }
    }
}