using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeoBank.API.Configuration;
using NeoBank.API.Endpoints;
using NeoBank.API.Endpoints.Validators;
using NeoBank.API.Middleware;
using NeoBank.Application.Commands.CreateAccount;
using NeoBank.Application.Commands.Login;
using NeoBank.Application.Commands.Logout;
using NeoBank.Application.Commands.Refresh;
using NeoBank.Application.Commands.Register;
using NeoBank.Application.Configuration;
using NeoBank.Application.EventHandlers;
using NeoBank.Application.Interfaces;
using NeoBank.Application.Queries.GetAccountById;
using NeoBank.Application.Queries.GetAccounts;
using NeoBank.Domain.Events;
using NeoBank.Domain.Interfaces;
using NeoBank.Infrastructure.Persistence;
using NeoBank.Infrastructure.Persistence.Interceptors;
using NeoBank.Infrastructure.Persistence.Repositories;
using NeoBank.Infrastructure.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RefreshCookieSettings>(builder.Configuration.GetSection("RefreshCookie"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsSettings.AllowedOrigins.Length == 0)
            return;

        policy.WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<DomainEventDispatcher>();

builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    opt.AddInterceptors(sp.GetRequiredService<DomainEventDispatcher>());
});

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtService>();

builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<GetAccountByIdHandler>();
builder.Services.AddScoped<GetAccountsHandler>();
builder.Services.AddScoped<RegisterHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshHandler>();
builder.Services.AddScoped<LogoutHandler>();

builder.Services.AddScoped<IDomainEventHandler<AccountCreated>, AccountCreatedLogger>();

builder.Services.AddValidatorsFromAssemblyContaining<OpenAccountRequestValidator>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "NeoBank API";
        options.Theme = ScalarTheme.Moon;
        options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.HttpClient);
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAccountEndpoints();

app.Run();