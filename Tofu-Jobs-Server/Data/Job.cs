using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("Jobs")]
public class Job
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Location { get; set; }
    public bool IsRemote { get; set; }
    public int Salary { get; set; }
    public Company? Company { get; set; }
    public int? CompanyId { get; set; }

    public int? JobListId { get; set; }
    public JobList? JobList { get; set; }
    public ICollection<Interview>? Interviews { get; set; }
    public ICollection<CoverLetter>? CoverLetters { get; set; }
    public string UserId { get; set; }

    public ICollection<Activity>? Activities { get; set; }
}