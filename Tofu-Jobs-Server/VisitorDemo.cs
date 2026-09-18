using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tofu_Jobs_Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Tofu_Jobs_Server;

public static class VisitorDemo
{
    public const string Issuer = "tofu-jobs-visitor-demo";

    public static void MapVisitorDemo(this WebApplication app, string signingKey)
    {
        app.MapPost("/demo/session", async (ApplicationDbContext db) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(52125212)");
            if (await db.VisitorSessions.CountAsync() >= 100) return Results.Json(new { error = "Demo capacity reached. Please try again later." }, statusCode: 503);
            var session = new VisitorSession
            {
                Id = $"visitor-{Guid.NewGuid():N}",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            db.VisitorSessions.Add(session);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            var token = new JwtSecurityToken(Issuer, Issuer,
                [new Claim("sub", session.Id)], expires: session.ExpiresAt,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256));
            return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), subject = session.Id, expiresAt = session.ExpiresAt });
        }).RequireRateLimiting("sessions");
        app.MapGet("/demo/session", (ClaimsPrincipal user) => Results.Ok(new { subject = user.FindFirst(ClaimTypes.NameIdentifier)!.Value }))
            .RequireAuthorization();
        app.MapDelete("/demo/session", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            await DeleteSession(db, user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    public static async Task DeleteSession(ApplicationDbContext db, string subject)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize reset with in-flight writes so revoked sessions cannot leave new records behind.
        var session = await db.VisitorSessions.FromSqlInterpolated(
            $"SELECT * FROM \"VisitorSessions\" WHERE \"Id\" = {subject} FOR UPDATE").SingleOrDefaultAsync();
        if (session == null) return;
        await db.Activities.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        await db.Interviews.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        await db.CoverLetters.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        await db.Jobs.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        await db.Companies.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        await db.JobLists.Where(x => x.UserId == subject).ExecuteDeleteAsync();
        db.VisitorSessions.RemoveRange(await db.VisitorSessions.Where(s => s.Id == subject).ToListAsync());
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}

public sealed class VisitorCleanup(IServiceScopeFactory scopes, ILogger<VisitorCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var expired = await db.VisitorSessions.Where(s => s.ExpiresAt <= DateTime.UtcNow)
                    .Select(s => s.Id).ToListAsync(stoppingToken);
                foreach (var subject in expired) await VisitorDemo.DeleteSession(db, subject);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Visitor data cleanup failed; retrying on the next interval"); }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
