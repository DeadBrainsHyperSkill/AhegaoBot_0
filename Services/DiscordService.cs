using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Handlers;

namespace DiscordBot.Services;

public class DiscordService
{
    public readonly DiscordSocketClient _client;
    private readonly CommandHandler _commandHandler;
    private readonly ServiceProvider _services;

    public DiscordService()
    {
        _services = ConfigureServices();
        _client = _services.GetRequiredService<DiscordSocketClient>();
        _commandHandler = _services.GetRequiredService<CommandHandler>();
    }

    public async Task InitializeAsync()
    {
        await _client.LoginAsync(TokenType.Bot, "<token>");
        await _client.StartAsync();
        await _commandHandler.InitializeAsync();
        await Task.Delay(-1);
    }

    private static ServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig() { MessageCacheSize = 100, GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.GuildMembers | GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMessageReactions }))
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandler>()
            .BuildServiceProvider();
    }
}
