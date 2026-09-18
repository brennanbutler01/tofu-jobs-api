using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("Companies")]
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public string? User { get; set; }

    public ICollection<Job> Jobs { get; set; }
}