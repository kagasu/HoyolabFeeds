namespace HoyolabFeeds.Model;

public class Post
{
    public required string PostId { get; set; }
    public required string Uid { get; set; }
    public required string Subject { get; set; }
    public required string Content { get; set; }
    public required long CreatedAt { get; set; }
    public required string Lang { get; set; }
    public required string OriginLang { get; set; }
}
