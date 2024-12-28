using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordNetBotTemplate.Services;
using GFDeckMaid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace DiscordNetBotTemplate;
public class Startup
{
    private DiscordSocketClient _client;
    private IConfiguration _configuration;
    private readonly List<Task> CurentJobs = new();

    public async Task Initialize()
    {
        Console.WriteLine($"Hellow world");
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
        new CommandHub(_configuration["DiscordBot:PREFIX"]);
        var dbString = _configuration["MongoDB:ConnectionString"];
        var dbName = _configuration["MongoDB:Name"];
        new DBConnection(dbString, dbName);
        Console.WriteLine($"MongoDB connected");

        await using var services = ConfigureServices();
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                GatewayIntents.GuildMembers |
                GatewayIntents.GuildMessages |
                GatewayIntents.DirectMessages|
                GatewayIntents.GuildMessageReactions
        };

        _client = new DiscordSocketClient(config);

        //Set up lisners
        _client.Ready += OnReady;
        _client.MessageReceived += OnMessageReceived;
        _client.Log += LogAsync;
        services.GetRequiredService<CommandService>().Log += LogAsync;


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
        {
            Console.WriteLine($"- {guild.Name}");
        }

        _client.SetGameAsync(_configuration["DiscordBot:ACTIVITY"] ?? "I'm alive!",
            type: ActivityType.CustomStatus);
        Console.WriteLine($"Activity set to '{_client.Activity?.Name}'");

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        CurentJobs.Add(Task.Run(() => CommandHub.commandHub.OnMessage(message)));
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

