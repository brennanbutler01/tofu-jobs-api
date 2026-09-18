using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("Interviews")]
public class Interview
{
    public int Id { get; set; }
    public int Round { get; set; }
    public InterviewTypes InterviewType { get; set; }
    public int JobId { get; set; }
    public Job? Job { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string UserId { get; set; }
}