using System.Threading.Tasks;
using DiscordBot.Services;

namespace DiscordBot;

class Program
{
    private static Task Main()
        => new DiscordService().InitializeAsync();
}
