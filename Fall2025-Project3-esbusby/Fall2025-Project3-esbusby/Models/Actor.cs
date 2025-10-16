using System.ComponentModel.DataAnnotations;

namespace Fall2025_Project3_esbusby.Models
{
    public class Actor
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public int Age { get; set; }
        public string? ImdbLink { get; set; }
        public byte[]? Photo { get; set; }

        public ICollection<ActorMovie>? ActorMovies { get; set; }

    }
}
