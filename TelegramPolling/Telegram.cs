using log4net;
using Newtonsoft.Json;
using RestSharp;
using System;
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
        TelegramBotClient clientTelegram;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            registered = new List<TelegramUser>();
            clientTelegram = new TelegramBotClient(ConfigurationManager.AppSettings["Token"]);
        }

        public async Task<Update[]> GetUpdates(int offset)
        {
            return await clientTelegram.GetUpdatesAsync(offset);
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

        public void SendMessage(User user, string message, bool Html = false)
        {
            try
            {
                RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
                RestRequest rr = new RestRequest();
                rr.Resource = $"api/Telegram/SendMessage/{user.Id}/{Html}";
                rr.Method = Method.POST;
                rr.AddJsonBody(message);

                var resp = rc.Execute(rr);

                if (resp.ErrorException != null)
                {
                    Console.WriteLine($"{resp.ErrorMessage}{Environment.NewLine}{resp.ErrorException.StackTrace}");
                    log.Error($"{resp.ErrorMessage}{Environment.NewLine}{resp.ErrorException.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                log.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
        }
    }
}
