using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Focusberry.Api.Data;
using Focusberry.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=focusberry;Username=focusberry;Password=focusberry";

builder.Services.AddDbContext<FocusberryDbContext>(options => options.UseNpgsql(connectionString));

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters long.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Focusberry",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Focusberry",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(
        builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FocusberryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "focusberry-api" }));

app.MapPost("/api/auth/register", async (RegisterRequest request, FocusberryDbContext db, IConfiguration config) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (!IsValidEmail(email) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "Use a valid email and a password of at least 8 characters." });

    if (await db.Users.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "An account with this email already exists." });

    var user = new User { Id = Guid.NewGuid(), Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) };
    db.Users.Add(user);
    db.States.Add(new UserState { UserId = user.Id, Data = "{}", Version = 0, UpdatedAt = DateTimeOffset.UtcNow });
    await db.SaveChangesAsync();
    return Results.Ok(CreateAuthResponse(user, config));
});

app.MapPost("/api/auth/login", async (LoginRequest request, FocusberryDbContext db, IConfiguration config) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();
    return Results.Ok(CreateAuthResponse(user, config));
});

var account = app.MapGroup("/api").RequireAuthorization();

account.MapGet("/me", async (ClaimsPrincipal principal, FocusberryDbContext db) =>
{
    var id = UserId(principal);
    var user = await db.Users.FindAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(new { user.Id, user.Email, user.CreatedAt });
});

account.MapGet("/sync", async (ClaimsPrincipal principal, FocusberryDbContext db) =>
{
    var id = UserId(principal);
    var state = await db.States.FindAsync(id);
    return state is null ? Results.NotFound() : Results.Ok(new { state.Data, state.Version, state.UpdatedAt });
});

account.MapPut("/sync", async (ClaimsPrincipal principal, SyncRequest request, FocusberryDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Data) || request.Data.Length > 5_000_000)
        return Results.BadRequest(new { message = "Invalid or oversized state." });

    var id = UserId(principal);
    var state = await db.States.FindAsync(id);
    if (state is null)
    {
        state = new UserState { UserId = id };
        db.States.Add(state);
    }

    // Last-write-wins for the MVP. The client sends an incrementing local version.
    if (request.Version >= state.Version)
    {
        state.Data = request.Data;
        state.Version = request.Version;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    return Results.Ok(new { state.Data, state.Version, state.UpdatedAt });
});

app.Run();

static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
static bool IsValidEmail(string email) => new System.Net.Mail.MailAddress(email).Address == email;

static AuthResponse CreateAuthResponse(User user, IConfiguration config)
{
    var issuer = config["Jwt:Issuer"] ?? "Focusberry";
    var audience = config["Jwt:Audience"] ?? "Focusberry";
    var secret = config["Jwt:Secret"]!;
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email) };
    var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddDays(30), signingCredentials: credentials);
    return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Id, user.Email);
}

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
record SyncRequest(string Data, long Version);
record AuthResponse(string Token, Guid UserId, string Email);
