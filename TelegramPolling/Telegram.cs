using System.Configuration;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramPolling
{
    class Telegram
    {
        public async Task<Update[]> GetUpdates(int offset)
        {
            return await new TelegramBotClient(ConfigurationManager.AppSettings["Token"]).GetUpdatesAsync(offset);
        }
    }
}
