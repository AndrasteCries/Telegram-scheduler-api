using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TelegramScheduler.Services.Exeptions;
using TelegramScheduler.Services.Posts;
using TelegramScheduler.Services.Templates;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found");

builder.Services.Configure<TdLibOptions>(
    builder.Configuration.GetSection("TdLib"));

builder.Services.AddHangfire(cfg => cfg
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddScoped<IPasswordHasher<TelegramScheduler.Models.User>, PasswordHasher<TelegramScheduler.Models.User>>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
builder.Services.AddSingleton<TelegramSessionManager>();
builder.Services.AddScoped<TelegramSessionService>();
builder.Services.AddScoped<TelegramChannelService>();
builder.Services.AddScoped<TelegramAccountService>();
builder.Services.AddScoped<TelegramMediaService>();
builder.Services.AddScoped<SignedUrlService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<PostDateValidator>();
builder.Services.AddScoped<PostFileValidator>();
builder.Services.AddScoped<PostAccessService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("auth_token"))
                {
                    context.Token = context.Request.Cookies["auth_token"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
}); 

builder.Services.AddAuthorization();
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("MediaStorage"));

new TdLib.TdClient().Execute(new TdLib.TdApi.SetLogVerbosityLevel { NewVerbosityLevel = 1 });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();