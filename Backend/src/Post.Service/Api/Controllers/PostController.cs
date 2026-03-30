using Microsoft.AspNetCore.Mvc;
using Post.Service.Application.Interfaces;

namespace Post.Service.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly IPostService _service;

    public PostController(IPostService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostRequest request, CancellationToken ct = default)
    {
        await _service.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

