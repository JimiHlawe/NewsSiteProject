# NewsSite Project

A modern news aggregation and social platform built with ASP.NET Core 6.0 and Firebase, featuring AI-powered content tagging, personalized news feeds, and community interaction.

## Overview

NewsSite is a full-stack web application that aggregates news from external APIs (NewsAPI), enriches content with AI-generated tags and images, and provides users with a personalized news reading experience. The platform includes social features like commenting, sharing, liking, and user profiles with gamification elements.

## Features

### Core Functionality
- **News Aggregation**: Automatically fetches and displays top US news headlines from NewsAPI
- **AI-Powered Content Enhancement**:
  - Automatic article tagging using OpenAI
  - AI-generated article images
  - AI-generated advertisements
- **Personalized News Feed**: Articles filtered by user interest tags
- **Real-time Updates**: Firebase Realtime Database integration for live data synchronization

### User Features
- **User Authentication & Profiles**:
  - User registration and login
  - Profile customization with avatar levels (Bronze, Silver, Gold, etc.)
  - Profile image uploads
  - Email notifications settings
- **Social Interactions**:
  - Article commenting system
  - Like/favorite articles
  - Share articles with other users
  - User threads and inbox for private messages
- **Gamification**:
  - Avatar level progression system
  - Activity tracking and statistics

### Admin Features
- **User Management**: Activate/deactivate users, manage permissions
- **Content Moderation**: Control user sharing and commenting privileges
- **Site Statistics**: Monitor platform usage and engagement

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 6.0 (Web API)
- **Language**: C# with .NET 6
- **Database**: Microsoft SQL Server
- **External APIs**:
  - NewsAPI for news content
  - OpenAI for tagging and image generation
  - Firebase Realtime Database for real-time features

### Frontend
- **HTML/CSS/JavaScript**: Static frontend served from `wwwroot`
- **Firebase SDK**: Client-side Firebase integration
- **Pages**:
  - Index (main news feed)
  - Login/Profile
  - Favorites
  - Threads (messages)
  - Inbox
  - Admin dashboard

### Key Dependencies
- `Microsoft.Data.SqlClient` (5.1.0) - SQL Server connectivity
- `Swashbuckle.AspNetCore` (6.5.0) - API documentation (Swagger)

## Project Structure

```
NewsSiteProject/
├── Controllers/          # API endpoints
│   ├── AdminController.cs
│   ├── ArticlesController.cs
│   ├── CommentsController.cs
│   ├── LikesController.cs
│   ├── UsersController.cs
│   ├── AdsController.cs
│   └── TaggingController.cs
├── Models/              # Data models
│   ├── Article.cs
│   ├── User.cs
│   ├── Comment.cs
│   ├── Tag.cs
│   └── DTOs/           # Data transfer objects
├── Services/           # Business logic
│   ├── DBServices.cs
│   ├── NewsApiService.cs
│   ├── OpenAiTagService.cs
│   ├── ImageGenerationService.cs
│   ├── AdsGenerationService.cs
│   ├── FirebaseRealtimeService.cs
│   └── EmailService.cs
├── DAL/                # Data access layer
├── wwwroot/            # Static frontend files
│   ├── html/          # HTML pages
│   ├── css/           # Stylesheets
│   ├── scripts/       # JavaScript files
│   ├── pictures/      # Images
│   └── uploads/       # User-uploaded content
├── Program.cs          # Application entry point
├── NewsSite.csproj     # Project configuration
└── appsettings.json    # Application settings
```

## API Endpoints

### Articles Controller
**Discovery & Fetching**
- `GET /api/articles/AllFiltered?userId={id}` - Get filtered articles for a user based on their interest tags, logs fetch once per day
- `GET /api/articles/Sidebar?page={page}&pageSize={pageSize}` - Get paginated sidebar articles (default: page=1, pageSize=6)
- `GET /api/articles/Threads/{userId}` - Get all public threads (publicly shared articles) visible for the user
- `GET /api/articles/Inbox/{userId}` - Get all articles shared privately with the user (Inbox)

