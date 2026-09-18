using System.ComponentModel.DataAnnotations.Schema;

namespace Tofu_Jobs_Server.Data;

[Table("UserIds")]
public class UserId
{
    public string Id { get; set; }
}