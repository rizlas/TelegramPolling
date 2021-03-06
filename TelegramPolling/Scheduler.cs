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
using System.Text;

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
            //SignalRTerminalsClient sc = new SignalRTerminalsClient();
            Telegram tg = new Telegram();
            bool ret = tg.FillUsers();

            if (ret)
            {
                RestClient rc = new RestClient(ConfigurationManager.AppSettings["ApiTelegram"]);
                RestRequest rr = new RestRequest();
                Update[] updates = null;

                while (true)
                {
                    try
                    {
                        updates = await tg.GetUpdates(_lastUpdateId);

                        if (updates.Length > 0)
                        {
                            _lastUpdateId = updates[updates.Length - 1].Id + 1;

                            for (int i = 0; i < updates.Length; i++)
                            {
                                string messaggio = updates[i].Message.Text;
                                User user = updates[i].Message.From;
                                string[] parametri = messaggio.Split(' ');

                                switch (parametri[0].ToLower())
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
                                                rr.AddJsonBody(new { id = -1, FirstName = user.FirstName, ChatId = user.Id, LastName = user.LastName, Username = user.Username, GroupId = 3 });

                                                #region Start Execution

                                                rc.ExecuteAsync(rr, response =>
                                                {
                                                    if (response.StatusCode == HttpStatusCode.Created)
                                                    {
                                                        Console.WriteLine("Aggiunto");
                                                        tg.Registered.Add(new TelegramUser() { id = -1, FirstName = user.FirstName, ChatId = user.Id, LastName = user.LastName, Username = user.Username, GroupId = 3 });
                                                        tg.SendMessage(user, $"Benvenuto {user.FirstName}, per la lista comandi digita /help. {_emojiHappy}. Prima di usarli dovrai attendere che l'amministratore ti aggiunga nella sua cerchia di fedeli!");
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.NotModified)
                                                    {
                                                        Console.WriteLine("Esiste");
                                                        tg.SendMessage(user, $"{user.FirstName}, risulti già iscritto.");
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                    {
                                                        InternalServerError(response, user, tg);
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
                                                tg.SendMessage(user, $"{user.FirstName}, non sei il benvenuto!!");
                                            }
                                        }
                                        else
                                        {
                                            tg.SendMessage(user, $"{user.FirstName}, non sei il benvenuto!!");
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
                                                tg.SendMessage(user, $"Ciao {user.FirstName}, ci dispiace vederti andar via. {_emojiSad}");
                                            }
                                            else if (response.StatusCode == HttpStatusCode.NotFound)
                                            {
                                                Console.WriteLine("Non esiste, impossibile eliminare");
                                                tg.SendMessage(user, $"{user.FirstName}, non risulti iscritto alla ricezione notifiche comincia ora inviando il comando /start");
                                            }
                                            else if (response.StatusCode == HttpStatusCode.InternalServerError)
                                            {
                                                InternalServerError(response, user, tg);
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
                                        StringBuilder sb = new StringBuilder();

                                        sb.AppendLine("<b>Lista comandi per il bot IGF</b>");
                                        sb.AppendLine();
                                        sb.AppendLine("<b>/start</b> - Abilita la ricezione delle notifiche");
                                        sb.AppendLine("<b>/stop</b> - Disabilita la ricezione delle notifiche");
                                        sb.AppendLine("<b>/stato</b> - Aggiungi come parametro il nome macchina e avrai lo stato di quella macchina (esempio: /stato mBR01)");
                                        sb.AppendLine("<b>/busta</b> - Aggiungi come parametro il numero di commessa e avrai il pdf della busta lavoro (esempio: /busta 546.17)");
                                        sb.AppendLine("<b>/riepilogo</b> - Aggiungi come parametro il numero di commessa e avrai il pdf del riepilogo produzione (esempio: /riepilogo 741.17)");

                                        if (IsRegisteredAndAuthorized(tg, user, 1))
                                        {
                                            sb.AppendLine("<b>/log</b> - Aggiungi come parametro il nome macchina e avrai l'ultimo log registrato del PDA associato");
                                            sb.AppendLine("<b>/online</b> - Ritorna gli utenti connessi ad IGF Droid");
                                        }

                                        sb.AppendLine("<b>/help</b> - Visualizza questa lista");

                                        string help = sb.ToString();

                                        tg.SendMessage(user, help, true);
                                        break;
                                    case "/stato":
                                        if (IsRegisteredAndAuthorized(tg, user, 2))
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
                                                            InternalServerError(response, user, tg);
                                                        }
                                                        else if (response.StatusCode == HttpStatusCode.NotFound)
                                                        {
                                                            tg.SendMessage(user, $"Non conosco questa macchina {parametri[1]}");
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
                                                    tg.SendMessage(user, $"Non conosco questa macchina {parametri[1]}");
                                                }
                                            }
                                            else
                                            {
                                                tg.SendMessage(user, "Comando errato, riprova...");
                                            }
                                        }
                                        else
                                        {
                                            tg.SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    case "/busta":
                                        if (IsRegisteredAndAuthorized(tg, user, 2))
                                        {
                                            if (parametri.Length > 1)
                                            {
                                                rr.Resource = $"api/Telegram/WorkOrder/{user.Id}/{parametri[1]}/{ConfigurationManager.AppSettings["PasswordUserIntranet"]}";
                                                rr.Method = Method.GET;

                                                #region Busta Execution

                                                rc.ExecuteAsync(rr, response =>
                                                {
                                                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                    {
                                                        InternalServerError(response, user, tg);
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.NotFound)
                                                    {
                                                        tg.SendMessage(user, $"Non conosco questa commessa {parametri[1]}");
                                                        log.Error($"Non conosco questa commessa {parametri[1]}, {user.FirstName}");
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
                                                tg.SendMessage(user, "Comando errato, riprova...");
                                            }
                                        }
                                        else
                                        {
                                            tg.SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    case "/riepilogo":
                                        if (IsRegisteredAndAuthorized(tg, user, 2))
                                        {
                                            if (parametri.Length > 1)
                                            {
                                                rr.Resource = $"api/Telegram/WorkSummary/{user.Id}/{parametri[1]}/{ConfigurationManager.AppSettings["PasswordUserIntranet"]}";
                                                rr.Method = Method.GET;

                                                #region Riepilogo Execution

                                                rc.ExecuteAsync(rr, response =>
                                                {
                                                    if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                    {
                                                        InternalServerError(response, user, tg);
                                                    }
                                                    else if (response.StatusCode == HttpStatusCode.NotFound)
                                                    {
                                                        tg.SendMessage(user, $"Non conosco questa commessa {parametri[1]}");
                                                        log.Error($"Non conosco questa commessa {parametri[1]}, {user.FirstName}");
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
                                                tg.SendMessage(user, "Comando errato, riprova...");
                                            }
                                        }
                                        else
                                        {
                                            tg.SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    case "/log":
                                        if (IsRegisteredAndAuthorized(tg, user, 1))
                                        {
                                            if (parametri.Length > 1)
                                            {
                                                if (parametri[1].Length == 5)
                                                {
                                                    rr.Resource = $"api/PdaMachine/PdaCommand/log/{parametri[1]}/{user.Id}";
                                                    rr.Method = Method.GET;

                                                    #region Log Execution

                                                    rc.ExecuteAsync(rr, response =>
                                                    {
                                                        if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                        {
                                                            InternalServerError(response, user, tg);
                                                        }
                                                        else if (response.StatusCode == HttpStatusCode.NotFound)
                                                        {
                                                            tg.SendMessage(user, response.Content);
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
                                                    tg.SendMessage(user, $"Non conosco questa macchina {parametri[1]}");
                                                }
                                            }
                                            else
                                            {
                                                tg.SendMessage(user, "Comando errato, riprova...");
                                            }
                                        }
                                        else
                                        {
                                            tg.SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    case "/online":
                                        if (IsRegisteredAndAuthorized(tg, user, 1))
                                        {
                                            rr.Resource = $"api/Telegram/GetLogged/{user.Id}";
                                            rr.Method = Method.GET;

                                            #region Online Execution

                                            rc.ExecuteAsync(rr, response =>
                                            {
                                                if (response.StatusCode == HttpStatusCode.InternalServerError)
                                                {
                                                    InternalServerError(response, user, tg);
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
                                            tg.SendMessage(user, "Non sei autorizzato ad usare questo comando o non sei registrato!");
                                        }
                                        break;
                                    default:
                                        tg.SendMessage(user, $"Comando sconosciuto {_emojiSad}");
                                        Console.WriteLine($"Messaggio: {messaggio}");
                                        break;
                                }

                                Console.WriteLine($"Last Id: {_lastUpdateId}");
                            }

                            GC.Collect();
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

        private static bool IsRegisteredAndAuthorized(Telegram tg, User user, int minLevelAuthorize)
        {
            return tg.Registered.Find(u => u.ChatId == user.Id) != null && tg.Registered.Find(u => u.ChatId == user.Id).GroupId <= minLevelAuthorize;
        }

        private static void InternalServerError(IRestResponse response, User user, Telegram tg)
        {
            ExceptionModel ex = JsonConvert.DeserializeObject<ExceptionModel>(response.Content);
            string toLog = $"{ex.Message}{Environment.NewLine}{ex.ExceptionMessage}{Environment.NewLine}{ex.ExceptionType}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
            Console.WriteLine(toLog);
            log.Error(toLog);
            tg.SendMessage(user, $"Purtroppo non sono riuscito a portare a termine la tua richiesta {_emojiSad}");
        }

        private static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{9})$").Success;
        }
    }
}
