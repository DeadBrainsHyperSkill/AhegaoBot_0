using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Handlers;
using DiscordBot.Services;

namespace DiscordBot.Modules;

public class Command : ModuleBase<SocketCommandContext>
{
    [Command("Синхронизировать")]
    public async Task Sync([Remainder] string input)
    {
        if (Context.User.Id != 891369679451480105)
        {
            return;
        }
        string[] id = input.Split(' ');
        CommandsService.InteractionWithBot(id[0], id[1]);
        await Context.Message.AddReactionAsync(new Emoji("☑️"));
    }

    [Command("SelectMenus")]
    public async Task SelectMenus()
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId("содержание")
            .WithPlaceholder("Выберите интересующий вас раздел.")
            .AddOption("Навигация", "навигация", "Структура сервера Ахегао.", Emoji.Parse(":compass:"))
            .AddOption("Правила", "правила", "Принципы сообщества Ахегао.", Emoji.Parse(":notebook:"))
            .AddOption("Пространства", "пространства", "Руководство по текстовым и голосовым каналам.", Emoji.Parse(":milky_way:"))
            .AddOption("Роли", "роли", "Упорядоченная информация по ролям.", Emoji.Parse(":performing_arts:"));

        var builder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

        await ReplyAsync(embed: await EmbedHandler.CreateNavigationEmbed(), components: builder.Build());
    }
}