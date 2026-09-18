namespace Tofu_Jobs_Server.Exceptions.Auth0;

public enum Auth0SearchUserExceptionCode
{
    //if we cannot make the request to communicate with auth0 user search endpoint
    AUTH0_ENDPOINT_ERROR,

    //if there are just no users...
    NO_USERS,
    NO_CONTENT
}

public class SearchUserException : Exception
{
    public SearchUserException(Auth0SearchUserExceptionCode exceptionCode) : base(ExceptionCodeToString(exceptionCode))
    {
    }

    public Auth0SearchUserExceptionCode ExceptionCode { get; set; }


    public static string ExceptionCodeToString(Auth0SearchUserExceptionCode exceptionCode)
    {
        return exceptionCode switch
        {
            Auth0SearchUserExceptionCode.NO_USERS => "No users in database that match the email",
            Auth0SearchUserExceptionCode.AUTH0_ENDPOINT_ERROR => "Error fetching users from auth0",
            Auth0SearchUserExceptionCode.NO_CONTENT => "No content returned from API",
            _ => throw new ArgumentOutOfRangeException(nameof(exceptionCode))
        };
    }
}