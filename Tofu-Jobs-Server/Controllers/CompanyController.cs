using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server.Controllers;

[Authorize, ApiController, Route("[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ApplicationDbContext db;
    public CompanyController(ApplicationDbContext database) => db = database;
    private string Owner => User.Identity!.Name!;
    private IQueryable<Company> Owned => db.Companies.Where(x => x.UserId == Owner);
    private bool Valid(Company input) => !string.IsNullOrWhiteSpace(input.Name) && input.Name.Length <= 200;
    private static void Copy(Company input, Company target)
    {
        target.Name = input.Name.Trim(); target.Location = input.Location; target.Website = input.Website;
        target.Industry = input.Industry; target.Description = input.Description;
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
    public async Task<IActionResult> Create(Company input)
    {
        if (!(Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        var item = new Company { UserId = Owner, };
        Copy(input, item); db.Companies.Add(item); await db.SaveChangesAsync();
        return Created($"/Company/{item.Id}", item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Company input)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        if (input.Id != 0 && input.Id != id) return BadRequest("Route and body IDs differ.");
        if (!(Valid(input))) return BadRequest("Invalid fields or related record ownership.");
        Copy(input, item); await db.SaveChangesAsync(); return Ok(item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Owned.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        if (await db.Jobs.AnyAsync(x => x.CompanyId == id)) return Conflict("Remove related jobs first.");
        db.Companies.Remove(item); await db.SaveChangesAsync(); return Ok(item);
    }
}
