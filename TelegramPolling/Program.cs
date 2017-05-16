using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TelegramPolling
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)//&& System.Diagnostics.Debugger.IsAttached)
            {
                Scheduler telegramPolling = new Scheduler();
                telegramPolling.Start();

                Console.WriteLine("Premi un tasto per continuare...");
                Console.ReadKey();

                telegramPolling.Stop();
            }
            else
            {
                ServiceBase.Run(new Scheduler());
            }
        }
    }
}
