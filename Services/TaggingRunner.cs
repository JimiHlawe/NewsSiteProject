using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;

public class TaggingRunner
{
    private readonly OpenAiTagService _tagService;
    private readonly EmailService _mailer;
    private readonly DBServices _db;
    private readonly string _connectionString;

    public TaggingRunner(IConfiguration config)
    {
        _tagService = new OpenAiTagService(config);
        _mailer = new EmailService();
        _db = new DBServices();
        _connectionString = config.GetConnectionString("myProjDB");
    }

    // ✅ Main entry point to tag articles and notify interested users
    public async Task RunAsync()
    {
        var articles = GetArticlesFromDb();

        foreach (var article in articles)
        {
            try
            {
                var tags = await _tagService.DetectTagsAsync(article.Title, article.Content);
                SaveTagsToDb(article.Id, tags);
                NotifyInterestedUsers(article, tags);

                Console.WriteLine($"✓ Tagged and notified for article ID: {article.Id}");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error processing article ID {article.Id}: {ex.Message}");
            }
        }

        Console.WriteLine("✓ All articles tagged and notifications sent.");
    }

    // ✅ Retrieves all articles that do not yet have tags
    private List<Article> GetArticlesFromDb()
    {
        var list = new List<Article>();

        using (SqlConnection con = _db.connect())
        {
            SqlCommand cmd = new SqlCommand(@"
                SELECT A.id, A.title, A.content
                FROM News_Articles A
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM News_ArticleTags T
                    WHERE T.articleId = A.id
                )", con);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Article
                {
                    Id = (int)reader["id"],
                    Title = reader["title"].ToString(),
                    Content = reader["content"].ToString()
                });
            }
        }

        return list;
    }

    // ✅ Saves tags to the database for a given article
    private void SaveTagsToDb(int articleId, List<string> tags)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        foreach (var tag in tags)
        {
            int tagId = GetOrCreateTagId(tag, con);

            try
            {
                var cmd = new SqlCommand("INSERT INTO News_ArticleTags (articleId, tagId) VALUES (@articleId, @tagId)", con);
                cmd.Parameters.AddWithValue("@articleId", articleId);
                cmd.Parameters.AddWithValue("@tagId", tagId);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601) // Duplicate key
                    continue;
                throw;
            }
        }
    }

    // ✅ Gets an existing tag ID or creates a new one if it doesn't exist
    private int GetOrCreateTagId(string tagName, SqlConnection con)
    {
        var checkCmd = new SqlCommand("SELECT id FROM News_Tags WHERE name = @name", con);
        checkCmd.Parameters.AddWithValue("@name", tagName);
        var result = checkCmd.ExecuteScalar();

        if (result != null)
            return Convert.ToInt32(result);

        var insertCmd = new SqlCommand("INSERT INTO News_Tags (name) OUTPUT INSERTED.id VALUES (@name)", con);
        insertCmd.Parameters.AddWithValue("@name", tagName);
        return (int)insertCmd.ExecuteScalar();
    }

    // ✅ Notifies all users interested in the detected tags
    private void NotifyInterestedUsers(Article article, List<string> tags)
    {
        List<int> tagIds = tags.Select(tagName => _db.GetOrAddTagId(tagName)).ToList();
        if (!tagIds.Any()) return;

        List<User> interestedUsers = _db.GetUsersInterestedInTags(tagIds)
                                        .Where(u => u.ReceiveNotifications)
                                        .ToList();

        foreach (var user in interestedUsers)
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
