using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordNetBotTemplate.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace DiscordNetBotTemplate;
public class Startup
{
    private DiscordSocketClient _client;
    private IConfiguration _configuration;

    public async Task Initialize()
    {
        // Load configuration
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Replace placeholders in configuration with environment variables
        var jsonConfig = File.ReadAllText("appsettings.Local.json");
        var jObject = JObject.Parse(jsonConfig);
        _configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jObject.ToString())))
            .AddEnvironmentVariables()
            .Build();
        var token = _configuration["DiscordBot:TOKEN"];
        await using var services = ConfigureServices();
        var config = new DiscordSocketConfig { GatewayIntents =  GatewayIntents.DirectMessages };
        _client = new DiscordSocketClient(config);

        //Set up lisners
        _client.Ready += OnReady;
        _client.MessageReceived += OnMessageReceived;
        _client.Log += LogAsync;
        services.GetRequiredService<CommandService>().Log += LogAsync;

        
        _client = new DiscordSocketClient(config);

        //Login
        await _client.LoginAsync(TokenType.Bot, token.ToString());
        await _client.StartAsync();

        // Here we initialize the logic required to register our commands.
        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task OnReady()
    {
        // Logs the bot name and all the servers that it's connected to
        Console.WriteLine($"Connected to these servers as '{_client.CurrentUser.Username}': ");
        foreach (var guild in _client.Guilds)
            Console.WriteLine($"- {guild.Name}");

        // Set the activity from the environment variable or fallback to 'I'm alive!'
        _client.SetGameAsync(_configuration["DiscordBot:ACTIVITY"] ?? "I'm alive!",
            type: ActivityType.CustomStatus);
        Console.WriteLine($"Activity set to '{_client.Activity?.Name}'");

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot)
        {
            return;
        }

        Console.WriteLine($"Message received: {message.Content}");

        await message.Channel.SendMessageAsync($"You said: {message.Content}");
    }

    private static Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private static ServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandlingService>()
            .BuildServiceProvider();
    }
}

