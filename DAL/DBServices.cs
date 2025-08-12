using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NewsSite.Services;
using NewsSite1.Models;
using NewsSite1.Services;
using System;
using System.Collections.Generic;
using System.Data;
using NewsSite1.Models.DTOs;
using NewsSite1.Models.DTOs.Requests;


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
                    SqlCommand cmd = new SqlCommand("NewsSP_RegisterUser", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        int userId = (int)result;
                        user.Id = userId;

                        // הוספת תגיות אם יש
                        foreach (int tagId in user.Tags)
                            AddUserTag(userId, tagId);

                        return true;
                    }

                    return false;
                }
            }
            catch (SqlException ex)
            {
                // תפיסת שגיאת כפילות לפי קונסטריינט ייחודיות
                if (ex.Number == 2627 || ex.Message.Contains("UQ__News_Users") || ex.Message.Contains("UQ_News_Users_Name"))
                    return false;

                throw; // שגיאה לא צפויה – נזרוק החוצה
            }
        }

        public bool IsEmailExists(string email)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_IsEmailExists", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Email", email);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool IsNameExists(string name)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_IsNameExists", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Name", name);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public User LoginUser(string email, string password)
        {
            using (SqlConnection con = connect())
            {
                // שימוש ב־Stored Procedure במקום שאילתה ישירה
                SqlCommand cmd = new SqlCommand("NewsSP_LoginUser", con);
                cmd.CommandType = CommandType.StoredProcedure;

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
                        Password = null, // הסיסמה לא מוחזרת מטעמי אבטחה
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
                SqlCommand cmd = new SqlCommand("NewsSP_GetUserById", con);
                cmd.CommandType = CommandType.StoredProcedure;
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
                        ReceiveNotifications = Convert.ToBoolean(reader["ReceiveNotifications"]),
                        AvatarLevel = reader["AvatarLevel"] as string ?? "BRONZE"
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
                SqlCommand cmd = new SqlCommand("NewsSP_LogArticleFetch", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public int? GetUserIdByUsername(string username)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetUserIdByUsername", con);
                cmd.CommandType = CommandType.StoredProcedure;
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
                SqlCommand cmd = new SqlCommand("NewsSP_BlockUser", con);
                cmd.CommandType = CommandType.StoredProcedure;
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
                SqlCommand cmd = new SqlCommand("NewsSP_GetBlockedUsers", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

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
                SqlCommand cmd = new SqlCommand("NewsSP_UnblockUser", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@BlockerUserId", blockerUserId);
                cmd.Parameters.AddWithValue("@BlockedUserId", blockedUserId);

                cmd.ExecuteNonQuery(); 
                return true;
            }
        }


        public void RemoveUserTag(int userId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_RemoveUserTag", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePassword(int userId, string newPassword)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_UpdatePassword", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Password", newPassword);
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }

        public void LogUserLogin(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_LogUserLogin", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void SetUserActiveStatus(int userId, bool isActive)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_SetUserActiveStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Active", isActive);
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }

        public void SetUserSharingStatus(int userId, bool canShare)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_SetUserSharingStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CanShare", canShare);
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }

        public void SetUserCommentingStatus(int userId, bool canComment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_SetUserCommentingStatus", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CanComment", canComment);
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }

        public async Task UpdateProfileImagePath(int userId, string path)
        {
            using (SqlConnection con = connect())
            {
                using (SqlCommand cmd = new SqlCommand("NewsSP_UpdateProfileImagePath", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Path", path);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public bool GetUserCanComment(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetUserCanComment", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", userId);
                return (bool)cmd.ExecuteScalar();
            }
        }

        public bool GetUserCanShare(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetUserCanShare", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", userId);
                return (bool)cmd.ExecuteScalar();
            }
        }

        public bool ToggleUserNotifications(int userId, bool enable)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ToggleUserNotifications", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Val", enable);
                cmd.Parameters.AddWithValue("@Id", userId);

                cmd.ExecuteNonQuery(); 
                return true; 
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
                SqlCommand cmd = new SqlCommand("NewsSP_AddArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

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

            List<int> tagIds = new List<int>();

            foreach (string tagName in article.Tags)
            {
                int tagId = GetOrAddTagId(tagName);
                tagIds.Add(tagId);
                InsertArticleTag(newArticleId, tagId);
            }

            // ✅ Send email to interested users
            if (tagIds.Any())
            {
                List<User> interestedUsers = GetUsersInterestedInTags(tagIds);
                EmailService mailer = new EmailService(); 

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
                        mailer.Send(user.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to send to {user.Email}: {ex.Message}");
                    }
                }
            }

            return newArticleId;
        }


        public bool ArticleExists(string url)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ArticleExists", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Url", url);

                int result = (int)cmd.ExecuteScalar();
                return result == 1;
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

        public List<ArticleWithTags> GetSidebarArticles(int page, int pageSize)
        {
            var articles = new Dictionary<int, ArticleWithTags>();

            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_GetSidebarArticles", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Page", page);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int id = Convert.ToInt32(rdr["Id"]);

                        if (!articles.TryGetValue(id, out var article))
                        {
                            article = new ArticleWithTags
                            {
                                Id = id,
                                Title = rdr["Title"].ToString(),
                                Description = rdr["Description"]?.ToString(),
                                ImageUrl = rdr["ImageUrl"]?.ToString(),
                                SourceUrl = rdr["Url"]?.ToString(),
                                Author = rdr["Author"]?.ToString(),
                                PublishedAt = rdr["PublishedAt"] == DBNull.Value ? null : (DateTime?)rdr["PublishedAt"],
                                Tags = new List<string>()
                            };
                            articles[id] = article;
                        }

                        if (rdr["TagName"] != DBNull.Value)
                        {
                            article.Tags.Add(rdr["TagName"].ToString());
                        }
                    }
                }
            }

            return articles.Values.ToList();
        }



        // ✅ Returns all articles filtered and prioritized by user's tag preferences
        // Priority 1: Articles matching user's tags
        // Priority 2: Articles with no tags
        // Priority 3: Articles with unrelated tags
        public List<Article> GetArticlesFilteredByTags(int userId)
        {
            Dictionary<int, Article> articles = new Dictionary<int, Article>();

            using (SqlConnection con = connect())
            {
                // Call stored procedure instead of raw SQL query
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesFilteredByTags", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = (int)reader["id"];

                    // Create article only once and add to dictionary
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

                    // ✅ Add tag name to article only if exists and not already added
                    string tagName = reader["TagName"] as string;
                    if (!string.IsNullOrEmpty(tagName) && !articles[id].Tags.Contains(tagName))
                    {
                        articles[id].Tags.Add(tagName);
                    }
                }
            }

            return articles.Values.ToList();
        }



        // ✅ Returns a list of articles that are missing an image (imageUrl is NULL or empty)
        public List<Article> GetArticlesWithMissingImages()
        {
            List<Article> list = new List<Article>();

            using (SqlConnection conn = connect())
            {
                // Call stored procedure to fetch articles without image
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesWithMissingImages", conn);
                cmd.CommandType = CommandType.StoredProcedure;

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


        // ✅ Updates the image URL for a specific article by its ID
        public void UpdateArticleImageUrl(int articleId, string imageUrl)
        {
            using (SqlConnection conn = connect()) // connection is already opened
            {
                // Call stored procedure to update image URL
                SqlCommand cmd = new SqlCommand("NewsSP_UpdateArticleImageUrl", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@url", imageUrl);
                cmd.Parameters.AddWithValue("@id", articleId);

                cmd.ExecuteNonQuery();
            }
        }


        // ✅ Returns the number of unread shared articles for a specific user
        public int GetUnreadSharedArticlesCount(int userId)
        {
            using (SqlConnection con = connect())
            {
                // Call stored procedure to get unread shared article count
                SqlCommand cmd = new SqlCommand("NewsSP_GetUnreadSharedArticlesCount", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                return (int)cmd.ExecuteScalar();
            }
        }


        // ✅ Marks all shared articles as read for a specific user
        public void MarkSharedAsRead(int userId)
        {
            using (SqlConnection con = connect())
            {
                // Call stored procedure to mark all shared articles as read
                SqlCommand cmd = new SqlCommand("NewsSP_MarkSharedAsRead", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }


        // ✅ Retrieves a single article by its ID
        public Article GetArticleById(int articleId)
        {
            using (SqlConnection con = connect())
            {
                // Call stored procedure to get article by ID
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticleById", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", articleId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Article
                        {
                            Id = (int)reader["Id"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            Content = reader["Content"].ToString(),
                            Author = reader["Author"].ToString(),
                            SourceUrl = reader["url"].ToString(),        // 🔄 SourceUrl maps to 'url' column
                            ImageUrl = reader["imageUrl"].ToString(),
                            PublishedAt = Convert.ToDateTime(reader["PublishedAt"]),
                            Tags = new List<string>() // Tags can be loaded separately if needed
                        };
                    }
                }
            }

            return null;
        }



        // ============================================
        // ============ COMMENTS ======================
        // ============================================

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

        public void AddCommentToThreads(int publicArticleId, int userId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddCommentToThreads", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Comment", comment);
                cmd.ExecuteNonQuery();
            }
        }


        // ✅ Retrieves all public comments for a given public article, ordered by creation time
        public List<PublicComment> LoadThreadsComments(int articleId)
        {
            List<PublicComment> comments = new List<PublicComment>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_LoadThreadsComments", con); 
                cmd.CommandType = CommandType.StoredProcedure;
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

        // ✅ Returns the number of likes for a public article (thread) and updates Firebase in real-time
        public int GetThreadLikeCount(int publicArticleId)
        {
            using var con = connect();

            // Call stored procedure to get like count for the given public article ID
            using var cmd = new SqlCommand("NewsSP_GetThreadLikeCount", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);

            int count = (int)cmd.ExecuteScalar();

            // ✅ Push the like count to Firebase for real-time updates
            var firebase = new FirebaseRealtimeService();
            firebase.UpdateLikeCount(publicArticleId, count).Wait(); // or .GetAwaiter().GetResult()

            return count;
        }


        // ✅ Returns number of likes for a shared article and updates Firebase
        public int GetLikesCount(int articleId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetLikesCount", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ArticleId", articleId);
            int count = (int)cmd.ExecuteScalar();

            // ✅ Update Firebase
            var firebase = new FirebaseRealtimeService();
            firebase.UpdateLikeCount(articleId, count).Wait(); // או GetAwaiter().GetResult()

            return count;
        }

        // ✅ Toggles a like on a public article (thread) for the given user.
        // Returns true if the like was added, false if it was removed.
        public bool ToggleThreadLike(int userId, int publicArticleId)
        {
            using (var con = connect())
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // Call stored procedure to toggle the like
                using (var cmd = new SqlCommand("NewsSP_ToggleThreadLike", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);

                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) == 1; // 1 = like added, 0 = like removed
                }
            }
        }

        // ✅ Toggles a like on a regular article for the given user.
        // Returns true if like was added, false if removed.
        public bool ToggleArticleLike(int userId, int articleId)
        {
            using (var con = connect())
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                using (var cmd = new SqlCommand("NewsSP_ToggleArticleLike", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ArticleId", articleId);

                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }



        // ✅ Checks if a specific user has liked a specific public article (thread)
        public bool CheckIfUserLikedThread(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                // Call stored procedure to check for like existence
                SqlCommand cmd = new SqlCommand("NewsSP_CheckIfUserLikedThread", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                int count = (int)cmd.ExecuteScalar();
                return count > 0; // return true if like exists
            }
        }


        // ✅ Toggles a like on a comment for the given user and comment ID
        public void ToggleCommentLike(int userId, int commentId)
        {
            using var con = connect();

            // Call stored procedure to toggle the like on comment
            SqlCommand cmd = new SqlCommand("NewsSP_ToggleCommentLike", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CommentId", commentId);

            cmd.ExecuteNonQuery(); // No return needed, action performed inside SP
        }

        // ✅ Returns the number of likes on a specific comment
        public int GetArticleCommentLikeCount(int commentId)
        {
            using var con = connect();

            var cmd = new SqlCommand("NewsSP_ArticleCommentLikeCount", con); // ← SP החדש
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CommentId", commentId);

            return (int)cmd.ExecuteScalar();
        }



        // ✅ Toggles like/unlike on a public comment by the user
        public void TogglePublicCommentLike(int userId, int publicCommentId)
        {
            using var con = connect();

            var cmd = new SqlCommand("NewsSP_TogglePublicCommentLike", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PublicCommentId", publicCommentId);

            cmd.ExecuteNonQuery();
        }


        // ✅ Returns the number of likes for a given public comment
        public int GetPublicCommentLikeCount(int publicCommentId)
        {
            using var con = connect();

            var cmd = new SqlCommand("NewsSP_GetPublicCommentLikeCount", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PublicCommentId", publicCommentId);

            return (int)cmd.ExecuteScalar();
        }


        // ============================================
        // ============== SHARING =====================
        // ============================================

        public void ShareArticleByUsernames(string senderUsername, string receiverUsername, int articleId, string comment)
        {
            int? senderId = GetUserIdByUsername(senderUsername);
            int? receiverId = GetUserIdByUsername(receiverUsername);

            if (senderId == null || receiverId == null)
                throw new Exception("User not found");

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ShareArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", senderId.Value);
                cmd.Parameters.AddWithValue("@TargetUserId", receiverId.Value);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment);

                cmd.ExecuteNonQuery();
            }
        }

        public void ShareToThreads(int userId, int articleId, string comment)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_ShareToThreads", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Comment", comment ?? "");
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


        // ✅ Returns the tag names for a given shared article
        public List<string> GetTagsForSharedArticle(int sharedArticleId)
        {
            List<string> tags = new List<string>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetTagsForSharedArticle", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@SharedId", sharedArticleId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tags.Add(reader["name"].ToString());
                    }
                }
            }

            return tags;
        }


        // ✅ Returns all Inbox articles sent to a specific user, including tags
        public List<SharedArticle> GetInboxArticles(int userId)
        {
            List<SharedArticle> inboxArticles = new List<SharedArticle>();

            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_GetInboxArticles", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int sharedId = (int)reader["SharedId"];
                        int articleId = (int)reader["articleId"];
                        string comment = reader["comment"].ToString();
                        DateTime sharedAt = Convert.ToDateTime(reader["sharedAt"]);
                        string senderName = reader["SenderName"].ToString();

                        Article baseArticle = GetArticleById(articleId);
                        if (baseArticle == null) continue;

                        SharedArticle article = new SharedArticle(
                            baseArticle.Id,
                            baseArticle.Title,
                            baseArticle.Description,
                            baseArticle.Content,
                            baseArticle.Author,
                            baseArticle.SourceUrl,
                            baseArticle.ImageUrl,
                            baseArticle.PublishedAt,
                            new List<string>(), // Will populate below
                            comment,
                            sharedAt,
                            senderName,
                            sharedId
                        );

                        article.Tags = GetTagsForSharedArticle(sharedId);
                        inboxArticles.Add(article);
                    }
                }
            }

            return inboxArticles;
        }





        // ============================================
        // ======= PUBLIC ARTICLES & COMMENTS =========
        // ============================================
        public List<PublicArticle> GetAllThreads(int userId)
        {
            List<PublicArticle> list = new List<PublicArticle>();

            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_GetAllThreads", con))
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
                            SharedAt = (DateTime)reader["sharedAt"],
                            Tags = GetTagsForPublicArticle((int)reader["publicArticleId"]),
                            PublicComments = LoadThreadsComments((int)reader["publicArticleId"])
                        };

                        EnsurePublicArticleTagsExist(article.PublicArticleId);

                        list.Add(article);
                    }
                }
            }

            return list;
        }


        // ✅ Returns a list of user IDs that the given user has blocked
        public List<int> GetBlockedUserIds(int userId)
        {
            var list = new List<int>();

            using (var con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetBlockedUserIds", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add((int)reader["BlockedUserId"]);
                }
            }

            return list;
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

        // ✅ Returns the tag ID for the given name, or inserts it if it doesn't exist
        public int GetOrAddTagId(string tagName)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetOrAddTagId", con);
                cmd.CommandType = CommandType.StoredProcedure;
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

        // ✅ Inserts a tag-article link only if it doesn't already exist
        public void InsertArticleTag(int articleId, int tagId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_InsertArticleTagIfNotExists", con);
                cmd.CommandType = CommandType.StoredProcedure;
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


        public List<User> GetUsersInterestedInTags(List<int> tagIds)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetUsersInterestedInTags", con)
            { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("@TagIdsCsv", string.Join(",", tagIds));

            var users = new List<User>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                users.Add(new User
                {
                    Id = rd.GetInt32(rd.GetOrdinal("Id")),
                    Name = rd["Name"]?.ToString() ?? "",
                    Email = rd["Email"]?.ToString() ?? "",
                    ReceiveNotifications = rd.GetBoolean(rd.GetOrdinal("ReceiveNotifications"))
                });
            }
            return users;
        }




        // ============================================
        // =============== REPORTS ====================
        // ============================================
        // ✅ Reports an article or comment for abuse, spam, etc.
        public void ReportContent(int userId, string referenceType, int referenceId, string reason)
        {
            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_ReportContent", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                cmd.Parameters.AddWithValue("@Reason", reason ?? "");

                cmd.ExecuteNonQuery();
            }
        }

        public List<ReportEntryDTO> GetAllReports()
        {
            List<ReportEntryDTO> list = new List<ReportEntryDTO>();
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetAllReports", con);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ReportEntryDTO r = new ReportEntryDTO
                    {
                        Id = (int)reader["id"],
                        ReporterId = (int)reader["ReporterId"],
                        ReporterName = reader["ReporterName"].ToString(),
                        ReportType = reader["referenceType"].ToString(),
                        ReferenceId = (int)reader["referenceId"],
                        Reason = reader["reason"].ToString(),
                        ReportedAt = Convert.ToDateTime(reader["reportedAt"]),
                        Content = reader["ReportedContent"] != DBNull.Value ? reader["ReportedContent"].ToString() : null,
                        TargetName = reader["TargetName"] != DBNull.Value ? reader["TargetName"].ToString() : null
                    };
                    list.Add(r);
                }
            }
            return list;
        }

        public int GetTodayArticleReportsCount()
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetTodayArticleReportsCount", con);
                cmd.CommandType = CommandType.StoredProcedure;
                return (int)cmd.ExecuteScalar(); 
            }
        }


        // ============================================
        // ============== STATISTICS ==================
        // ============================================
        // ✅ Returns total users, articles, saved articles, today's logins & fetches
        public SiteStatistics GetSiteStatistics()
        {
            var stats = new SiteStatistics();

            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_GetSiteStatistics", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats.TotalUsers = reader.GetInt32(reader.GetOrdinal("TotalUsers"));
                        stats.TotalArticles = reader.GetInt32(reader.GetOrdinal("TotalArticles"));
                        stats.TotalSaved = reader.GetInt32(reader.GetOrdinal("TotalSaved"));
                        stats.TodayLogins = reader.GetInt32(reader.GetOrdinal("TodayLogins"));
                        stats.TodayFetches = reader.GetInt32(reader.GetOrdinal("TodayFetches"));
                    }
                }
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






        // ============================================
        // ============== TAGGING ==================
        // ============================================
        // 1) כתבות בלי תגיות
        public List<Article> GetUntaggedArticles()
        {
            var list = new List<Article>();
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetUntaggedArticles", con) { CommandType = CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new Article
                {
                    Id = rd.GetInt32(rd.GetOrdinal("Id")),
                    Title = rd["Title"]?.ToString() ?? "",
                    Content = rd["Content"]?.ToString() ?? ""
                });
            }
            return list;
        }

        // 3) הוסף קשר כתבה-תגית אם לא קיים
        public void InsertArticleTagIfNotExists(int articleId, int tagId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_InsertArticleTagIfNotExists", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@articleId", articleId);
            cmd.Parameters.AddWithValue("@tagId", tagId);
            cmd.ExecuteNonQuery();
        }


    }
}


