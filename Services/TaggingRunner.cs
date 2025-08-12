using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;

public class TaggingRunner
{
    private readonly OpenAiTagService _tagService;
    private readonly EmailService _mailer;
    private readonly DBServices _db;

    public TaggingRunner(IConfiguration config)
    {
        // OpenAI tag detection service (needs config for API key)
        _tagService = new OpenAiTagService(config);

        // Simple SMTP mailer (kept as-is)
        _mailer = new EmailService();

        // Data access layer (all DB calls go through here)
        _db = new DBServices();
    }

    /// <summary>
    /// Orchestrates the flow:
    /// 1) Load untagged articles from DB (SP).
    /// 2) Ask OpenAI for relevant tags.
    /// 3) Persist tags via DB SPs (idempotent).
    /// 4) Notify users who follow these tags.
    /// </summary>
    public async Task RunAsync()
    {
        var articles = _db.GetUntaggedArticles();

        foreach (var article in articles)
        {
            try
            {
                // 1) Detect relevant tags via OpenAI (comma list filtered to allow-list in service)
                var tagNames = await _tagService.DetectTagsAsync(article.Title, article.Content);
                if (tagNames == null || tagNames.Count == 0)
                    continue;

                // 2) Upsert tag ids and link to article (idempotent insert)
                var tagIds = new List<int>();
                foreach (var name in tagNames)
                {
                    int tagId = _db.GetOrAddTagId(name);
                    tagIds.Add(tagId);
                    _db.InsertArticleTag(article.Id, tagId);
                }

                // 3) Notify users who are interested in any of these tags
                NotifyInterestedUsers(article, tagIds);

                Console.WriteLine($"✓ Tagged & notified: Article {article.Id}");

                // Small delay to avoid hammering external services
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error Article {article.Id}: {ex.Message}");
            }
        }

        Console.WriteLine("✓ All articles tagged and notifications sent.");
    }

    /// <summary>
    /// Finds users who follow any of the given tag IDs and sends them an email.
    /// Uses the CSV-based SP wrapper (GetUsersInterestedInTagsCsv).
    /// </summary>
    private void NotifyInterestedUsers(Article article, List<int> tagIds)
    {
        if (tagIds == null || tagIds.Count == 0) return;

        // Uses the CSV-based SP: NewsSP_GetUsersInterestedInTags(@TagIdsCsv)
        // אם ה-SP כבר מסנן ReceiveNotifications=1, הסינון הנוסף כאן הוא רק ליתר ביטחון.
        List<User> users = _db.GetUsersInterestedInTags(tagIds)
                              .Where(u => u.ReceiveNotifications)
                              .ToList();

        foreach (var user in users)
        {
            string subject = "📰 New article in your interest!";
            string body = $@"
Hello {user.Name},<br/><br/>
A new article has been published that matches your interest:<br/>
<b>{article.Title}</b><br/><br/>
<a href='https://your-site-url.com'>Click here to read it on our website</a>";

            try
            {
                _mailer.Send(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send to {user.Email}: {ex.Message}");
            }
        }
    }

}