**Saved Articles (MyList)**
- `POST /api/articles/SaveArticle` - Save an article for a user (requires: UserId, ArticleId in body)
- `GET /api/articles/GetSavedArticles/{userId}` - Get all saved articles for a specific user
- `POST /api/articles/RemoveSavedArticle` - Remove a saved article for a user (requires: UserId, ArticleId in body)

**Sharing**
- `POST /api/articles/ShareByUsernames` - Share an article privately by usernames and update Firebase inbox count (requires: SenderUsername, ToUsername, ArticleId, Comment in body)
- `POST /api/articles/ShareToThreads` - Share an article publicly to Threads (requires: UserId, ArticleId, Comment in body)

**Reporting**
- `POST /api/articles/Report` - Report content (article/comment/thread) with a reason (requires: UserId, ReferenceType, ReferenceId, Reason in body)

### Users Controller
**Authentication & User Management**
- `POST /api/users/Register` - Register a new user with optional interest tags
- `POST /api/users/Login` - Authenticate a user by email and password
- `GET /api/users/GetUserById/{id}` - Get a user by ID (refreshes avatar levels first)
- `POST /api/users/UpdatePassword` - Update user's password (requires: UserId, NewPassword in body)
- `POST /api/users/ToggleNotifications` - Toggle email notifications on/off for a user (requires: UserId, Enable in body)
- `GET /api/users/AllUsers` - Get all registered users

**Tags Management**
- `GET /api/users/AllTags` - Get the full list of available tags
- `GET /api/users/GetTags/{userId}` - Get all tags assigned to a specific user
- `POST /api/users/AddTag` - Add a tag to a user (requires: UserId, TagId in body)
- `POST /api/users/RemoveTag` - Remove a tag from a user (requires: UserId, TagId in body)

**User Blocking**
- `POST /api/users/BlockUser` - Block another user by username (requires: BlockerUserId, BlockedUsername in body)
- `POST /api/users/UnblockUser` - Unblock a previously blocked user (requires: BlockerUserId, BlockedUserId in body)
- `GET /api/users/BlockedByUser/{userId}` - Get all users blocked by the given user

**Shared Articles & Inbox**
- `POST /api/users/MarkSharedAsRead/{userId}` - Mark all shared articles for a user as read and update Firebase inbox count
- `DELETE /api/users/RemoveShared/{sharedId}` - Remove a shared article by its sharedId

**Media Upload**
- `POST /api/users/UploadProfileImage?userId={userId}` - Upload a profile image (multipart/form-data file upload)

### Comments Controller
**Article Comments**
- `POST /api/comments/AddToArticle` - Add a comment to a regular article (validates user can comment, requires: ArticleId, UserId, Comment in body)
- `GET /api/comments/GetForArticle/{articleId}` - Get all comments for a specific regular article

**Public Thread Comments**
- `POST /api/comments/AddToThreads` - Add a comment to a public thread article (requires: PublicArticleId, UserId, Comment in body)
- `GET /api/comments/GetForThreads/{articleId}` - Get all comments for a public thread article

**Comment Likes**
- `POST /api/comments/ToggleLikeForArticles` - Toggle like on a regular article comment (requires: UserId, CommentId in body)
- `POST /api/comments/ToggleLikeForThreads` - Toggle like on a public thread comment (requires: UserId, PublicCommentId in body)

**Like Counts**
- `GET /api/comments/ArticleCommentLikeCount/{commentId}` - Get like count for a regular article comment
- `GET /api/comments/ThreadsCommentLikeCount/{publicCommentId}` - Get like count for a public thread comment

### Likes Controller
**Article Likes**
- `POST /api/likes/ToggleArticleLike` - Toggle like on a regular article and update Firebase like count (requires: UserId, ArticleId in body)
- `POST /api/likes/ToggleThreadLike` - Toggle like on a public thread article and update Firebase like count (requires: UserId, PublicArticleId in body)

