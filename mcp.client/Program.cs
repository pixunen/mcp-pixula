using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OllamaSharp;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);

//var modelName = "gpt-oss:20b";
var modelName = "llama3.2";

var app = builder.Build();

// --- Service & MCP Client Setup ---
IChatClient chatClient = new ChatClientBuilder(new OllamaApiClient(new Uri("http://localhost:11434"), modelName))
    .UseFunctionInvocation()
    .Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// --- Spiced-up Console Output ---

// ASCII Art Title
AnsiConsole.MarkupLine("[bold cyan]╔════════════════════════════════════════╗[/]");
AnsiConsole.MarkupLine("[bold cyan]║      Local MCP Sandbox      ║[/]");
AnsiConsole.MarkupLine("[bold cyan]╚════════════════════════════════════════╝[/]");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[grey]MCP Client starting...[/]");

IMcpClient mcpClient;
IList<McpClientTool> tools;
ChatOptions chatOptions = new() { Tools = [] };

try
{
    // Configure and connect to your MCP server.
    mcpClient = await McpClientFactory.CreateAsync(
        new StdioClientTransport(new()
        {
            Command = "dotnet",
            Arguments = ["run", "--project", "C:\\Users\\ottop\\Documents\\GitHub\\mcp\\mcp.server\\mcp.server.csproj"],
            Name = "Minimal MCP Server",
        }));

    await mcpClient.PingAsync();

    var connectedPanel = new Panel("[green]Successfully connected to MCP server.[/]")
    {
        Header = new PanelHeader("Connection Status"),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Green)
    };
    AnsiConsole.Write(connectedPanel);

    // List all available tools from the MCP server.
    tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

    var toolsPanel = new Panel(new Rows(
        tools.Select(tool => new Markup($"[yellow]- {tool.Name}:[/] [grey][[{tool.Description}]][/]"))
    ))
    {
        Header = new PanelHeader("Available Tools"),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Yellow),
        Expand = true
    };
    AnsiConsole.Write(toolsPanel);

    foreach (var tool in tools)
    {
        chatOptions.Tools?.Add(tool);
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[bold red]Failed to connect to the MCP server.[/]");
    AnsiConsole.MarkupLine("[red]Please check the project path.[/]");
    logger.LogError(ex, "Failed to connect to the MCP server.");
    return;
}

AnsiConsole.MarkupLine("\n[bold magenta]Type a prompt or '/quit' to exit.[/]");
AnsiConsole.Write(new Rule { Style = new Style(Color.Magenta1) });
AnsiConsole.WriteLine();


// Conversational loop that can utilize the tools via prompts.
List<ChatMessage> messages = [];
while (true)
{
    AnsiConsole.Markup("[bold green]Prompt: [/]");
    var userInput = Console.ReadLine();
    if (string.Equals(userInput, "/quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    messages.Add(new(ChatRole.User, userInput ?? string.Empty));

    List<ChatResponseUpdate> updates = [];
    AnsiConsole.Markup("[bold cyan]AI: [/]");

    // Get the async enumerable without starting the loop yet
    var streamingResponse = chatClient.GetStreamingResponseAsync(messages, chatOptions);

    // Manually control the enumerator to handle the first update separately
    await using var enumerator = streamingResponse.GetAsyncEnumerator();

    // Show a spinner ONLY while waiting for the VERY FIRST response chunk
    bool hasReceivedFirstUpdate = false;
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("Thinking...", async ctx =>
        {
            if (await enumerator.MoveNextAsync())
            {
                // First chunk received, print it. The Status context will now end.
                AnsiConsole.Markup($"[cyan]{enumerator.Current}[/]");
                updates.Add(enumerator.Current);
                hasReceivedFirstUpdate = true;
            }
        });

    // If we successfully received the first update, stream the rest of the response
    if (hasReceivedFirstUpdate)
    {
        while (await enumerator.MoveNextAsync())
        {
            AnsiConsole.Markup($"[cyan]{enumerator.Current}[/]");
            updates.Add(enumerator.Current);
        }
    }
    // If there was no response at all, we just move on.

    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine();

    messages.AddMessages(updates);
}

AnsiConsole.MarkupLine("[bold yellow]Session ended.[/]");