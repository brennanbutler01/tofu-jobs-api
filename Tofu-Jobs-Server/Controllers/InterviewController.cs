using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class InterviewController : ControllerBase
{
    private readonly ApplicationDbContext db;
    public InterviewController(ApplicationDbContext database) => db = database;
    private string Owner => User.Identity!.Name!;
    private IQueryable<Interview> Owned => db.Interviews.Where(x => x.UserId == Owner);
    private async Task<bool> Valid(Interview input) => input.Round > 0 && Enum.IsDefined(typeof(InterviewTypes), input.InterviewType) && input.End >= input.Start
        && await db.Jobs.AnyAsync(x => x.Id == input.JobId && x.UserId == Owner);
    private static void Copy(Interview input, Interview target)
    {
        target.Round = input.Round; target.InterviewType = input.InterviewType; target.JobId = input.JobId;
        target.Start = input.Start.ToUniversalTime(); target.End = input.End.ToUniversalTime();
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
    public async Task<IActionResult> Create(Interview input)
    {
        if (!(await Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        var item = new Interview { UserId = Owner, };
        Copy(input, item); db.Interviews.Add(item); await db.SaveChangesAsync();
        return Created($"/Interview/{item.Id}", item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Interview input)
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
        db.Interviews.Remove(item); await db.SaveChangesAsync(); return Ok(item);
    }
}
