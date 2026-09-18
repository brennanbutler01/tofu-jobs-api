using Tofu_Jobs_Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Tofu_Jobs_Server;

public sealed class VisitorWriteFilter(ApplicationDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method is not ("POST" or "PUT" or "DELETE"))
        {
            await next(); return;
        }
        var subject = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var session = await db.VisitorSessions.FromSqlInterpolated(
            $"SELECT * FROM \"VisitorSessions\" WHERE \"Id\" = {subject} FOR UPDATE").SingleOrDefaultAsync();
        if (session == null || session.ExpiresAt <= DateTime.UtcNow)
        {
            context.Result = new UnauthorizedResult(); return;
        }
        if (context.HttpContext.Request.Method == "POST")
        {
            var count = await db.Companies.CountAsync(x => x.UserId == subject) + await db.Jobs.CountAsync(x => x.UserId == subject)
                + await db.JobLists.CountAsync(x => x.UserId == subject) + await db.Interviews.CountAsync(x => x.UserId == subject)
                + await db.CoverLetters.CountAsync(x => x.UserId == subject) + await db.Activities.CountAsync(x => x.UserId == subject);
            if (count >= 100) { context.Result = new ObjectResult(new { error = "Demo record limit reached. Delete records or reset the demo." }) { StatusCode = 429 }; return; }
        }
        var executed = await next();
        var status = (executed.Result as IStatusCodeActionResult)?.StatusCode ?? 200;
        // Commit before serializing the action result so a successful response means the write persisted.
        if (executed.Exception == null && status < 400) await transaction.CommitAsync();
    }
}
