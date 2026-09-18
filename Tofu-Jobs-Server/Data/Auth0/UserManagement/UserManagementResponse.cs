namespace Tofu_Jobs_Server.Data.Auth0.UserManagement;

public class UserManagementResponse
{
    public string email { get; set; }
    public bool email_verified { get; set; }
    public string username { get; set; }
    public string phone_number { get; set; }
    public bool phone_verified { get; set; }
    public string user_id { get; set; }
    public string created_at { get; set; }
    public string updated_at { get; set; }
    public List<Identity> identities { get; set; }
    public AppMetadata app_metadata { get; set; }
    public UserMetadata user_metadata { get; set; }
    public string picture { get; set; }
    public string name { get; set; }
    public string nickname { get; set; }
    public List<string> multifactor { get; set; }
    public string last_ip { get; set; }
    public string last_login { get; set; }
    public int logins_count { get; set; }
    public bool blocked { get; set; }
    public string given_name { get; set; }
    public string family_name { get; set; }
}