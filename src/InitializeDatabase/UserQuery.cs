using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InitializeDatabase
{
    public class UserQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryId { get; set; }

        public required string UserId { get; set; }

        public required string Parameters { get; set; }

        public DateTime Timestamp { get; set; }
    }
}