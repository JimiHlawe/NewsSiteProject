using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NewsSite1.Models;
using NewsSite1.Services;

public class TaggingRunner
{
    private readonly OpenAiTagService _tagService;
    private readonly string _connectionString;

    public TaggingRunner()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        _tagService = new OpenAiTagService(config);
        _connectionString = "Data Source=Media.ruppin.ac.il;Initial Catalog=igroup113_test2;User ID=igroup113;Password=igroup113_82421;TrustServerCertificate=True;";
    }

    public async Task RunAsync()
    {
        var articles = GetArticlesFromDb();

        foreach (var article in articles)
        {
            try
            {
                var tags = await _tagService.DetectTagsAsync(article.Title, article.Content);
                SaveTagsToDb(article.Id, tags);
                Console.WriteLine($"✓ Tagged article ID: {article.Id}");

                await Task.Delay(5000); // ❗ השהיה של 5 שניות בין בקשות
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error tagging article ID {article.Id}: {ex.Message}");
            }
        }

        Console.WriteLine("✓ All existing articles tagged.");
    }

    private List<Article> GetArticlesFromDb()
    {
        var list = new List<Article>();

        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        SELECT A.id, A.title, A.content
        FROM News_Articles A
        WHERE NOT EXISTS (
            SELECT 1
            FROM News_ArticleTags T
            WHERE T.articleId = A.id
        )", con);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Article
            {
                Id = (int)reader["id"],
                Title = reader["title"].ToString(),
                Content = reader["content"].ToString()
            });
        }

        return list;
    }


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

    private void SaveTagsToDb(int articleId, List<string> tags)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        foreach (var tag in tags)
        {
            int tagId = GetOrCreateTagId(tag, con);

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM News_ArticleTags WHERE articleId = @articleId AND tagId = @tagId", con);
            checkCmd.Parameters.AddWithValue("@articleId", articleId);
            checkCmd.Parameters.AddWithValue("@tagId", tagId);
            int count = (int)checkCmd.ExecuteScalar();

            if (count == 0)
            {
                var cmd = new SqlCommand("INSERT INTO News_ArticleTags (articleId, tagId) VALUES (@articleId, @tagId)", con);
                cmd.Parameters.AddWithValue("@articleId", articleId);
                cmd.Parameters.AddWithValue("@tagId", tagId);
                cmd.ExecuteNonQuery();
            }
        }
    }

}
