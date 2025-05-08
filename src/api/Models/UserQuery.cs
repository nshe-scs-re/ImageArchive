using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class UserQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryId { get; set; }

        public string UserId { get; set; }

        public string Parameters { get; set; }

        public DateTime Timestamp { get; set; }
    }
}