using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramPolling
{
    public partial class TelegramGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TelegramGroup()
        {
            this.TelegramUsers = new HashSet<TelegramUser>();
        }

        public int ID { get; set; }
        public string Gruppo { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TelegramUser> TelegramUsers { get; set; }
    }
}
