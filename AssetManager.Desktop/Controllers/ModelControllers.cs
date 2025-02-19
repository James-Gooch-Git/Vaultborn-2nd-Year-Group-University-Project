using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController]
[Route("api/models")]
public class ModelsController : ControllerBase
{
    private static string selectedModelId = null;

    [HttpPost("select")]
    public IActionResult SelectModel([FromBody] string modelId)
    {
        selectedModelId = modelId;
        return Ok();
    }

    [HttpGet("selected_model")]
    public IActionResult GetSelectedModel()
    {
        return Ok(new { model_id = selectedModelId });
    }

    [HttpGet("download/{modelId}")]
    public IActionResult DownloadModel(string modelId)
    {
        string filePath = Path.Combine("C:\\AssetManager\\Models", $"{modelId}.f3d");

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return File(fileStream, "application/octet-stream", $"{modelId}.f3d");
    }
}