### Admin Controller
**Statistics & Reports**
- `GET /api/admin/LikesStats` - Get like statistics for the site
- `GET /api/admin/Reports/TodayCount` - Get the number of article reports created today
- `GET /api/admin/AllReports` - Get all reports (articles and comments)
- `GET /api/admin/GetStatistics` - Get overall site statistics

**User Moderation**
- `POST /api/admin/SetActiveStatus` - Set a user's active status to enable/disable login (requires: UserId, IsActive in body)
- `POST /api/admin/SetSharingStatus` - Set whether a user can share articles (requires: UserId, CanShare in body)
- `POST /api/admin/SetCommentingStatus` - Set whether a user can comment on articles (requires: UserId, CanComment in body)

**Article Management**
- `POST /api/admin/AddUserArticle` - Add a new user-submitted article (requires: Title, Description, Content, Author, SourceUrl, ImageUrl, PublishedAt in body)
- `POST /api/admin/GetFromNewsAPI` - Import external articles from NewsAPI and save new ones
- `POST /api/admin/FixMissingImages` - Generate images for articles missing an image using AI
- `POST /api/admin/DeleteReportedTarget` - Delete a reported target (article or comment) and its associated reports (requires: TargetId, TargetKind in body)

### Ads Controller
**Ad Generation**
- `GET /api/ads/Generate?category={category}` - Generate an ad (text + image) for the given category using AI

### Tagging Controller
**AI Tagging**
- `POST /api/tagging/RunTagging` - Trigger the article-tagging process using OpenAI and send notifications

## Getting Started

### Prerequisites
- .NET 6.0 SDK
- SQL Server (local or remote)
- Firebase account (for realtime features)
- NewsAPI key
- OpenAI API key

### Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd NewsSiteProject
```

2. Configure your database connection:
   - Update the connection string in `appsettings.json` or `appsettings.Development.json`

3. Add your API keys:
   - **Important**: Never commit API keys to source control
   - Store API keys in environment variables or secure configuration files
   - Use `appsettings.Development.json` (excluded from source control) for local development
   - For production, use Azure Key Vault, environment variables, or your hosting provider's secrets management
   - Configure NewsAPI key, OpenAI API keys, and Firebase credentials securely
   - Update the services to read keys from configuration instead of hardcoded values

4. Restore dependencies:
```bash
dotnet restore
```

5. Build the project:
```bash
dotnet build
```

6. Run the application:
```bash
dotnet run
```

The application will start on `https://localhost:5001` (or the port specified in `launchSettings.json`).

### Development

To run in development mode with Swagger documentation:
```bash
dotnet run --environment Development
```

Access Swagger UI at: `https://localhost:5001/swagger`

## Configuration

### CORS
⚠️ **Security Warning**: The application is currently configured to allow all origins, headers, and methods. This configuration is insecure for production environments.

**For production deployments**, you MUST:
- Restrict CORS to specific trusted origins
- Update the CORS policy in `Program.cs` to only allow requests from your frontend domain(s)
- Example:
  ```csharp
  app.UseCors(policy =>
      policy.WithOrigins("https://your-domain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
  );
  ```

### Static Files
Static files are served from the `wwwroot` directory. The application is configured to serve static HTML, CSS, JavaScript, and uploaded images.

### Firebase Hosting
The project includes Firebase configuration (`firebase.json`) for hosting the public-facing site.

## Features in Detail

### AI Tagging System
Articles are automatically tagged using OpenAI's API to categorize content by topics like Politics, Technology, Sports, Entertainment, etc. Users can select interest tags to personalize their news feed.

### Image Generation
When articles lack images, the system can generate relevant images using AI based on the article content.

### Notification System
Users can opt-in to receive email notifications for various events like new comments, likes, and messages.

### Avatar Progression
Users earn avatar levels (Bronze → Silver → Gold → Platinum → Diamond) based on their activity and engagement on the platform.

## Contributing

This is a personal project. If you'd like to contribute or have suggestions, please reach out to the repository owner.

## License

This project is private and not currently licensed for public use.

## Contact

For questions or issues, please contact the repository owner through GitHub.
