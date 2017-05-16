namespace TelegramPolling
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione componenti

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.TelegramPollingProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.TelegramPollingInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // TelegramPollingProcessInstaller
            // 
            this.TelegramPollingProcessInstaller.Account = System.ServiceProcess.ServiceAccount.NetworkService;
            this.TelegramPollingProcessInstaller.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.TelegramPollingInstaller});
            this.TelegramPollingProcessInstaller.Password = null;
            this.TelegramPollingProcessInstaller.Username = null;
            // 
            // TelegramPollingInstaller
            // 
            this.TelegramPollingInstaller.Description = "Polling aggiornamenti bot telegram avvisi";
            this.TelegramPollingInstaller.ServiceName = "Telegram polling";
            this.TelegramPollingInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.TelegramPollingProcessInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller TelegramPollingProcessInstaller;
        private System.ServiceProcess.ServiceInstaller TelegramPollingInstaller;
    }
}