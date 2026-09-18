using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class CoverLetterController : ControllerBase
{
    private readonly ApplicationDbContext db;
    public CoverLetterController(ApplicationDbContext database) => db = database;
    private string Owner => User.Identity!.Name!;
    private IQueryable<CoverLetter> Owned => db.CoverLetters.Where(x => x.UserId == Owner);
    private async Task<bool> Valid(CoverLetter input) => !string.IsNullOrWhiteSpace(input.Title) && input.Title.Length <= 200
        && (input.JobId == null || await db.Jobs.AnyAsync(x => x.Id == input.JobId && x.UserId == Owner))
        && Uri.TryCreate(input.Url, UriKind.Absolute, out var url) && url.Scheme == "https";
    private static void Copy(CoverLetter input, CoverLetter target)
    {
        target.Title = input.Title.Trim(); target.Url = input.Url; target.JobId = input.JobId;
    }
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await Owned.AsNoTracking().ToListAsync());
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUnique(int id)
    {
        var item = await Owned.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return item == null ? NotFound() : Ok(item);
    }
    [HttpPost]
    public async Task<IActionResult> Create(CoverLetter input)
    {
        if (!(await Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        var item = new CoverLetter { UserId = Owner, Created = DateTime.UtcNow, };
        Copy(input, item); db.CoverLetters.Add(item); await db.SaveChangesAsync();
        return Created($"/CoverLetter/{item.Id}", item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CoverLetter input)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        if (input.Id != 0 && input.Id != id) return BadRequest("Route and body IDs differ.");
        if (!(await Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        Copy(input, item); await db.SaveChangesAsync(); return Ok(item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        db.CoverLetters.Remove(item); await db.SaveChangesAsync(); return Ok(item);
    }
}
