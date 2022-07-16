using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;

namespace DiscordBot.Handlers;

public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;

    public CommandHandler(IServiceProvider services)
    {
        _commands = services.GetRequiredService<CommandService>();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _services = services;

        HookEvents();
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(
            assembly: Assembly.GetEntryAssembly(),
            services: _services);
    }

    public void HookEvents()
    {
        _commands.CommandExecuted += HandleCommandExecuted;
        _client.MessageReceived += HandleCreatedMessage;
        _client.MessageDeleted += HandleDeletedMessage;
        _client.UserVoiceStateUpdated += HandleVoiceStatedUpdated;
        _client.UserJoined += HandleUserJoined;
        _client.SelectMenuExecuted += HandleSelectMenuExecuted;
    }

    private async Task HandleDeletedMessage(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel)
    {
        if (messageChannel.Id == 836964413298704395)
        {
            var user = (SocketGuildUser)await message.GetOrDownloadAsync().ContinueWith(m => m.Result.Author);
            var messages = await messageChannel.GetOrDownloadAsync().ContinueWith(ch => ch.Result.GetMessagesAsync().FlattenAsync().Result);
            if (messages.FirstOrDefault(m => m.Author == user && m.Attachments.Count != 0) == null && user.Roles.Any(r => r.Id == 846079780284661760))
            {
                await user.RemoveRoleAsync(user.Guild.GetRole(846079780284661760));
            }
        }
    }

    private async Task HandleSelectMenuExecuted(SocketMessageComponent component)
    {
        Embed embed = null;
        switch (string.Join(", ", component.Data.Values))
        {
            case "навигация":
                embed = await EmbedHandler.CreateNavigationEmbed();
                break;
            case "правила":
                embed = await EmbedHandler.CreateRulesEmbed();
                break;
            case "пространства":
                embed = await EmbedHandler.CreateSpacesEmbed();
                break;
            case "роли":
                embed = await EmbedHandler.СreateRolesEmbed();
                break;

        }
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
        });
    }

    private async Task HandleUserJoined(SocketGuildUser socketGuildUser)
    {
        await CommandsService.AddDefaultRoles(socketGuildUser);
    }

    private async Task HandleVoiceStatedUpdated(SocketUser socketUser, SocketVoiceState socketVoiceStatePrevious, SocketVoiceState socketVoiceState)
    {
        SocketGuildUser user = socketUser as SocketGuildUser;
        await CommandsService.CheckUserDeafenedOrMuted(user, socketVoiceState, socketVoiceStatePrevious);
        await CommandsService.CreateDeleteChannels(user, socketVoiceState, socketVoiceStatePrevious);
    }

    private async Task HandleCreatedMessage(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message || message.Author.IsWebhook || message.Channel is IPrivateChannel)
            return;

        var user = (SocketGuildUser)socketMessage.Author;
        if (socketMessage.Channel.Id == 836964413298704395)
        {
            if (socketMessage.Attachments.Count == 0)
            {
                await socketMessage.DeleteAsync();
            }
            else if (!user.Roles.Any(r => r.Id == 846079780284661760))
            {
                await user.AddRoleAsync(user.Guild.GetRole(846079780284661760));
            }
            await socketMessage.AddReactionAsync(new Emoji("❤️"));
        }

        var argPos = 0;
        if (!message.HasStringPrefix("!", ref argPos))
            return;

        var context = new SocketCommandContext(_client, socketMessage as SocketUserMessage);

        await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
    }

    public static async Task HandleCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (!command.IsSpecified)
            return;

        if (result.IsSuccess)
            return;

        var embed = await Task.Run(() => new EmbedBuilder()
            .WithTitle(result.ToString())
            .WithDescription($"Используйте команду ***!help*** для получения информации")
            .WithColor(Color.Red)
            .WithFooter(footer => footer.Text = $"Если бот не функционирует или работает нестабильно, пожалуйста, обратитесь к DeadBrains_HyperSkill#0319")
            .Build());
        await context.Channel.SendMessageAsync("", false, embed);
    }
}