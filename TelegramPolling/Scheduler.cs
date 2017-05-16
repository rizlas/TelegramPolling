using System;
using System.ServiceProcess;
using System.Threading;
using Telegram.Bot.Types;
using RestSharp;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;

namespace TelegramPolling
{
    public partial class Scheduler : ServiceBase
    {
        Thread _threadOnStart;
        int lastUpdateId = 0;

        public Scheduler()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            this.Dispose();
        }

        public void Start()
        {
            inizializzaThread();
        }

        private void inizializzaThread()
        {
            _threadOnStart = new Thread(StartThread);
            _threadOnStart.Name = "StartThread";
            _threadOnStart.IsBackground = false;
            _threadOnStart.Start();
        }

        private async void StartThread()
        {
            Telegram tg = new Telegram();
            RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
            RestRequest rr = new RestRequest();

            while (true)
            {
                Update[] updates = await tg.GetUpdates(lastUpdateId);

                if(updates.Length > 0)
                {
                    lastUpdateId = updates[updates.Length - 1].Id + 1;

                    for (int i = 0; i < updates.Length; i++)
                    {
                        string messaggio = updates[i].Message.Text;
                        User user = updates[i].Message.From;

                        switch (messaggio)
                        {
                            case "/start":
                                Console.WriteLine($"Utente: {updates[i].Message.From.FirstName} registrato");
                                rr.Resource = "api/Telegram/Subscribe";
                                rr.Method = Method.POST;
                                rr.AddJsonBody(new { id = -1, FirstName = user.FirstName, ChatId = user.Id, LastName = user.LastName, Username = user.Username });

                                rc.ExecuteAsync(rr, response =>
                                {
                                    if (response.StatusCode == HttpStatusCode.Created)
                                    {
                                        Console.WriteLine("Aggiunto");
                                        SendMessage(user, $"Benvenuto {user.Username}, per la lista comandi digita /help.");
                                    }
                                    else if (response.StatusCode == HttpStatusCode.NotModified)
                                    {
                                        Console.WriteLine("Esiste");
                                        SendMessage(user, $"{user.Username}, risulti già iscritto.");
                                    }
                                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                    {
                                        ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
                                        Console.WriteLine($"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                                    }
                                    else
                                        Console.WriteLine(response.Content);
                                });
                                break;
                            case "/stop":
                                Console.WriteLine($"Utente: {updates[i].Message.From.FirstName} rimosso");
                                rr.Resource = $"api/Telegram/Unsubscribe/{user.Id}";
                                rr.Method = Method.DELETE;

                                rc.ExecuteAsync(rr, response =>
                                {
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        Console.WriteLine("Rimosso");
                                        SendMessage(user, $"Ciao {user.Username}, ci dispiace vederti andar via. :(");
                                    }
                                    else if (response.StatusCode == HttpStatusCode.NotFound)
                                    {
                                        Console.WriteLine("Non esiste, impossibile eliminare");
                                        SendMessage(user, $"{user.Username}, non risulti iscritto alla ricezione notifiche comincia ora inviando il comando /start");
                                    }
                                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                    {
                                        ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
                                        Console.WriteLine($"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                                    }
                                    else
                                        Console.WriteLine(response.Content);
                                });
                                break;
                            case "/help":
                                Console.WriteLine($"/start - Abilita ricezione notifiche{Environment.NewLine}/stop - Disabilita ricezione notifche{Environment.NewLine}/help - Fa vedere questa lista");
                                SendMessage(user, $"Lista comandi per il bot IGF Avvisi{Environment.NewLine}{Environment.NewLine}/start - Abilita la ricezione delle notifiche{Environment.NewLine}/stop - Disabilita la ricezione delle notifiche{Environment.NewLine}/help - Visualizza questa lista", true);
                                break;
                            default:
                                Console.WriteLine($"Messaggio: {messaggio}");
                                break;
                        }

                        Console.WriteLine($"Last Id: {lastUpdateId}");
                    }
                }
            }
        }

        private static void SendMessage(User user, string message, bool Html = false)
        {
            RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
            RestRequest rr = new RestRequest();
            rr.Resource = $"api/Telegram/SendMessage/{user.Id}/{Html}";
            rr.Method = Method.POST;
            rr.AddJsonBody(message);

            rc.Execute(rr);
        }
    }
}
