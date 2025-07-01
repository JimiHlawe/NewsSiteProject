using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NewsSite1.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace NewsSite1.DAL
{
    public class DBServices
    {
        // ===================== CONSTRUCTOR + CONNECTION =====================
        public DBServices() { }

        public SqlConnection connect()
        {
            string cStr;
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                cStr = configuration.GetConnectionString("myProjDB");
            }
            catch { cStr = null; }

            if (string.IsNullOrEmpty(cStr))
            {
                cStr = "Data Source=Media.ruppin.ac.il;Initial Catalog=igroup113_test2;User ID=igroup113;Password=igroup113_82421;TrustServerCertificate=True;";
            }

            SqlConnection con = new SqlConnection(cStr);
            con.Open();
            return con;
        }

        // ===================== USERS =====================

        public int RegisterUser(UserWithTags user)
        {
            int newUserId;

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO News_Users (Name, Email, Password, Active)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Email, @Password, @Active)", con);

                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);
                cmd.Parameters.AddWithValue("@Active", user.Active);

                newUserId = (int)cmd.ExecuteScalar();
            }

            // שמירת תחומי עניין
            using (SqlConnection con = connect())
            {
                foreach (int tagId in user.Tags)
                {
                    SqlCommand cmd = new SqlCommand(@"
                INSERT INTO News_UserTags (userId, tagId)
                VALUES (@UserId, @TagId)", con);

                    cmd.Parameters.AddWithValue("@UserId", newUserId);
                    cmd.Parameters.AddWithValue("@TagId", tagId);

                    cmd.ExecuteNonQuery();
                }
            }

            return newUserId;
        }


        public User LoginUser(string email, string password)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_LoginUser", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new User
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"],
                        Email = (string)reader["email"],
                        Active = (bool)reader["active"]
                    };
                }

                return null;
            }
        }

        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllUsers", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"],
                        Email = (string)reader["email"],
                        Active = (bool)reader["active"]
                    });
                }
            }

            return users;
        }

        public int? GetUserIdByUsername(string username)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT id FROM News_Users WHERE name = @Name", con);
                cmd.Parameters.AddWithValue("@Name", username);

                object result = cmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result);

                return null;
            }
        }

        // ===================== ARTICLES =====================


        public List<Article> GetAllArticles()
        {
            List<Article> articles = new List<Article>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllArticles", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Article a = new Article
                    {
                        Id = (int)reader["id"],
                        Title = reader["title"]?.ToString(),
                        Description = reader["description"]?.ToString(),
                        Content = reader["content"]?.ToString(),
                        Author = reader["author"]?.ToString(),
                        SourceUrl = reader["url"]?.ToString(),
                        ImageUrl = reader["imageUrl"]?.ToString(),
                        PublishedAt = (DateTime)reader["publishedAt"]
                    };
                    articles.Add(a);
                }
            }

            return articles;
        }

        public List<Article> FilterArticles(string title, DateTime? from, DateTime? to)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_FilterArticles", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Title", (object?)title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)to ?? DBNull.Value);

                List<Article> list = new List<Article>();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Article a = new Article
                    {
                        Id = (int)reader["id"],
                        Title = (string)reader["title"],
                        Description = (string)reader["description"],
                        Content = (string)reader["content"],
                        Author = (string)reader["author"],
                        SourceUrl = (string)reader["url"],
                        ImageUrl = (string)reader["imageUrl"],
                        PublishedAt = (DateTime)reader["publishedAt"]
                    };
                    list.Add(a);
                }

                return list;
            }
        }

        public void SaveArticle(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_SaveArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Article> GetSavedArticles(int userId)
        {
            Dictionary<int, Article> articlesDict = new Dictionary<int, Article>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetSavedArticles", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int articleId = (int)reader["id"];

                    if (!articlesDict.ContainsKey(articleId))
                    {
                        Article a = new Article
                        {
                            Id = articleId,
                            Title = reader["title"]?.ToString(),
                            Description = reader["description"]?.ToString(),
                            Content = reader["content"]?.ToString(),
                            Author = reader["author"]?.ToString(),
                            SourceUrl = reader["url"]?.ToString(),
                            ImageUrl = reader["imageUrl"]?.ToString(),
                            PublishedAt = (DateTime)reader["publishedAt"],
                            Tags = new List<string>()
                        };
                        articlesDict.Add(articleId, a);
                    }

                    string tagName = reader["TagName"]?.ToString();
                    if (!string.IsNullOrEmpty(tagName) && !articlesDict[articleId].Tags.Contains(tagName))
                    {
                        articlesDict[articleId].Tags.Add(tagName);
                    }
                }
            }

            return articlesDict.Values.ToList();
        }
        public int AddUserArticle(Article article)
        {
            int newArticleId;

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO News_Articles (Title, Description, Content, Author, url, ImageUrl, PublishedAt)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Description, @Content, @Author, @SourceUrl, @ImageUrl, @PublishedAt)
        ", con);

                cmd.Parameters.AddWithValue("@Title", article.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", article.Description ?? "");
                cmd.Parameters.AddWithValue("@Content", article.Content ?? "");
                cmd.Parameters.AddWithValue("@Author", article.Author ?? "");
                cmd.Parameters.AddWithValue("@SourceUrl", article.SourceUrl ?? "");
                cmd.Parameters.AddWithValue("@ImageUrl", article.ImageUrl ?? "");
                cmd.Parameters.AddWithValue("@PublishedAt", article.PublishedAt);

                newArticleId = (int)cmd.ExecuteScalar();
            }

            // ✅ הגנה כפולה
            if (article.Tags == null)
            {
                article.Tags = new List<string>();
            }

            foreach (string tagName in article.Tags)
            {
                int tagId = GetOrAddTagId(tagName);
                InsertArticleTag(newArticleId, tagId);
            }

            return newArticleId;
        }

        public void BlockUser(int blockerUserId, int blockedUserId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                  "INSERT INTO News_UserBlocks (BlockerUserId, BlockedUserId) VALUES (@Blocker, @Blocked)", con);
                cmd.Parameters.AddWithValue("@Blocker", blockerUserId);
                cmd.Parameters.AddWithValue("@Blocked", blockedUserId);
                cmd.ExecuteNonQuery();
            }
        }


        public void RemoveSavedArticle(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RemoveSavedArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<ArticleWithTags> GetArticlesWithTags()
        {
            List<ArticleWithTags> articles = new List<ArticleWithTags>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesWithTags", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ArticleWithTags article = new ArticleWithTags
                        {
                            Id = Convert.ToInt32(rdr["Id"]),
                            Title = rdr["Title"].ToString(),
                            Description = rdr["Description"]?.ToString(),
                            ImageUrl = rdr["ImageUrl"]?.ToString(),
                            SourceUrl = rdr["Url"]?.ToString(),
                            Author = rdr["Author"]?.ToString(),
                            PublishedAt = rdr["PublishedAt"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(rdr["PublishedAt"]),
                            Tags = new List<string>() // נטען אחרי הלולאה
                        };

                        articles.Add(article);
                    }
                }

                // טוען את התגיות לכל כתבה
                foreach (var article in articles)
                {
                    article.Tags = GetTagsForArticle(article.Id);
                }
            }

            return articles;
        }


        public List<ArticleWithTags> GetArticlesPaginated(int page, int pageSize)
        {
            List<ArticleWithTags> articles = new List<ArticleWithTags>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesPaginated", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Page", page);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    Dictionary<int, ArticleWithTags> dict = new Dictionary<int, ArticleWithTags>();

                    while (rdr.Read())
                    {
                        int id = Convert.ToInt32(rdr["Id"]);

                        if (!dict.ContainsKey(id))
                        {
                            dict[id] = new ArticleWithTags
                            {
                                Id = id,
                                Title = rdr["Title"].ToString(),
                                Description = rdr["Description"]?.ToString(),
                                ImageUrl = rdr["ImageUrl"]?.ToString(),
                                SourceUrl = rdr["Url"]?.ToString(),
                                Author = rdr["Author"]?.ToString(),
                                PublishedAt = rdr["PublishedAt"] == DBNull.Value
                                    ? null
                                    : (DateTime?)Convert.ToDateTime(rdr["PublishedAt"]),
                                Tags = new List<string>()
                            };
                        }

                        if (rdr["TagName"] != DBNull.Value)
                        {
                            dict[id].Tags.Add(rdr["TagName"].ToString());
                        }
                    }

                    articles = dict.Values.ToList();
                }
            }

            return articles;
        }

        public void ReportContent(int userId, string referenceType, int referenceId, string reason)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO News_Reports (userId, referenceType, referenceId, reason, reportedAt)
            VALUES (@UserId, @ReferenceType, @ReferenceId, @Reason, GETDATE())", con);

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                cmd.Parameters.AddWithValue("@Reason", reason ?? "");

                cmd.ExecuteNonQuery();
            }
        }


        // ===================== SHARING =====================
        public void ShareArticleByUsernames(string senderUsername, string targetUsername, int articleId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ShareArticleByUsernames", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SenderUsername", senderUsername);
                cmd.Parameters.AddWithValue("@TargetUsername", targetUsername);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment ?? "");

                cmd.ExecuteNonQuery();
            }
        }

        public List<SharedArticle> GetArticlesSharedWithUser(int userId)
        {
            List<SharedArticle> sharedArticles = new List<SharedArticle>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesSharedWithUser", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@userId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    SharedArticle article = new SharedArticle
                    {
                        Id = (int)reader["articleId"],
                        Title = reader["title"].ToString(),
                        Description = reader["description"].ToString(),
                        Content = reader["content"].ToString(),
                        Author = reader["author"].ToString(),
                        SourceUrl = reader["sourceUrl"].ToString(),
                        ImageUrl = reader["imageUrl"].ToString(),
                        PublishedAt = (DateTime)reader["publishedAt"],
                        Comment = reader["comment"].ToString(),
                        SharedAt = (DateTime)reader["sharedAt"],
                        SenderName = reader["senderName"].ToString()
                    };

                    sharedArticles.Add(article);
                }
            }

            return sharedArticles;
        }

        public void ShareArticlePublic(int userId, int articleId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ShareArticlePublic", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);        // ← שים לב לשם המדויק!
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment ?? ""); // ← null-safe
                cmd.ExecuteNonQuery();
            }
        }


        public List<PublicArticle> GetAllPublicArticles(int userId)
        {
            List<PublicArticle> list = new List<PublicArticle>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllPublicArticles", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var article = new PublicArticle
                    {
                        PublicArticleId = (int)reader["publicArticleId"],
                        ArticleId = (int)reader["id"],
                        Title = reader["title"].ToString(),
                        Description = reader["description"].ToString(),
                        Content = reader["content"].ToString(),
                        Author = reader["author"].ToString(),
                        SourceUrl = reader["sourceUrl"].ToString(),
                        ImageUrl = reader["imageUrl"].ToString(),
                        PublishedAt = (DateTime)reader["publishedAt"],
                        SenderName = reader["senderName"].ToString(),
                        InitialComment = reader["initialComment"].ToString(),
                        SharedAt = (DateTime)reader["sharedAt"]
                    };

                    // שליפת תגובות לכתבה
                    article.PublicComments = GetCommentsForPublicArticle(article.PublicArticleId);

                    list.Add(article);
                }
            }

            // סינון לפי רשימת חסומים
            var blocked = GetBlockedUserIds(userId);
            return list.Where(a => !blocked.Contains(GetUserIdByUsername(a.SenderName) ?? -1)).ToList();
        }


        public List<int> GetBlockedUserIds(int userId)
        {
            var list = new List<int>();
            using (var con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT BlockedUserId FROM News_UserBlocks WHERE BlockerUserId = @UserId", con);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add((int)reader["BlockedUserId"]);
            }
            return list;
        }


        // ===================== PUBLIC COMMENTS =====================

        public void AddPublicComment(int publicArticleId, int userId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddCommentToPublicArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Comment", comment);
                cmd.ExecuteNonQuery();
            }
        }

        public List<PublicComment> GetCommentsForPublicArticle(int articleId)
        {
            List<PublicComment> comments = new List<PublicComment>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
    SELECT c.*, u.name AS username
    FROM News_PublicComments c
    JOIN News_Users u ON c.userId = u.id
    WHERE c.publicArticleId = @ArticleId
    ORDER BY c.createdAt ASC", con);


                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    PublicComment c = new PublicComment
                    {
                        Id = (int)reader["id"],
                        PublicArticleId = (int)reader["publicArticleId"],
                        UserId = (int)reader["userId"],
                        Comment = reader["comment"].ToString(),
                        CreatedAt = (DateTime)reader["createdAt"],
                        Username = reader["username"].ToString()
                    };
                    comments.Add(c);
                }
            }

            return comments;
        }

        // ===================== TAGS =====================

        public void AddUserTag(int userId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddUserTag", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Tag> GetUserTags(int userId)
        {
            List<Tag> tags = new List<Tag>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetUserTags", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tags.Add(new Tag
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"]
                    });
                }
            }

            return tags;
        }
        public int AddArticleWithTags(ArticleWithTags article)
        {
            int newArticleId;

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO News_Articles (Title, Description, ImageUrl, Url, PublishedAt, Author)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Description, @ImageUrl, @Url, @PublishedAt, @Author)
        ", con);

                cmd.Parameters.AddWithValue("@Title", article.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", article.Description ?? "");
                cmd.Parameters.AddWithValue("@ImageUrl", article.ImageUrl ?? "");
                cmd.Parameters.AddWithValue("@Url", article.SourceUrl ?? "");
                cmd.Parameters.AddWithValue("@PublishedAt", article.PublishedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Author", article.Author ?? "");

                newArticleId = (int)cmd.ExecuteScalar();
            }

            // אחרי הוספת הכתבה: טיפול בתגיות
            foreach (string tagName in article.Tags)
            {
                int tagId = GetOrAddTagId(tagName);
                InsertArticleTag(newArticleId, tagId);
            }

            return newArticleId;
        }

