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
        // ============================================
        // ========== BASE CONNECTION ================
        // ============================================
        
        public DBServices() { }

        // Open SQL connection to DB
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

        // ============================================
        // ============= USERS ========================
        // ============================================

        public bool RegisterUser(UserWithTags user)
        {
            try
            {
                using (SqlConnection con = connect())
                {
                    SqlCommand cmd = new SqlCommand(@"
                INSERT INTO News_Users (Name, Email, Password)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Email, @Password)", con);

                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);

                    int userId = (int)cmd.ExecuteScalar();
                    user.Id = userId;

                    // הוספת תגיות אם יש
                    foreach (int tagId in user.Tags)
                        AddUserTag(userId, tagId);

                    return true;
                }
            }
            catch (SqlException ex)
            {
                // בדיקה האם מדובר על כפילות במייל
                if (ex.Number == 2627 || ex.Message.Contains("UQ__News_Users") || ex.Message.Contains("UQ_News_Users_Name"))
                    // violation of UNIQUE constraint
                    return false;

                throw; // כל שגיאה אחרת - נזרוק כרגיל
            }
        }

        public bool IsEmailExists(string email)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM News_Users WHERE Email = @Email", con);
                cmd.Parameters.AddWithValue("@Email", email);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool IsNameExists(string name)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM News_Users WHERE Name = @Name", con);
                cmd.Parameters.AddWithValue("@Name", name);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }


        public User LoginUser(string email, string password)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM News_Users WHERE Email = @Email AND Password = @Password", con);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    User user = new User
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"],
                        Email = (string)reader["email"],
                        Password = null, 
                        Active = Convert.ToBoolean(reader["active"]),
                        CanShare = Convert.ToBoolean(reader["CanShare"]),
                        CanComment = Convert.ToBoolean(reader["CanComment"]),
                        IsAdmin = Convert.ToBoolean(reader["isAdmin"]),
                        ProfileImagePath = reader["ProfileImagePath"] != DBNull.Value ? reader["ProfileImagePath"].ToString() : null 
                    };

                    return user;
                }

                return null;
            }
        }


        public User GetUserById(int id)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM News_Users WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new User
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString(),
                        Email = reader["email"].ToString(),
                        Password = null, // לא מחזירים סיסמה
                        Active = Convert.ToBoolean(reader["active"]),
                        CanShare = Convert.ToBoolean(reader["canShare"]),
                        CanComment = Convert.ToBoolean(reader["canComment"]),
                        IsAdmin = Convert.ToBoolean(reader["isAdmin"]),
                        ProfileImagePath = reader["ProfileImagePath"] as string,
                        AvatarLevel = reader["AvatarLevel"] as string ?? "BRONZE"
                    };
                }

                return null;
            }
        }
        public void ExecuteStoredProcedure(string spName)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(spName, con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
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
                        Active = (bool)reader["active"],
                        CanShare = reader["CanShare"] != DBNull.Value ? (bool)reader["CanShare"] : true,
                        CanComment = reader["CanComment"] != DBNull.Value ? (bool)reader["CanComment"] : true
                    });
                }
            }

            return users;
        }

        public void LogArticleFetch(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO News_ArticleFetchLog (UserId) VALUES (@UserId)", con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
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

        public List<User> GetBlockedUsers(int userId)
        {
            List<User> result = new List<User>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT u.id, u.name, u.email
            FROM News_UserBlocks b
            JOIN News_Users u ON b.blockedUserId = u.id
            WHERE b.blockerUserId = @userId
        ", con);
                cmd.Parameters.AddWithValue("@userId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new User
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"],
                        Email = (string)reader["email"]
                    });
                }
            }

            return result;
        }

        public bool UnblockUser(int blockerUserId, int blockedUserId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            DELETE FROM News_UserBlocks
            WHERE blockerUserId = @blockerUserId AND blockedUserId = @blockedUserId
        ", con);
                cmd.Parameters.AddWithValue("@blockerUserId", blockerUserId);
                cmd.Parameters.AddWithValue("@blockedUserId", blockedUserId);

                return cmd.ExecuteNonQuery() > 0;
            }
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

        public void SetUserActiveStatus(int userId, bool isActive)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "UPDATE News_Users SET Active = @Active WHERE Id = @UserId", con);
                cmd.Parameters.AddWithValue("@Active", isActive);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void LogUserLogin(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO News_Logins (UserId) VALUES (@UserId)", con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void SetUserSharingStatus(int userId, bool canShare)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "UPDATE News_Users SET CanShare = @CanShare WHERE Id = @UserId", con);
                cmd.Parameters.AddWithValue("@CanShare", canShare);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void SetUserCommentingStatus(int userId, bool canComment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
                    "UPDATE News_Users SET CanComment = @CanComment WHERE Id = @UserId", con);
                cmd.Parameters.AddWithValue("@CanComment", canComment);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }
        public async Task UpdateProfileImagePath(int userId, string path)
        {
            using (SqlConnection con = connect()) // כבר מחובר
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE News_Users SET ProfileImagePath = @path WHERE Id = @userId", con))
                {
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }






        // ============================================
        // ============= ARTICLES =====================
        // ============================================

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
            if (ArticleExists(article.SourceUrl))
                return -1; 

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

        public bool ArticleExists(string url)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM News_Articles WHERE Url = @Url", con);
                cmd.Parameters.AddWithValue("@Url", url);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
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

        public List<ArticleWithTags> GetArticlesWithTags(int page, int pageSize)
        {
            List<ArticleWithTags> articles = new List<ArticleWithTags>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesWithTags", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Page", page);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

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
                            Tags = new List<string>()
                        };

                        articles.Add(article);
                    }
                }

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


        public List<Article> GetArticlesWithMissingImages()
        {
            List<Article> list = new List<Article>();
            using (SqlConnection conn = connect()) // connect() כבר פותח את החיבור
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT id, title, description, content, author, url 
            FROM News_Articles 
            WHERE imageUrl IS NULL OR imageUrl = ''", conn);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Article
                    {
                        Id = (int)reader["id"],
                        Title = reader["title"] as string ?? "",
                        Description = reader["description"] as string ?? "",
                        Content = reader["content"] as string ?? "",
                        Author = reader["author"] as string ?? "",
                        SourceUrl = reader["url"] as string ?? "",
                        Tags = new List<string>()
                    });
                }
            }
            return list;
        }




        public void UpdateArticleImageUrl(int articleId, string imageUrl)
        {
            using (SqlConnection conn = connect()) // connect() כבר פותח את החיבור
            {
                SqlCommand cmd = new SqlCommand("UPDATE News_Articles SET imageUrl = @url WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@url", imageUrl);
                cmd.Parameters.AddWithValue("@id", articleId);
                cmd.ExecuteNonQuery();
            }
        }


        public int GetUnreadSharedCount(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM News_SharedArticles WHERE targetUserId = @id AND IsRead = 0", con);
                cmd.Parameters.AddWithValue("@id", userId);
                return (int)cmd.ExecuteScalar();
            }
        }

        public void MarkSharedAsRead(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("UPDATE News_SharedArticles SET IsRead = 1 WHERE targetUserId = @id", con);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }



        // ============================================
        // ============ COMMENTS ======================
        // ============================================

        // 🟢 הוספת תגובה
        public void AddCommentToArticle(int articleId, int userId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddCommentToArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Comment", comment);
                cmd.ExecuteNonQuery();
            }
        }

        // 🟢 שליפת תגובות
        public List<Comment> GetCommentsForArticle(int articleId)
        {
            List<Comment> list = new List<Comment>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetCommentsForArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    Comment c = new Comment
                    {
                        Id = (int)rdr["id"],
                        ArticleId = (int)rdr["articleId"],
                        UserId = (int)rdr["userId"],
                        CommentText = rdr["comment"].ToString(), 
                        CreatedAt = (DateTime)rdr["createdAt"],
                        Username = rdr["username"].ToString()
                    };
                    list.Add(c);
                }
            }

            return list;
        }

        // ============================================
        // ============== LIKES =====================
        // ============================================

        public void AddLike(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddLike", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.ExecuteNonQuery();
            }
        }

        public void RemoveLike(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RemoveLike", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.ExecuteNonQuery();
            }
        }

        public int GetLikesCount(int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetLikesCount", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                return (int)cmd.ExecuteScalar();
            }
        }

        public void AddThreadLike(int userId, int publicArticleId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_AddThreadLike", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
            cmd.ExecuteNonQuery();
        }




        public void RemoveThreadLike(int userId, int publicArticleId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_RemoveThreadLike", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId); // ✅ תואם לשם ב-SP
            cmd.ExecuteNonQuery();
        }


        public int GetThreadLikeCount(int publicArticleId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM News_ThreadLikes WHERE PublicArticleId = @id", con);
            cmd.Parameters.AddWithValue("@id", publicArticleId);
            return (int)cmd.ExecuteScalar();
        }


        public bool ToggleThreadLike(int userId, int publicArticleId)
        {
            using (var con = connect())
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // בדיקת קיום לייק קיים
                using (var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM News_ThreadLikes WHERE UserId = @UserId AND PublicArticleId = @PublicArticleId", con))
                {
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    checkCmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        // אם כבר קיים לייק – הסר אותו
                        using (var deleteCmd = new SqlCommand(
                            "DELETE FROM News_ThreadLikes WHERE UserId = @UserId AND PublicArticleId = @PublicArticleId", con))
                        {
                            deleteCmd.Parameters.AddWithValue("@UserId", userId);
                            deleteCmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
                            deleteCmd.ExecuteNonQuery();
                            return false; // הסרנו את הלייק
                        }
                    }
                    else
                    {
                        // אם אין לייק – הוסף אותו
                        using (var insertCmd = new SqlCommand(
                            "INSERT INTO News_ThreadLikes (UserId, PublicArticleId) VALUES (@UserId, @PublicArticleId)", con))
                        {
                            insertCmd.Parameters.AddWithValue("@UserId", userId);
                            insertCmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
                            insertCmd.ExecuteNonQuery();
                            return true; // הוספנו לייק
                        }
                    }
                }
            }
        }


        public bool CheckIfUserLikedThread(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(
     "SELECT COUNT(*) FROM News_ThreadLikes WHERE UserId = @userId AND PublicArticleId = @articleId", con);

                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@articleId", articleId);

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }




        public void ToggleCommentLike(int userId, int commentId)
        {
            using var con = connect();
            var check = new SqlCommand("SELECT COUNT(*) FROM News_CommentLikes WHERE UserId = @u AND CommentId = @c", con);
            check.Parameters.AddWithValue("@u", userId);
            check.Parameters.AddWithValue("@c", commentId);
            int exists = (int)check.ExecuteScalar();

            SqlCommand cmd;
            if (exists > 0)
                cmd = new SqlCommand("DELETE FROM News_CommentLikes WHERE UserId = @u AND CommentId = @c", con);
            else
                cmd = new SqlCommand("INSERT INTO News_CommentLikes (UserId, CommentId) VALUES (@u, @c)", con);

            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", commentId);
            cmd.ExecuteNonQuery();
        }

        public int GetCommentLikeCount(int commentId)
        {
            using var con = connect();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM News_CommentLikes WHERE CommentId = @id", con);
            cmd.Parameters.AddWithValue("@id", commentId);
            return (int)cmd.ExecuteScalar();
        }

        public void TogglePublicCommentLike(int userId, int publicCommentId)
        {
            using var con = connect();
            var check = new SqlCommand("SELECT COUNT(*) FROM News_PublicCommentLikes WHERE UserId = @u AND PublicCommentId = @c", con);
            check.Parameters.AddWithValue("@u", userId);
            check.Parameters.AddWithValue("@c", publicCommentId);
            int exists = (int)check.ExecuteScalar();

            SqlCommand cmd;
            if (exists > 0)
                cmd = new SqlCommand("DELETE FROM News_PublicCommentLikes WHERE UserId = @u AND PublicCommentId = @c", con);
            else
                cmd = new SqlCommand("INSERT INTO News_PublicCommentLikes (UserId, PublicCommentId) VALUES (@u, @c)", con);

            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", publicCommentId);
            cmd.ExecuteNonQuery();
        }

        public int GetPublicCommentLikeCount(int publicCommentId)
        {
            using var con = connect();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM News_PublicCommentLikes WHERE PublicCommentId = @id", con);
            cmd.Parameters.AddWithValue("@id", publicCommentId);
            return (int)cmd.ExecuteScalar();
        }


        // ============================================
        // ============== SHARING =====================
        // ============================================

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
                        SenderName = reader["senderName"].ToString(),
                        SharedId = (int)reader["id"]
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

        public void RemoveSharedArticle(int sharedId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RemoveSharedArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@sharedId", sharedId);
                cmd.ExecuteNonQuery();
            }
        }






        // ============================================
        // ======= PUBLIC ARTICLES & COMMENTS =========
        // ============================================
        public List<PublicArticle> GetAllPublicArticles(int userId)
        {
            List<PublicArticle> list = new List<PublicArticle>();

            using (SqlConnection con = connect())
            {
                using (SqlCommand cmd = new SqlCommand("NewsSP_GetAllPublicArticles", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
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
                            EnsurePublicArticleTagsExist(article.PublicArticleId);

                            // שליפת תגובות
                            article.PublicComments = GetCommentsForPublicArticle(article.PublicArticleId);

                            // שליפת תגיות מהטבלה החדשה של PublicArticleTags
                            article.Tags = GetTagsForPublicArticle(article.PublicArticleId);

                            list.Add(article);
                        }
                    }
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


        // ============================================
        // ================ TAGS ======================
        // ============================================

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
            if (!string.IsNullOrWhiteSpace(article.SourceUrl))
            {
                using (SqlConnection con = connect())
                {
                    SqlCommand checkCmd = new SqlCommand("SELECT Id FROM News_Articles WHERE Url = @Url", con);
                    checkCmd.Parameters.AddWithValue("@Url", article.SourceUrl);
                    object result = checkCmd.ExecuteScalar();
                    if (result != null)
                        return (int)result; 
                }
            }

            int newArticleId;

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
        INSERT INTO News_Articles (Title, Description, ImageUrl, Url, PublishedAt, Author)
        OUTPUT INSERTED.Id
        VALUES (@Title, @Description, @ImageUrl, @Url, @PublishedAt, @Author)", con);

                cmd.Parameters.AddWithValue("@Title", article.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", article.Description ?? "");
                cmd.Parameters.AddWithValue("@ImageUrl", article.ImageUrl ?? "");
                cmd.Parameters.AddWithValue("@Url", article.SourceUrl ?? "");
                cmd.Parameters.AddWithValue("@PublishedAt", article.PublishedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Author", article.Author ?? "");

                newArticleId = (int)cmd.ExecuteScalar();
            }

            foreach (string tagName in article.Tags)
            {
                int tagId = GetOrAddTagId(tagName);
                InsertArticleTag(newArticleId, tagId);
            }

            return newArticleId;
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

        public void InsertArticleTag(int articleId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM News_ArticleTags WHERE ArticleId = @ArticleId AND TagId = @TagId", con);
                checkCmd.Parameters.AddWithValue("@ArticleId", articleId);
                checkCmd.Parameters.AddWithValue("@TagId", tagId);

                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0) return; 

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO News_ArticleTags (ArticleId, TagId) VALUES (@ArticleId, @TagId)", con);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                cmd.ExecuteNonQuery();
            }
        }


public List<string> GetTagsForPublicArticle(int publicArticleId)
{
    List<string> tags = new List<string>();

    using (SqlConnection con = connect())
    {
        SqlCommand cmd = new SqlCommand("NewsSP_GetTagsForPublicArticle", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);

        using (SqlDataReader rdr = cmd.ExecuteReader())
        {
            while (rdr.Read())
            {
                tags.Add(rdr["Name"].ToString());
            }
        }
    }

    return tags;
}


        private void EnsurePublicArticleTagsExist(int publicArticleId)
        {
            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_CopyTagsToPublicArticle", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
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



        // ============================================
        // =============== REPORTS ====================
        // ============================================
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


        // ============================================
        // ============== STATISTICS ==================
        // ============================================
        public SiteStatistics GetSiteStatistics()
        {
            var stats = new SiteStatistics();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd;

                cmd = new SqlCommand("SELECT COUNT(*) FROM News_Users", con);
                stats.TotalUsers = (int)cmd.ExecuteScalar();

                cmd = new SqlCommand("SELECT COUNT(*) FROM News_Articles", con);
                stats.TotalArticles = (int)cmd.ExecuteScalar();

                cmd = new SqlCommand("SELECT COUNT(*) FROM News_SavedArticles", con);
                stats.TotalSaved = (int)cmd.ExecuteScalar();

                cmd = new SqlCommand("SELECT COUNT(*) FROM News_Logins WHERE CAST(LoginTime AS DATE) = CAST(GETDATE() AS DATE)", con);
                stats.TodayLogins = (int)cmd.ExecuteScalar();

                cmd = new SqlCommand("SELECT COUNT(*) FROM News_ArticleFetchLog WHERE CAST(FetchTime AS DATE) = CAST(GETDATE() AS DATE)", con);
                stats.TodayFetches = (int)cmd.ExecuteScalar();
            }

            return stats;
        }

        public object GetLikesStats()
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetLikesStats", con);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader reader = cmd.ExecuteReader();

                int articleLikes = 0;
                int articleLikesToday = 0;
                int threadLikes = 0;
                int threadLikesToday = 0;

                if (reader.Read())
                {
                    articleLikes = Convert.ToInt32(reader["ArticleLikes"]);
                    articleLikesToday = Convert.ToInt32(reader["ArticleLikesToday"]);
                    threadLikes = Convert.ToInt32(reader["ThreadLikes"]);
                    threadLikesToday = Convert.ToInt32(reader["ThreadLikesToday"]);
                }

                return new
                {
                    articleLikes,
                    articleLikesToday,
                    threadLikes,
                    threadLikesToday
                };
            }
        }


        public List<ReportedArticleDTO> GetReportedArticles()
        {
            List<ReportedArticleDTO> list = new List<ReportedArticleDTO>();
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetReportedArticles", con);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ReportedArticleDTO
                    {
                        ReporterName = reader["ReporterName"].ToString(),
                        TargetName = reader["TargetName"].ToString(),
                        ArticleTitle = reader["ArticleTitle"].ToString(),
                        Reason = reader["Reason"].ToString(),
                        ReportedAt = Convert.ToDateTime(reader["ReportedAt"])
                    });
                }
            }
            return list;
        }

        public List<ReportedCommentDTO> GetReportedComments()
        {
            List<ReportedCommentDTO> list = new List<ReportedCommentDTO>();
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetReportedComments", con);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ReportedCommentDTO
                    {
                        ReporterName = reader["ReporterName"].ToString(),
                        TargetName = reader["TargetName"].ToString(),
                        CommentText = reader["CommentText"].ToString(),
                        Reason = reader["Reason"].ToString(),
                        ReportedAt = Convert.ToDateTime(reader["ReportedAt"])
                    });
                }
            }
            return list;
        }

        public List<ReportEntry> GetAllReports()
        {
            List<ReportEntry> list = new List<ReportEntry>();
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllReports", con);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ReportEntry r = new ReportEntry
                    {
                        Id = (int)reader["id"],
                        ReporterId = (int)reader["ReporterId"],
                        ReporterName = reader["ReporterName"].ToString(),
                        ReportType = reader["referenceType"].ToString(),
                        ReferenceId = (int)reader["referenceId"],
                        Reason = reader["reason"].ToString(),
                        ReportedAt = Convert.ToDateTime(reader["reportedAt"]),
                        Content = reader["ReportedContent"].ToString(),
                        TargetName = reader["TargetName"].ToString()
                    };
                    list.Add(r);
                }
            }
            return list;
        }




        public class ReportedArticleDTO
        {
            public string ReporterName { get; set; }
            public string TargetName { get; set; }
            public string ArticleTitle { get; set; }
            public string Reason { get; set; }
            public DateTime ReportedAt { get; set; }
        }

        public class ReportedCommentDTO
        {
            public string ReporterName { get; set; }
            public string TargetName { get; set; }
            public string CommentText { get; set; }
            public string Reason { get; set; }
            public DateTime ReportedAt { get; set; }
        }
        public class UserBlockRequest
        {
            public int BlockerUserId { get; set; }
            public int BlockedUserId { get; set; }
        }

    }
}


