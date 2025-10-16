using System.ComponentModel.DataAnnotations;

namespace Fall2025_Project3_esbusby.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? ImdbLink { get; set; }
        public string? Genre { get; set; }
        public int YearOfRelease { get; set; }
        public byte[]? Poster { get; set; }
        public ICollection<ActorMovie>? ActorMovies { get; set; }

    }
}
