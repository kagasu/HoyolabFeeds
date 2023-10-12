using HoyolabFeeds.Model.Hoyolab;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace HoyolabFeeds.Controllers;
[ApiController]
[Route("[controller]")]
public class FeedsController : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory;

    public FeedsController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    private async Task<ResponseData> GetUserPosts(string userId, string language)
    {
        var url = $"https://bbs-api-os.hoyolab.com/community/post/wapi/userPost?size=15&uid={userId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Rpc-Language", language);
        var client = _clientFactory.CreateClient();
        var httpResponse = await client.SendAsync(request).ConfigureAwait(false);
        var str =  await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        Response? response;
        try
        {
            response = JsonSerializer.Deserialize<Response>(str, options);
        }
        catch (JsonException)
        {
            throw new Exception("Failed to deserialize json");
        }

        if (response?.Retcode != 0)
        {
            throw new Exception("Invalid retcode");
        }

        return response.Data;
    }

    private SyndicationFeed GenerateSyndicationFeed(ResponseData data)
    {
        var feed = new SyndicationFeed();
        var user = data.List.FirstOrDefault()!.User;
        
        feed.Title = new TextSyndicationContent(user.Nickname);
        feed.Description = new TextSyndicationContent(user.Introduce);
        feed.Links.Add(new SyndicationLink(new Uri($"https://www.hoyolab.com/accountCenter/postList?id={user.Uid}")));
        feed.ImageUrl = new Uri(user.AvatarUrl);
        feed.LastUpdatedTime = new DateTimeOffset(DateTime.Now);
        feed.Generator = "HoyolabFeeds";

        var baseUrl = "https://www.hoyolab.com/article/";
        var items = new List<SyndicationItem>();
        foreach (var postInfo in data.List)
        {
            var item = new SyndicationItem();
            item.Id = postInfo.Post.PostId;
            item.Title = new TextSyndicationContent(postInfo.Post.Subject);
            item.Links.Add(new SyndicationLink(new Uri($"{baseUrl}{postInfo.Post.PostId}")));
            item.Summary = new TextSyndicationContent(postInfo.Post.Content);
            item.PublishDate = DateTimeOffset.FromUnixTimeSeconds(postInfo.Post.CreatedAt);
            items.Add(item);
        }

        feed.Items = items;

        return feed;
    }

    private MemoryStream GenerateFeedXmlStream(string type, SyndicationFeed feed)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true
        };

        var ms = new MemoryStream();
        using var writer = XmlWriter.Create(ms, settings);
        switch (type)
        {
            case "rss":
                feed.SaveAsRss20(writer);
                break;
            case "atom":
                feed.SaveAsAtom10(writer);
                break;
            default:
                throw new Exception("Invalid type");
        }
        writer.Flush();
        ms.Position = 0;

        return ms;
    }

    [HttpGet("{userId}/{language}/{type}")]
    public async Task<IActionResult> GetRss(string userId, string language, string type)
    {
        var postInfos = await GetUserPosts(userId, language).ConfigureAwait(false);
        var feed = GenerateSyndicationFeed(postInfos);

        var ms = GenerateFeedXmlStream(type, feed);
        return new FileStreamResult(ms, "application/xml");
    }
}
