using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

class Program
{
    private DiscordSocketClient _client;
    private InteractionService _interactions;
    private IServiceProvider _services;
    private string _token = "MTM2MTk5MjAzNzIzNDM4MDk3Mg.GavpI1.k2qCkGC5GIxKO8vTumXS4RsOEaXxn5uTYmu6U8";

    static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });

        _interactions = new InteractionService(_client.Rest);
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddSingleton<QuestionService>()
            .BuildServiceProvider();

        _client.Log += LogAsync;
        _interactions.Log += LogAsync;

        _client.Ready += async () =>
        {
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _interactions.RegisterCommandsToGuildAsync(600407450272792582);
            await _interactions.RegisterCommandsGloballyAsync();
            Console.WriteLine("✅ Slash příkazy registrovány.");
        };

        _client.InteractionCreated += async interaction =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        };

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
