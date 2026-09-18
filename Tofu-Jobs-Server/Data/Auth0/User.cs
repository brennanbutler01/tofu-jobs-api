namespace Tofu_Jobs_Server.Data;

public class User
{
    public User(string userId)
    {
        user_id = userId;
    }

    public string user_id { get; set; }
}