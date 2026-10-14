namespace HalalStocks.Models;

public class VideoCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Video> Videos { get; set; } = new List<Video>();
}
