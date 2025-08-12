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
using System.ComponentModel.Design;


namespace NewsSite1.DAL
{
    public class DBServices
    {
        // ============================================
        // ========== BASE CONNECTION ================
        // ============================================
        public DBServices() { }

        // ===== BASE CONNECTION (No DI, Lazy static) =====
        private static readonly Lazy<string> _connStr = new(() =>
        {
            // Get the base directory where the application is running
            var basePath = AppContext.BaseDirectory;

            // Get the current environment (e.g., Development, Production)
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Build configuration from appsettings.json and environment-specific file
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Main settings file
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true) // Environment-specific settings
                .AddEnvironmentVariables() // Include environment variables
                .Build();

            // Retrieve the connection string by name
            var cs = config.GetConnectionString("myProjDB");

            // Throw an error if the connection string is missing
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    $"Missing connection string. Looked for ConnectionStrings:myProjDB in appsettings(.{env}).json");

            return cs; // Return the connection string
        });

        // Opens and returns a SQL connection
        public SqlConnection connect()
        {
            try
            {
                var con = new SqlConnection(_connStr.Value); // Use the lazy-loaded connection string
                con.Open(); // Open the connection
                return con; // Return the open connection
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("SQL open failed: " + ex); // Log the error
                throw new InvalidOperationException("Failed to open SQL connection", ex); // Rethrow with context
            }
        }

        // Executes a stored procedure without parameters
        public void ExecuteStoredProcedure(string spName)
        {
            using (SqlConnection con = connect()) // Open connection
            {
                SqlCommand cmd = new SqlCommand(spName, con); // Create SQL command
                cmd.CommandType = CommandType.StoredProcedure; // Set command type
                cmd.ExecuteNonQuery(); // Execute without returning rows
            }
        }



        // ============================================
        // ============= USERS ========================
        // ============================================

        /// Checks if an email already exists (used before registration).
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

        /// Checks if a username already exists (used before registration).
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

        /// Registers a new user and attaches initial tags if provided.
        /// Returns true on success; false if unique constraints are violated.
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

                        // Attach user interest tags if any (idempotent at SP level)
                        foreach (int tagId in user.Tags)
                            AddUserTag(userId, tagId);

                        return true;
                    }

                    return false;
                }
            }
            catch (SqlException ex)
            {
                // Handle unique constraint violations (duplicate name/email)
                // ex.Number 2627 = Unique index/constraint violation.
                // Message contains our specific unique constraint names.
                if (ex.Number == 2627 || ex.Message.Contains("UQ__News_Users") || ex.Message.Contains("UQ_News_Users_Name"))
                    return false;

                // Unexpected DB error - bubble up.
                throw;
            }
        }

        /// Authenticates a user by email and password.
        /// Returns a populated User object (without password) or null if not found.
        public User LoginUser(string email, string password)
        {
            using (SqlConnection con = connect())
            {
                // Use stored procedure (no inline SQL)
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
                        Password = null, // never return password for security
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

        /// Logs a successful user login (audit/analytics).
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

        /// Retrieves a single user by id. Password is never returned.
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
                        Password = null, // do not return password
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

        /// Returns all users (basic fields). Admin-focused listing.
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
                        // default to true if DB value is NULL (backward compatibility)
                        CanShare = reader["CanShare"] != DBNull.Value ? (bool)reader["CanShare"] : true,
                        CanComment = reader["CanComment"] != DBNull.Value ? (bool)reader["CanComment"] : true
                    });
                }
            }

            return users;
        }

        /// Resolves a user id by username. Returns null if not found.
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

        /// Updates the user's password (already validated upstream).
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

        /// Updates path to the user's profile image on disk/storage.
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

        /// Enables or disables email/notification preferences for a user.
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

        /// Sets whether the user account is active (admin control).
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

        /// Sets whether the user is allowed to share (admin control).
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

        /// Sets whether the user is allowed to comment (admin control).
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

        /// Returns whether the user can comment (policy flag).
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

        /// Returns whether the user can share (policy flag).
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

        /// Blocks another user (adds a row to a blocklist table).
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

        /// Returns the list of users that the given user has blocked.
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

        /// Removes a block entry (unblocks a previously blocked user).
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


        // Returns a list of user IDs that the given user has blocked
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
        // ============= ARTICLES =====================
        // ============================================

        /// Returns all articles (basic listing).
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

        /// Retrieves a single article by its ID (no tags populated here).
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
                            SourceUrl = reader["url"].ToString(),        // SourceUrl maps to 'url' column
                            ImageUrl = reader["imageUrl"].ToString(),
                            PublishedAt = Convert.ToDateTime(reader["PublishedAt"]),
                            Tags = new List<string>() // Tags can be loaded separately if needed
                        };
                    }
                }
            }

            return null;
        }

        /// Filters articles by optional title and date range.
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

        /// Returns articles prioritized by user's interest tags (match > no-tags > unrelated).
        public List<Article> GetArticlesFilteredByTags(int userId)
        {
            Dictionary<int, Article> articles = new Dictionary<int, Article>();

            using (SqlConnection con = connect())
            {
                // Use stored procedure (no inline SQL)
                SqlCommand cmd = new SqlCommand("NewsSP_GetArticlesFilteredByTags", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = (int)reader["id"];

                    // Create and cache the base article only once.
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

                    // Append tag name if present and not already added.
                    string tagName = reader["TagName"] as string;
                    if (!string.IsNullOrEmpty(tagName) && !articles[id].Tags.Contains(tagName))
                    {
                        articles[id].Tags.Add(tagName);
                    }
                }
            }

            return articles.Values.ToList();
        }

        /// Returns compact set of articles for sidebar (paginated, with tag aggregation).
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

                        // Build article row once; aggregate tags across joined rows.
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

        /// Saves (bookmarks) an article for a user.
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

        /// Returns user's saved articles including tag names (aggregated).
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

                    // Create base article object once per articleId.
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

                    // Append tag if present and not already included.
                    string tagName = reader["TagName"]?.ToString();
                    if (!string.IsNullOrEmpty(tagName) && !articlesDict[articleId].Tags.Contains(tagName))
                    {
                        articlesDict[articleId].Tags.Add(tagName);
                    }
                }
            }

            return articlesDict.Values.ToList();
        }

        /// Removes a user's saved article (unbookmark).
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

        /// Checks if an article with the given URL already exists (idempotency guard).
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

        /// Creates a new article, attaches tags (creating when needed), and notifies interested users.
        /// Returns the new Article Id, or -1 if the SourceUrl already exists.
        public int AddUserArticle(Article article)
        {
            // Fast-fail if URL already exists.
            if (ArticleExists(article.SourceUrl))
                return -1;

            int newArticleId;

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_AddArticle", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Null-safe value assignment
                cmd.Parameters.AddWithValue("@Title", article.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", article.Description ?? "");
                cmd.Parameters.AddWithValue("@Content", article.Content ?? "");
                cmd.Parameters.AddWithValue("@Author", article.Author ?? "");
                cmd.Parameters.AddWithValue("@SourceUrl", article.SourceUrl ?? "");
                cmd.Parameters.AddWithValue("@ImageUrl", article.ImageUrl ?? "");
                cmd.Parameters.AddWithValue("@PublishedAt", article.PublishedAt);

                // Expect SP to return the new ID (SCOPE_IDENTITY/OUTPUT)
                newArticleId = (int)cmd.ExecuteScalar();
            }

            // Ensure we always have a tags list to iterate over.
            if (article.Tags == null)
            {
                article.Tags = new List<string>();
            }

            List<int> tagIds = new List<int>();

            // Resolve/create tag ids and bind them to the new article (idempotent SP).
            foreach (string tagName in article.Tags)
            {
                int tagId = GetOrAddTagId(tagName);
                tagIds.Add(tagId);
                InsertArticleTag(newArticleId, tagId);
            }

            // Notify interested users (based on tag preferences).
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
                        // Avoid failing the whole operation due to email issues.
                        Console.WriteLine($"❌ Failed to send to {user.Email}: {ex.Message}");
                    }
                }
            }

            return newArticleId;
        }

        /// Returns articles missing images (imageUrl is NULL/empty) to backfill thumbnails.
        public List<Article> GetArticlesWithMissingImages()
        {
            List<Article> list = new List<Article>();

            using (SqlConnection conn = connect())
            {
                // Use SP to fetch only items needing image remediation
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

        /// Updates the image URL of an article (used after generation/fix).
        public void UpdateArticleImageUrl(int articleId, string imageUrl)
        {
            using (SqlConnection conn = connect())
            {
                // Call stored procedure to update image URL
                SqlCommand cmd = new SqlCommand("NewsSP_UpdateArticleImageUrl", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@url", imageUrl);
                cmd.Parameters.AddWithValue("@id", articleId);

                cmd.ExecuteNonQuery();
            }
        }



        // ============================================
        // ============ COMMENTS ======================
        // ============================================

        /// Adds a comment to a regular article.
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

        /// Returns all comments for a specific (regular) article.
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

        /// Adds a comment to a public article (thread).
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

        /// Returns all public comments for a given public article (ordered by creation time).
        /// Note: parameter is named 'articleId' but represents a public-article id (publicArticleId).
        public List<PublicComment> LoadThreadsComments(int articleId)
        {
            List<PublicComment> comments = new List<PublicComment>();

            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_LoadThreadsComments", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                // SP is expected to return comments already ordered by creation time.
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
        // ============== LIKES ======================
        // ============================================

        /// Returns the number of likes for a regular article and pushes it to Firebase (real-time sync).
        public int GetLikesCount(int articleId)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetLikesCount", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ArticleId", articleId);
            int count = (int)cmd.ExecuteScalar();

            // Update Firebase so all connected clients see the updated count immediately.
            var firebase = new FirebaseRealtimeService();
            firebase.UpdateLikeCount(articleId, count).Wait(); // blocking wait

            return count;
        }

        /// Toggles a like/unlike on a regular article for a given user.
        /// Returns true if like was added, false if removed.
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

        /// Returns the number of likes for a public article (thread) and pushes it to Firebase (real-time sync).
        public int GetThreadLikeCount(int publicArticleId)
        {
            using var con = connect();

            using var cmd = new SqlCommand("NewsSP_GetThreadLikeCount", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@PublicArticleId", publicArticleId);

            int count = (int)cmd.ExecuteScalar();

            // Push the like count to Firebase for real-time updates
            var firebase = new FirebaseRealtimeService();
            firebase.UpdateLikeCount(publicArticleId, count).Wait();

            return count;
        }

        /// Toggles a like/unlike on a public article (thread) for a given user.
        /// Returns true if the like was added, false if it was removed.
        public bool ToggleThreadLike(int userId, int publicArticleId)
        {
            using (var con = connect())
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

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

        /// Checks if a specific user has liked a specific public article (thread).
        /// Note: parameter `articleId` here actually represents the public article id.
        public bool CheckIfUserLikedThread(int userId, int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_CheckIfUserLikedThread", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        /// Toggles a like/unlike on a regular article comment for a given user.
        public void ToggleCommentLike(int userId, int commentId)
        {
            using var con = connect();

            SqlCommand cmd = new SqlCommand("NewsSP_ToggleCommentLike", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CommentId", commentId);

            cmd.ExecuteNonQuery();
        }

        /// Returns the number of likes for a specific regular article comment.
        public int GetArticleCommentLikeCount(int commentId)
        {
            using var con = connect();

            var cmd = new SqlCommand("NewsSP_ArticleCommentLikeCount", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CommentId", commentId);

            return (int)cmd.ExecuteScalar();
        }

        /// Toggles a like/unlike on a public comment for a given user.
        public void TogglePublicCommentLike(int userId, int publicCommentId)
        {
            using var con = connect();

            var cmd = new SqlCommand("NewsSP_TogglePublicCommentLike", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PublicCommentId", publicCommentId);

            cmd.ExecuteNonQuery();
        }

        /// Returns the number of likes for a given public comment.
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

        /// Shares an article from one user to another by their usernames.
        /// Resolves usernames to userIds and inserts a private share row.
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

        /// Shares an article publicly into Threads (creates a public-article entry).
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

        /// Returns all public threads (public-article shares) with tags and loaded public comments.
        /// Note: This method also ensures tag propagation to the public-article record (side-effect).
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
                        // Build the public article (thread) from the base joined result set
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

                            // Load tags and public comments per thread (separate DAL calls)
                            // Consider batching/caching if this becomes a hot path.
                            Tags = GetTagsForPublicArticle((int)reader["publicArticleId"]),
                            PublicComments = LoadThreadsComments((int)reader["publicArticleId"])
                        };

                        // Ensure tags are copied/synchronized to the public-article link (idempotent SP).
                        // Side-effect lives in DAL for now; could be moved to a service if preferred.
                        EnsurePublicArticleTagsExist(article.PublicArticleId);

                        list.Add(article);
                    }
                }
            }

            return list;
        }


        /// Returns all inbox (privately shared) articles for a specific user, including tags.
        /// Note: pulls base article via GetArticleById and then enriches with share metadata.
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

                        // Load base article (single source of truth for article fields)
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

                        // Enrich with tag names attached to the shared record
                        article.Tags = GetTagsForSharedArticle(sharedId);
                        inboxArticles.Add(article);
                    }
                }
            }

            return inboxArticles;
        }

        /// Marks all private shares as read for the given user.
        public void MarkSharedAsRead(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_MarkSharedAsRead", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                cmd.ExecuteNonQuery();
            }
        }

        /// Returns the unread inbox count for the given user (badge counter).
        public int GetUnreadSharedArticlesCount(int userId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetUnreadSharedArticlesCount", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                return (int)cmd.ExecuteScalar();
            }
        }

        /// Returns the tag names associated with a given shared article (by shared-id).
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

        /// Deletes a shared record (removes a private share from inbox/history).
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
        // ================ TAGS ======================
        // ============================================

        /// Adds a tag to a specific user (link in UserTags table)
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

        /// Returns all tags for a given user
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

        /// Gets the tag ID for a given name, inserting a new tag if it doesn't already exist
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

        /// Retrieves all tags in the system
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

        /// Links a tag to an article only if the link does not already exist
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

        /// Returns all tag names linked to a given public article
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

        /// Ensures that all tags linked to the original article are copied to its public-article share
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

        /// Returns all users interested in any of the given tag IDs
        public List<User> GetUsersInterestedInTags(List<int> tagIds)
        {
            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetUsersInterestedInTags", con)
            { CommandType = CommandType.StoredProcedure };

            // Tag IDs are passed to the SP as a comma-separated string
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

        /// Removes a tag from a user (unlink from UserTags table)
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

        // ============================================
        // =============== REPORTS ====================
        // ============================================

        /// Reports a piece of content (article or comment) for abuse, spam, etc.
        public void ReportContent(int userId, string referenceType, int referenceId, string reason)
        {
            using (SqlConnection con = connect())
            using (SqlCommand cmd = new SqlCommand("NewsSP_ReportContent", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Who is reporting
                cmd.Parameters.AddWithValue("@UserId", userId);
                // Type of content ("Article" / "Comment")
                cmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                // The ID of the content being reported
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                // Reason for reporting (empty string if null)
                cmd.Parameters.AddWithValue("@Reason", reason ?? "");

                cmd.ExecuteNonQuery();
            }
        }

        /// Retrieves all reports in the system for admin review
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
                        Id = (int)reader["id"],                                  // Report ID
                        ReporterId = (int)reader["ReporterId"],                  // ID of user who reported
                        ReporterName = reader["ReporterName"].ToString(),        // Name of reporting user
                        ReportType = reader["referenceType"].ToString(),         // "Article" or "Comment"
                        ReferenceId = (int)reader["referenceId"],                // ID of reported content
                        Reason = reader["reason"].ToString(),                    // Report reason
                        ReportedAt = Convert.ToDateTime(reader["reportedAt"]),   // When report was made
                        Content = reader["ReportedContent"] != DBNull.Value
                                    ? reader["ReportedContent"].ToString(): null,// The content that was reported
                        TargetName = reader["TargetName"] != DBNull.Value
                                    ? reader["TargetName"].ToString(): null // Name of user whose content was reported
                    };
                    list.Add(r);
                }
            }

            return list;
        }

        /// Returns the number of article reports submitted today
        public int GetTodayArticleReportsCount()
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_GetTodayArticleReportsCount", con);
                cmd.CommandType = CommandType.StoredProcedure;

                return (int)cmd.ExecuteScalar();
            }
        }

        public int DeleteArticleAndReports(int articleId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_DeleteArticleAndReports", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int DeleteCommentAndReports(int commentId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_DeleteCommentAndReports", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CommentId", commentId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int DeletePublicCommentAndReports(int publicCommentId)
        {
            using (SqlConnection con = connect())
            {
                SqlCommand cmd = new SqlCommand("NewsSP_DeletePublicCommentAndReports", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@PublicCommentId", publicCommentId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool CommentExists(int id)
        {
            using var con = connect();
            using var cmd = new SqlCommand("SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.News_Comments WHERE id=@id) THEN 1 ELSE 0 END", con);
            cmd.Parameters.AddWithValue("@id", id);
            return (int)cmd.ExecuteScalar() == 1;
        }

        public bool PublicCommentExists(int id)
        {
            using var con = connect();
            using var cmd = new SqlCommand("SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.News_PublicComments WHERE id=@id) THEN 1 ELSE 0 END", con);
            cmd.Parameters.AddWithValue("@id", id);
            return (int)cmd.ExecuteScalar() == 1;
        }

        // ============================================
        // ============== STATISTICS ==================
        // ============================================

        /// Retrieves overall site statistics: 
        /// total users, total articles, total saved articles, 
        /// number of logins today, and number of article fetches today.
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

        /// Retrieves likes statistics for both articles and threads.
        /// Includes total likes and likes added today for each type.
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

        /// Logs an article fetch event for the given user.
        /// Used for tracking user activity and generating statistics.
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



        // ============================================
        // ============== TAGGING =====================
        // ============================================

        /// Retrieves all articles that currently have no tags assigned to them.
        /// Used in the tagging process to identify which articles still need tagging.
        public List<Article> GetUntaggedArticles()
        {
            var list = new List<Article>();

            using var con = connect();
            using var cmd = new SqlCommand("NewsSP_GetUntaggedArticles", con)
            {
                CommandType = CommandType.StoredProcedure
            };

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


    }
}


