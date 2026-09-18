using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("Activities")]
public class Activity
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string Title { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Note { get; set; }
    public bool IsCompleted { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public ActivityCategories ActivityCategory { get; set; }
}