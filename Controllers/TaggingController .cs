// Controllers/TaggingController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class TaggingController : ControllerBase
{
    [HttpPost("Run")]
    public async Task<IActionResult> RunTagging()
    {
        var runner = new TaggingRunner();
        await runner.RunAsync();
        return Ok("Tagging completed.");
    }
}
