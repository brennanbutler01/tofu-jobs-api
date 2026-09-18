using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class JobController : ControllerBase
{
    private readonly ApplicationDbContext db;
    public JobController(ApplicationDbContext database) => db = database;
    private string Owner => User.Identity!.Name!;
    private IQueryable<Job> Owned => db.Jobs.Where(x => x.UserId == Owner);
    private async Task<bool> Valid(Job input) => !string.IsNullOrWhiteSpace(input.Title) && input.Title.Length <= 200 && input.Salary >= 0
        && (input.CompanyId == null || await db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.UserId == Owner))
        && (input.JobListId == null || await db.JobLists.AnyAsync(x => x.Id == input.JobListId && x.UserId == Owner));
    private static void Copy(Job input, Job target)
    {
        target.Title = input.Title.Trim(); target.Location = input.Location ?? string.Empty; target.IsRemote = input.IsRemote;
        target.Salary = input.Salary; target.CompanyId = input.CompanyId; target.JobListId = input.JobListId;
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
    public async Task<IActionResult> Create(Job input)
    {
        if (!(await Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        var item = new Job { UserId = Owner, };
        Copy(input, item); db.Jobs.Add(item); await db.SaveChangesAsync();
        return Created($"/Job/{item.Id}", item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Job input)
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
        if (await db.Interviews.AnyAsync(x => x.JobId == id) || await db.CoverLetters.AnyAsync(x => x.JobId == id) || await db.Activities.AnyAsync(x => x.JobId == id)) return Conflict("Remove related records first.");
        db.Jobs.Remove(item); await db.SaveChangesAsync(); return Ok(item);
    }
}
