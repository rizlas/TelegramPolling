namespace TelegramPolling
{
    public partial class TelegramUser
    {
        public int id { get; set; }
        public string FirstName { get; set; }
        public int ChatId { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public int GroupId { get; set; }

        public virtual TelegramGroup TelegramGroup { get; set; }
    }
}
