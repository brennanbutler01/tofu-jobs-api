using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("CoverLetters")]
public class CoverLetter
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public string UserId { get; set; }
}