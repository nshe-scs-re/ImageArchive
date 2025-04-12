using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InitializeDatabase.Models
{
    public class UserQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QueryId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Parameters { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }
    }
}