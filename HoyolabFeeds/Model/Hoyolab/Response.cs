namespace HoyolabFeeds.Model.Hoyolab;

public class Response
{
    public required uint Retcode { get; set; }
    public required string Message { get; set; }
    public required ResponseData Data { get; set; }
}