public List<Article> GetArticlesFilteredByTags(int userId)
{
    Dictionary<int, Article> articles = new Dictionary<int, Article>();

    using (SqlConnection con = connect())
    {
                SqlCommand cmd = new SqlCommand(@"
(
    -- 1️⃣ כתבות עם תיוגים רלוונטיים
    SELECT A.*, T.name AS TagName, 1 AS Priority
    FROM News_Articles A
    JOIN News_ArticleTags AT ON A.id = AT.articleId
    JOIN News_Tags T ON AT.tagId = T.id
    JOIN News_UserTags UT ON UT.tagId = AT.tagId
    WHERE UT.userId = @UserId

    UNION ALL

    -- 2️⃣ כתבות ללא תיוג בכלל
    SELECT A.*, NULL AS TagName, 2 AS Priority
    FROM News_Articles A
    WHERE NOT EXISTS (
        SELECT 1
        FROM News_ArticleTags AT
        WHERE AT.articleId = A.id
    )

    UNION ALL

    -- 3️⃣ כתבות אחרות עם תיוגים לא שלך
    SELECT A.*, T.name AS TagName, 3 AS Priority
    FROM News_Articles A
    JOIN News_ArticleTags AT ON A.id = AT.articleId
    JOIN News_Tags T ON AT.tagId = T.id
    WHERE NOT EXISTS (
        SELECT 1
        FROM News_UserTags UT
        WHERE UT.tagId = AT.tagId AND UT.userId = @UserId
    )
)
ORDER BY Priority, publishedAt DESC

", con);


                cmd.Parameters.AddWithValue("@UserId", userId);

        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int id = (int)reader["id"];
            if (!articles.ContainsKey(id))
            {
                Article article = new Article
                {
                    Id = id,
                    Title = reader["title"] as string ?? "",
                    Description = reader["description"] as string ?? "",
                    Content = reader["content"] as string ?? "",
                    Author = reader["author"] as string ?? "",
                    SourceUrl = reader["url"] as string ?? "",
                    ImageUrl = reader["imageUrl"] as string ?? "",
                    PublishedAt = reader["publishedAt"] == DBNull.Value
                        ? DateTime.MinValue
                        : Convert.ToDateTime(reader["publishedAt"]),
                    Tags = new List<string>()
                };

                articles.Add(id, article);
            }

            // הוספת Tag רק אם באמת קיים
            string tagName = reader["TagName"] as string;
            if (!string.IsNullOrEmpty(tagName) && !articles[id].Tags.Contains(tagName))
            {
                articles[id].Tags.Add(tagName);
            }
        }
    }

    return articles.Values.ToList();
}



        public void RemoveUserTag(int userId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "DELETE FROM News_UserTags WHERE userId = @UserId AND tagId = @TagId", con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TagId", tagId);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePassword(int userId, string newPassword)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "UPDATE News_Users SET Password = @Password WHERE Id = @UserId", con);
                cmd.Parameters.AddWithValue("@Password", newPassword);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }



        public int GetOrAddTagId(string tagName)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT Id FROM News_Tags WHERE Name = @Name", con);
                cmd.Parameters.AddWithValue("@Name", tagName);

                object result = cmd.ExecuteScalar();
                if (result != null)
                    return (int)result;

                cmd = new SqlCommand(
                    "INSERT INTO News_Tags (Name) OUTPUT INSERTED.Id VALUES (@Name)", con);
                cmd.Parameters.AddWithValue("@Name", tagName);

                return (int)cmd.ExecuteScalar();
            }
        }



        public void InsertArticleTag(int articleId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO News_ArticleTags (ArticleId, TagId) VALUES (@ArticleId, @TagId)", con);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                cmd.ExecuteNonQuery();
            }
        }


        public List<string> GetTagsForArticle(int articleId)
        {
            List<string> tags = new List<string>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT T.name
            FROM News_ArticleTags AT
            JOIN News_Tags T ON AT.tagId = T.id
            WHERE AT.articleId = @ArticleId", con);

                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tags.Add(reader["name"].ToString());
                }
            }

            return tags;
        }

        public List<Tag> GetAllTags()
        {
            List<Tag> tags = new List<Tag>();
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllTags", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tags.Add(new Tag
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"]
                    });
                }
            }
            return tags;
        }

    }
}
