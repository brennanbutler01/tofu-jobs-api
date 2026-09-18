using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("JobLists")]
public class JobList
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsUserCreated { get; set; }
    public string UserId { get; set; }
    public Collection<Job> Jobs { get; set; }
    public DefaultJobLists? DefaultJobLists { get; set; }
}