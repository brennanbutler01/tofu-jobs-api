using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class ActivityController(ApplicationDbContext db) : ControllerBase
{
    private string Owner => User.Identity!.Name!;
    private IQueryable<Activity> Owned => db.Activities.Where(x => x.UserId == Owner);
    private async Task<bool> Valid(Activity input) => !string.IsNullOrWhiteSpace(input.Title)
        && input.Title.Length <= 200 && (input.Note?.Length ?? 0) <= 10000
        && Enum.IsDefined(input.ActivityCategory)
        && (!input.StartDateTime.HasValue || !input.EndDateTime.HasValue || input.EndDateTime >= input.StartDateTime)
        && (input.JobId == null || await db.Jobs.AnyAsync(x => x.Id == input.JobId && x.UserId == Owner));
    private static void Copy(Activity input, Activity target)
    {
        target.Title = input.Title.Trim(); target.Note = input.Note;
        target.StartDateTime = input.StartDateTime?.ToUniversalTime(); target.EndDateTime = input.EndDateTime?.ToUniversalTime();
        target.IsCompleted = input.IsCompleted; target.DateCompleted = input.IsCompleted ? input.DateCompleted?.ToUniversalTime() ?? DateTime.UtcNow : null;
        target.ActivityCategory = input.ActivityCategory; target.JobId = input.JobId;
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
    public async Task<IActionResult> Create(Activity input)
    {
        if (!await Valid(input)) return BadRequest("Invalid activity fields or job ownership.");
        var item = new Activity { UserId = Owner };
        Copy(input, item); db.Activities.Add(item); await db.SaveChangesAsync();
        return Created($"/Activity/{item.Id}", item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Activity input)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        if (input.Id != 0 && input.Id != id) return BadRequest("Route and body IDs differ.");
        if (!await Valid(input)) return BadRequest("Invalid activity fields or job ownership.");
        Copy(input, item); await db.SaveChangesAsync(); return Ok(item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        db.Activities.Remove(item); await db.SaveChangesAsync(); return Ok(item);
    }
}
