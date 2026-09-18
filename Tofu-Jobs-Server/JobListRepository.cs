using System.Collections.ObjectModel;
using Tofu_Jobs_Server.Data;

namespace Tofu_Jobs_Server;

public class JobListRepository
{
    private ApplicationDbContext _db;

    public void SetDbContext(ApplicationDbContext dbContext)
    {
        _db = dbContext;
    }

    private JobList _createDefaultList(DefaultJobLists i, string userId)
    {
        return new JobList
        {
            Title = Enum.GetName(typeof(DefaultJobLists), i) ?? "Wishlist", Jobs = new Collection<Job>(),
            DefaultJobLists = i, UserId = userId, IsUserCreated = false
        };
    }

    private List<JobList> _iterateDefaultListEnum(string userId)
    {
        var jobLists = new List<JobList>();

        foreach (int i in Enum.GetValues(typeof(DefaultJobLists)))
            jobLists.Add(_createDefaultList((DefaultJobLists)i, userId));

        return jobLists;
    }


    //create the default job lists for a user that we require to show kanban boards on front end
    public async Task<List<JobList>> CreateDefaultLists(string userId)
    {
        var jobLists = _iterateDefaultListEnum(userId);

        await _db.JobLists.AddRangeAsync(jobLists);
        await _db.SaveChangesAsync();
        return jobLists;
    }
}