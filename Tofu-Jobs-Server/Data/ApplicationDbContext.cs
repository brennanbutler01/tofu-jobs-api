using Microsoft.EntityFrameworkCore;

namespace Tofu_Jobs_Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<VisitorSession> VisitorSessions { get; set; }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobList> JobLists { get; set; }
    public DbSet<Interview> Interviews { get; set; }
    public DbSet<CoverLetter> CoverLetters { get; set; }
    public DbSet<UserId> UserIds { get; set; }
    public DbSet<Activity> Activities { get; set; }
}