using Npgsql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tofu_Jobs_Server.Data;
using Tofu_Jobs_Server.Exceptions;

namespace Tofu_Jobs_Server.Controllers;

[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Route("[controller]")]
public class JobListController : DeleteForeignKeyException
{
    private readonly ApplicationDbContext _db;
    private readonly JobListRepository _jobListRepository;
    private readonly ILogger<JobListController> _logger;

    public JobListController(ILogger<JobListController> logger, ApplicationDbContext dbContext,
        JobListRepository jobListRepository)
    {
        _logger = logger;
        _db = dbContext;
        _jobListRepository = jobListRepository;
        _jobListRepository.SetDbContext(_db);
    }

    [HttpGet(Name = "GetJobLists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var jobLists = await _db.JobLists.Where(list => list.UserId == User.Identity.Name).ToListAsync();

        return Ok(jobLists);
    }

    [HttpGet("{id}", Name = "GetJobList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnique(int id)
    {
        var list = await _db.JobLists.FirstOrDefaultAsync(list => list.Id == id && list.UserId == User.Identity!.Name);

        if (list == null) return NotFound();

        return Ok(list);
    }

    [HttpPost(Name = "CreateJobList")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(JobList jobList)
    {
        if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(jobList.Title) || jobList.Title.Length > 200)) return BadRequest($"Invalid jobList model - {ModelState.Values}");

        jobList = new JobList { Title = jobList.Title.Trim(), UserId = User.Identity!.Name!, IsUserCreated = true };
        await _db.JobLists.AddAsync(jobList);
        await _db.SaveChangesAsync();
        return Created($"/JobList/{jobList.Id}", jobList);
    }

    [HttpDelete("{id}", Name = "DeleteJobList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var list = await _db.JobLists.FirstOrDefaultAsync(list => list.Id == id && list.UserId == User.Identity!.Name);

        if (list == null) return NotFound();

        _db.JobLists.Remove(list);
        try
        {
            await _db.SaveChangesAsync();

            return Ok(list);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            return new DeleteForeignKeyException(ForeignKeyViolations.JobListJobs, ex.GetType().ToString())
                .ProblemResult();
        }
    }

    [HttpPut("{id}", Name = "UpdateJobList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, JobList updatedJobList)
    {
        if (!ModelState.IsValid) return BadRequest();

        var item = await _db.JobLists.SingleOrDefaultAsync(x => x.Id == id && x.UserId == User.Identity!.Name);
        if (item == null) return NotFound();
        if ((updatedJobList.Id != 0 && updatedJobList.Id != id) || (string.IsNullOrWhiteSpace(updatedJobList.Title) || updatedJobList.Title.Length > 200)) return BadRequest();
        item.Title = updatedJobList.Title.Trim();
        await _db.SaveChangesAsync();

        return Ok(item);
    }
}