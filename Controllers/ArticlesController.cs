﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;
using NewsSite1.Models;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly DBServices _db;
    private readonly NewsApiService _newsApiService;
    private readonly ArticleService _articleService;

    public ArticlesController(DBServices db, NewsApiService newsApiService, ArticleService articleService)
    {
        _db = db;
        _newsApiService = newsApiService;
        _articleService = articleService;
    }

    [HttpGet("All")]
    public IActionResult GetAll()
    {
        List<Article> articles = _db.GetAllArticles();
        return Ok(articles);
    }

    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        List<Article> result = _db.FilterArticles(sourceName, title, from, to);
        return Ok(result);
    }


    [HttpPost("Share")]
    public IActionResult ShareArticle([FromBody] SharedArticleRequest request)
    {
        if (request == null ||
            string.IsNullOrEmpty(request.SenderUsername) ||
            string.IsNullOrEmpty(request.ToUsername))
            return BadRequest("Invalid request");

        try
        {
            _db.ShareArticleByUsernames(request.SenderUsername, request.ToUsername, request.ArticleId, request.Comment);
            return Ok("Article shared successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }



    [HttpGet("SharedWithMe/{userId}")]
    public IActionResult GetSharedArticles(int userId)
    {
        var result = _db.GetArticlesSharedWithUser(userId);
        return Ok(result);
    }

    [HttpPost("SharePublic")]
    public IActionResult ShareArticlePublic([FromBody] PublicArticleShareRequest request)
    {
        try
        {
            _db.ShareArticlePublic(request.UserId, request.ArticleId, request.Comment);
            return Ok("Public share succeeded");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }








    [HttpGet("Public")]
    public IActionResult GetPublicArticles()
    {
        try
        {
            var result = _db.GetAllPublicArticles();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest("DB Error: " + ex.Message); // ← כדי לראות את השגיאה בדפדפן
        }
    }





    [HttpPost("AddPublicComment")]
    public IActionResult AddPublicComment([FromBody] PublicCommentRequest comment)
    {
        try
        {
            _db.AddPublicComment(comment.PublicArticleId, comment.UserId, comment.Comment);
            return Ok("Comment added successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message); // ← חשוב להדפיס את השגיאה
        }
    }




    [HttpGet("GetPublicComments/{articleId}")]
    public IActionResult GetPublicComments(int articleId)
    {
        try
        {
            var comments = _db.GetCommentsForPublicArticle(articleId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }



    [HttpPost("ImportExternal")]
    public async Task<IActionResult> ImportExternalArticles()
    {
        List<Article> externalArticles = await _newsApiService.GetTopHeadlinesAsync();

        foreach (var article in externalArticles)
        {
            int id = _articleService.SaveArticleAndGetId(article); // ← שימוש בשירות
            article.Id = id;
        }

        return Ok(externalArticles);
    }

    [HttpGet("WithTags")]
    public IActionResult GetArticlesWithTags()
    {
        DBServices dbs = new DBServices();
        var articles = dbs.GetArticlesWithTags();
        return Ok(articles);
    }


    [HttpGet("GetTagsForArticle/{articleId}")]
    public IActionResult GetTagsForArticle(int articleId)
    {
        DBServices db = new DBServices();
        var tags = db.GetTagsForArticle(articleId);
        return Ok(tags);
    }

    [HttpGet("Paginated")]
    public IActionResult GetPaginated(int page = 1, int pageSize = 6)
    {
        var paged = _db.GetArticlesPaginated(page, pageSize);
        return Ok(paged);
    }




}
