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
        public DBServices() { }

        // --- התחברות למסד נתונים ---
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

        public bool RegisterUser(User user)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RegisterUser", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);

                try
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException)
                {
                    return false;
                }
            }
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

        // ===================== ARTICLES =====================

        public void AddArticle(Article article)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Title", article.Title);
                cmd.Parameters.AddWithValue("@Description", article.Description);
                cmd.Parameters.AddWithValue("@Content", article.Content);
                cmd.Parameters.AddWithValue("@Author", article.Author);
                cmd.Parameters.AddWithValue("@SourceName", article.SourceName);
                cmd.Parameters.AddWithValue("@SourceUrl", article.SourceUrl);
                cmd.Parameters.AddWithValue("@ImageUrl", article.ImageUrl);
                cmd.Parameters.AddWithValue("@PublishedAt", article.PublishedAt);

                cmd.ExecuteNonQuery();
            }
        }

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
                        SourceName = reader["sourceName"]?.ToString(),
                        SourceUrl = reader["url"]?.ToString(),
                        ImageUrl = reader["imageUrl"]?.ToString(),
                        PublishedAt = (DateTime)reader["publishedAt"]
                    };
                    articles.Add(a);
                }
            }

            return articles;
        }

        public List<Article> FilterArticles(string sourceName, string title, DateTime? from, DateTime? to)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_FilterArticles", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SourceName", (object?)sourceName ?? DBNull.Value);
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
                        SourceName = (string)reader["sourceName"],
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
            List<Article> articles = new List<Article>();

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
                    Article a = new Article
                    {
                        Id = (int)reader["id"],
                        Title = reader["title"]?.ToString(),
                        Description = reader["description"]?.ToString(),
                        Content = reader["content"]?.ToString(),
                        Author = reader["author"]?.ToString(),
                        SourceName = reader["sourceName"]?.ToString(),
                        SourceUrl = reader["url"]?.ToString(),
                        ImageUrl = reader["imageUrl"]?.ToString(),
                        PublishedAt = (DateTime)reader["publishedAt"]
                    };
                    articles.Add(a);
                }
            }

            return articles;
        }


        public void ShareArticleByUsernames(string senderUsername, string targetUsername, int articleId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ShareArticleByUsernames", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@SenderUsername", senderUsername);
                cmd.Parameters.AddWithValue("@TargetUsername", targetUsername);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment ?? "");

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

        public List<SharedArticle> GetArticlesSharedWithUser(int userId)
        {
            List<SharedArticle> sharedArticles = new List<SharedArticle>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesSharedWithUser", con);
                cmd.CommandType = CommandType.StoredProcedure;
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
                SqlCommand cmd = new SqlCommand("NewsSP_ShareArticlePublic", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);        // ← שים לב לשם המדויק!
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment ?? ""); // ← null-safe
                cmd.ExecuteNonQuery();
            }
        }



        public List<PublicArticle> GetAllPublicArticles()
        {
            List<PublicArticle> list = new List<PublicArticle>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllPublicArticles", con);
                cmd.CommandType = CommandType.StoredProcedure;

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

                    // כאן מתווספת שליפת התגובות לכל כתבה
                    article.PublicComments = GetCommentsForPublicArticle(article.PublicArticleId);

                    list.Add(article);
                }
            }

            return list;
        }



        public void AddPublicComment(int publicArticleId, int userId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddCommentToPublicArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
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

        public void RemoveSavedArticle(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RemoveSavedArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.ExecuteNonQuery();
            }
        }


        // ===================== TAGS =====================

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

        public List<ArticleWithTags> GetArticlesWithTags()
        {
            List<ArticleWithTags> list = new List<ArticleWithTags>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT A.id, A.title, A.content, T.name AS tag
            FROM News_Articles A
            LEFT JOIN News_ArticleTags AT ON A.id = AT.articleId
            LEFT JOIN News_Tags T ON AT.tagId = T.id", con);

                SqlDataReader reader = cmd.ExecuteReader();

                Dictionary<int, ArticleWithTags> dict = new Dictionary<int, ArticleWithTags>();

                while (reader.Read())
                {
                    int id = (int)reader["id"];

                    if (!dict.ContainsKey(id))
                    {
                        dict[id] = new ArticleWithTags
                        {
                            Id = id,
                            Title = reader["title"].ToString(),
                            Content = reader["content"].ToString(),
                            Tags = new List<string>()
                        };
                    }

                    if (reader["tag"] != DBNull.Value)
                        dict[id].Tags.Add(reader["tag"].ToString());
                }

                list = dict.Values.ToList();
            }

            return list;
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


    }
}
