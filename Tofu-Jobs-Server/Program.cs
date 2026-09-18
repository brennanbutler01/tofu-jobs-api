using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tofu_Jobs_Server;
using Tofu_Jobs_Server.Data;

var builder = WebApplication.CreateBuilder(args);
var localDemo = builder.Configuration.GetValue<bool>("LocalDemo:Enabled");
var visitorDemo = builder.Configuration.GetValue<bool>("VisitorDemo:Enabled");
if (localDemo && !builder.Environment.IsDevelopment()) throw new InvalidOperationException("Local demo is development-only.");
if (localDemo && visitorDemo) throw new InvalidOperationException("Choose one demo mode.");
const string localKey = "tofu-jobs-public-local-test-key-not-a-hosted-credential-2026";
var signingKey = visitorDemo ? builder.Configuration["VisitorDemo:SigningKey"] : localKey;
if (visitorDemo && (signingKey == null || signingKey.Length < 32)) throw new InvalidOperationException("VisitorDemo__SigningKey must contain at least 32 characters.");
var issuer = visitorDemo ? VisitorDemo.Issuer : "tofu-jobs-local";
var demo = localDemo || visitorDemo;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16384);
builder.Services.AddCors();
builder.Services.AddSingleton(new Cloudinary(new Account(
    builder.Configuration["Cloudinary:CloudName"] ?? (demo ? "disabled" : null) ?? throw new InvalidOperationException("Missing Cloudinary__CloudName"),
    builder.Configuration["Cloudinary:ApiKey"] ?? (demo ? "disabled" : null) ?? throw new InvalidOperationException("Missing Cloudinary__ApiKey"),
    builder.Configuration["Cloudinary:ApiSecret"] ?? (demo ? "disabled" : null) ?? throw new InvalidOperationException("Missing Cloudinary__ApiSecret"))));
builder.Services.AddScoped<JobListRepository>();
builder.Services.AddControllers(options => {
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    if (visitorDemo) options.Filters.Add<VisitorWriteFilter>();
});
builder.Services.AddAuthorization(options => options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier).Build());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnectionString") ?? throw new InvalidOperationException("Missing ConnectionStrings__DefaultConnectionString")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    if (demo) {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = issuer,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!)),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero, NameClaimType = ClaimTypes.NameIdentifier
        };
        if (visitorDemo) options.Events = new JwtBearerEvents { OnTokenValidated = async context => {
            var subject = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            if (!await db.VisitorSessions.AnyAsync(x => x.Id == subject && x.ExpiresAt > DateTime.UtcNow)) context.Fail("Expired or revoked visitor session.");
        }};
    } else {
        options.Authority = "https://" + (builder.Configuration["Auth0:Domain"] ?? throw new InvalidOperationException("Missing Auth0__Domain")) + "/";
        options.Audience = builder.Configuration["Auth0:Audience"] ?? throw new InvalidOperationException("Missing Auth0__Audience");
    }
});
if (visitorDemo) {
    builder.Services.AddScoped<VisitorWriteFilter>();
    builder.Services.AddHostedService<VisitorCleanup>();
    builder.Services.AddRateLimiter(options => {
        options.RejectionStatusCode = 429;
        options.AddFixedWindowLimiter("sessions", limiter => { limiter.PermitLimit = 30; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous", _ => new FixedWindowRateLimiterOptions {
                PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
            }));
    });
}
var app = builder.Build();
if (demo) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.OpenConnectionAsync();
    try {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(52125213)");
        try { await db.Database.EnsureCreatedAsync(); }
        finally { await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(52125213)"); }
    } finally { await db.Database.CloseConnectionAsync(); }
}
if (localDemo) app.MapGet("/dev/token/{user}", (string user) => {
    if (user is not ("alice" or "bob")) return Results.NotFound();
    var token = new JwtSecurityToken(issuer, issuer, [new Claim("sub", $"demo-{user}")], expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localKey)), SecurityAlgorithms.HmacSha256));
    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
});
if (visitorDemo) app.MapVisitorDemo(signingKey!);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseExceptionHandler(error => error.Run(async context => {
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new { error = "The request could not be completed." });
}));
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://127.0.0.1:5213", "http://localhost:5173"];
app.UseCors(policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
if (visitorDemo) app.UseRateLimiter();
app.MapControllers();
app.Run();
