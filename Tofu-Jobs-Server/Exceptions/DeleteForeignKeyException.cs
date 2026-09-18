using Microsoft.AspNetCore.Mvc;

namespace Tofu_Jobs_Server.Exceptions;

[Serializable]
public class DeleteForeignKeyException : ControllerBase
{
    private readonly int _status = StatusCodes.Status409Conflict;
    private string _detail;
    private string _title;
    private string _type;
    private ForeignKeyViolations _violation;

    public DeleteForeignKeyException()
    {
    }

    public DeleteForeignKeyException(ForeignKeyViolations violation, string type)
    {
        _violation = violation;
        _type = type;
        _title = _getTitle();
        _detail = _getDetail();
    }

    private string _getTitle()
    {
        return _violation switch
        {
            ForeignKeyViolations.CompanyJobs => "Company has jobs",
            ForeignKeyViolations.JobListJobs => "Job list still has jobs",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string _getDetail()
    {
        return _violation switch
        {
            ForeignKeyViolations.CompanyJobs =>
                "Companies has jobs still, so it is not possible to move forward without deleting the jobs related to this company.",
            ForeignKeyViolations.JobListJobs =>
                "Job List still has jobs, so it is not possible to remove the list. Please delete all of the jobs on the list first.",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [HttpDelete]
    public IActionResult ProblemResult()
    {
        return Problem(_detail, type: _type, title: _title, statusCode: _status);
    }
}