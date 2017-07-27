﻿using System;
using System.ServiceProcess;
using System.Threading;
using Telegram.Bot.Types;
using RestSharp;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using log4net;
using System.Text.RegularExpressions;

namespace TelegramPolling
{
    public partial class Scheduler : ServiceBase
    {
        Thread _threadOnStart;
        int _lastUpdateId = 0;
        const string _emojiSad = "\U0001F614";
        const string _emojiHappy = "\U0001F60A";
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            log.Info("Servizio fermato....");
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

            log.Info("Servizio partito....");
        }

        private async void StartThread()
        {
            Telegram tg = new Telegram();
            bool ret = tg.FillUsers();

            if (ret)
            {
                RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
                RestRequest rr = new RestRequest();

                while (true)
                {
                    try
                    {
                        Update[] updates = await tg.GetUpdates(_lastUpdateId);

                        if (updates.Length > 0)
                        {
                            _lastUpdateId = updates[updates.Length - 1].Id + 1;

                            for (int i = 0; i < updates.Length; i++)
                            {
                                string messaggio = updates[i].Message.Text;
                                User user = updates[i].Message.From;
                                string[] parametri = messaggio.Split(' ');

                                switch (parametri[0])
                                {
                                    case "/start":
                                        //if(IsPhoneNumber(parametri[1]))
                                        //{
                                        //    // Authentication with swyx server
                                        //}
                                        //else
                                        //{

                                        //}

                                        if (parametri.Length >= 2)
                                        {
                                            if (parametri[1].CompareTo(ConfigurationManager.AppSettings["BotPassword"].ToString()) == 0)
                                            {
                                                Console.WriteLine($"Utente: {updates[i].Message.From.FirstName} registrato");
                                                rr.Resource = "api/Telegram/Subscribe";
                                                rr.Method = Method.POST;
                                                rr.AddJsonBody(new { id = -1, FirstName = user.FirstName, ChatId = user.Id, LastName = user.LastName, Username = user.Username });

                                                #region Start Execution

                                                rc.ExecuteAsync(rr, response =>
                                                {
                                                    if (response.StatusCode == HttpStatusCode.Created)
                                                    {
                                                        Console.WriteLine("Aggiunto");
                                                        tg.Registered.Add(new TelegramUser() { id = -1, FirstName = user.FirstName, ChatId = user.Id, LastName = user.LastName, Username = user.Username });
                                                        SendMessage(user, $"Benvenuto {user.FirstName}, per la lista comandi digita /help. {_emojiHappy}");
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.NotModified)
                                                    {
                                                        Console.WriteLine("Esiste");
                                                        SendMessage(user, $"{user.FirstName}, risulti già iscritto.");
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                    {
                                                        ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
                                                        string toLog = $"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                                                        Console.WriteLine(toLog);
                                                        log.Error(toLog);
                                                    }
                                                    else if (response.StatusCode != HttpStatusCode.OK)
                                                    {
                                                        Console.WriteLine(response.Content);
                                                        log.Warn($"Code: {response.StatusCode} Content: {response.Content}");
                                                    }
                                                });
                                            }
                                            else
                                            {
                                                SendMessage(user, $"{user.FirstName}, non sei il benvenuto!!");
                                            }
                                        }
                                        else
                                        {
                                            SendMessage(user, $"{user.FirstName}, non sei il benvenuto!!");
                                        }
                                        #endregion

                                        break;
                                    case "/stop":
                                        Console.WriteLine($"Utente: {updates[i].Message.From.FirstName} rimosso");
                                        rr.Resource = $"api/Telegram/Unsubscribe/{user.Id}";
                                        rr.Method = Method.DELETE;

                                        #region Stop Execution

                                        rc.ExecuteAsync(rr, response =>
                                        {
                                            if (response.StatusCode == HttpStatusCode.OK)
                                            {
                                                Console.WriteLine("Rimosso");
                                                tg.Registered.RemoveAll(u => u.ChatId == user.Id);
                                                SendMessage(user, $"Ciao {user.FirstName}, ci dispiace vederti andar via. {_emojiSad}");
                                            }
                                            else if (response.StatusCode == HttpStatusCode.NotFound)
                                            {
                                                Console.WriteLine("Non esiste, impossibile eliminare");
                                                SendMessage(user, $"{user.FirstName}, non risulti iscritto alla ricezione notifiche comincia ora inviando il comando /start");
                                            }
                                            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                            {
                                                ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
                                                string toLog = $"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                                                Console.WriteLine(toLog);
                                                log.Error(toLog);
                                            }
                                            else if (response.StatusCode != HttpStatusCode.OK)
                                            {
                                                Console.WriteLine(response.Content);
                                                log.Warn($"Code: {response.StatusCode} Content: {response.Content}");
                                            }
                                        });

                                        #endregion

                                        break;
                                    case "/help":
                                        Console.WriteLine($"/start - Abilita ricezione notifiche{Environment.NewLine}/stop - Disabilita ricezione notifche{Environment.NewLine}/help - Fa vedere questa lista");
                                        SendMessage(user, $"<b>Lista comandi per il bot IGF Avvisi</b>{Environment.NewLine}{Environment.NewLine}<b>/start</b> - Abilita la ricezione delle notifiche{Environment.NewLine}<b>/stop</b> - Disabilita la ricezione delle notifiche{Environment.NewLine}<b>/help</b> - Visualizza questa lista{Environment.NewLine}<b>/stato</b> - aggiungi come parametro il nome macchina e avrai lo stato di quella macchina (esempio: /stato mBR01)", true);
                                        break;
                                    case "/stato":
                                        if (tg.Registered.Find(u => u.ChatId == user.Id) != null)
                                        {
                                            if (parametri.Length > 1)
                                            {
                                                if (parametri[1].Length == 5)
                                                {
                                                    rr.Resource = $"api/Telegram/Statuses/{user.Id}/{parametri[1]}";
                                                    rr.Method = Method.GET;

                                                    #region Stato Execution

                                                    rc.ExecuteAsync(rr, response =>
                                                    {
                                                        if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                        {
                                                            ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
                                                            string toLog = $"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                                                            Console.WriteLine(toLog);
                                                            log.Error(toLog);
                                                        }
                                                        else if (response.StatusCode == HttpStatusCode.NotFound)
                                                        {
                                                            SendMessage(user, $"Non conosco questa macchina {parametri[1]}");
                                                            log.Error($"Non conosco questa macchina {parametri[1]}, {user.FirstName}");
                                                        }
                                                        else if (response.StatusCode != HttpStatusCode.OK)
                                                        {
                                                            if (response.ErrorException != null)
                                                                log.Error(response.ErrorException.Message);

                                                            Console.WriteLine(response.Content);
                                                            log.Warn($"Code: {response.StatusCode} Content: {response.Content}");
                                                        }
                                                    });

                                                    #endregion
                                                }
                                                else
                                                {
                                                    SendMessage(user, $"Non conosco questa macchina {parametri[1]}");
                                                }
                                            }
                                            else
                                            {
                                                SendMessage(user, "Comando errato, riprova...");
                                            }
                                        }
                                        else
                                        {
                                            SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    default:
                                        Console.WriteLine($"Messaggio: {messaggio}");
                                        break;
                                }

                                Console.WriteLine($"Last Id: {_lastUpdateId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    }
                }
            }
            else
            {
                log.Error("Non sono riuscito a determinare gli utenti!");
            }
        }

        private static void SendMessage(User user, string message, bool Html = false)
        {
            try
            {
                RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
                RestRequest rr = new RestRequest();
                rr.Resource = $"api/Telegram/SendMessage/{user.Id}/{Html}";
                rr.Method = Method.POST;
                rr.AddJsonBody(message);

                var resp = rc.Execute(rr);

                if(resp.ErrorException != null)
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

        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{9})$").Success;
        }
    }
}
