using Microsoft.AspNetCore.Mvc;
using NewsSite1.Services;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly OpenAiTagService _tagService;

    public TestController(OpenAiTagService tagService)
    {
        _tagService = tagService;
    }

    [HttpPost("DetectTags")]
    public async Task<IActionResult> DetectTags([FromBody] ArticleDto dto)
    {
        var tags = await _tagService.DetectTagsAsync(dto.Title, dto.Content);
        return Ok(tags);
    }
}

public class ArticleDto
{
    public string Title { get; set; }
    public string Content { get; set; }
}
