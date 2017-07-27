using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramPolling
{
    class Telegram
    {
        List<TelegramUser> registered;

        public List<TelegramUser> Registered
        {
            get
            {
                return registered;
            }

            set
            {
                registered = value;
            }
        }

        public Telegram()
        {
            registered = new List<TelegramUser>();
        }

        public async Task<Update[]> GetUpdates(int offset)
        {
            return await new TelegramBotClient(ConfigurationManager.AppSettings["Token"]).GetUpdatesAsync(offset);
        }

        public bool FillUsers()
        {
            RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
            RestRequest rr = new RestRequest();
            rr.Resource = "api/Telegram/";
            rr.Method = Method.GET;

            var response = rc.Execute(rr);

            if(response.StatusCode == HttpStatusCode.OK)
            {
                List<TelegramUser> users = JsonConvert.DeserializeObject<List<TelegramUser>>(response.Content);
                registered.AddRange(users);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
