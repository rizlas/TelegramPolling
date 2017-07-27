using Microsoft.AspNet.SignalR.Client;
using System;
using System.Configuration;
using IGF.Manufactoring.Api.Models;
using log4net;
using Telegram.Bot.Types;
using RestSharp;

namespace TelegramPolling
{
    public class SignalRTerminalsClient
    {
        HubConnection _hubConnection;
        IHubProxy _hubProxyMachine;

        Telegram tg;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SignalRTerminalsClient()
        {
            _hubConnection = new HubConnection($"{ConfigurationManager.AppSettings["ApiManufactoring"]}signalr/");
            _hubProxyMachine = _hubConnection.CreateHubProxy("machinemessages");
            tg = new Telegram();
            this.Run();
        }

        private void Run()
        {
            try
            {
                _hubConnection.Start().Wait();
                _hubProxyMachine.Invoke("Subscribe", "AllEvents").Wait();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                Console.WriteLine($"Exception {ex.Message}");
            }

            _hubProxyMachine.On<MachineStatus>("StatusChanged", (status) =>
            {
                string message = $"La macchina <b>{status.MachineId}</b> ha cambiato il suo stato in <b>{status.Status.Description}</b>";

                RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
                RestRequest rr = new RestRequest();
                rr.Resource = $"api/Telegram/SendMessage/{ConfigurationManager.AppSettings["TelegramChannel"]}/{true}";
                rr.Method = Method.POST;
                rr.AddJsonBody(message);

                var resp = rc.Execute(rr);

                if (resp.ErrorException != null)
                {
                    Console.WriteLine($"{resp.ErrorMessage}{Environment.NewLine}{resp.ErrorException.StackTrace}");
                    log.Error($"{resp.ErrorMessage}{Environment.NewLine}{resp.ErrorException.StackTrace}");
                }
            });
        }
    }
}