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

### Articles
- `GET /api/articles/AllFiltered?userId={id}` - Get personalized articles
- `GET /api/articles/GetNewsApis` - Fetch latest news from NewsAPI
- `POST /api/articles/Create` - Create a new article
- `PUT /api/articles/Update` - Update an article

### Users
- `POST /api/users/Register` - Register a new user
- `POST /api/users/Login` - User authentication
- `GET /api/users/{id}` - Get user profile
- `PUT /api/users/Update` - Update user profile
- `POST /api/users/UploadProfileImage` - Upload profile picture

### Comments
- `GET /api/comments?articleId={id}` - Get article comments
- `POST /api/comments/Create` - Add a comment
- `DELETE /api/comments/{id}` - Delete a comment

### Likes
- `POST /api/likes/Toggle` - Like/unlike an article
- `GET /api/likes/User?userId={id}` - Get user's liked articles

### Admin
- `GET /api/admin/statistics` - Get site statistics
- `PUT /api/admin/user/activate` - Activate/deactivate users
- `PUT /api/admin/user/permissions` - Manage user permissions

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
