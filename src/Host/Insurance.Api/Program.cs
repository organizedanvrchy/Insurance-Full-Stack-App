using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Insurance.Billing;
using Insurance.BuildingBlocks.Data;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog;
using Insurance.Claims;
using Insurance.Enrollments;
using Insurance.Identity;
using Insurance.Identity.Contracts;
using Insurance.Quotes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
                .AddJsonOptions(o =>
    // Enums travel as names ("Admin"), and unknown names/numbers are rejected with a 400.
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false))
)
                // Each module brings its own controllers.
                .AddApplicationPart(typeof(IdentityModule).Assembly)
                .AddApplicationPart(typeof(CatalogModule).Assembly)
                .AddApplicationPart(typeof(QuotesModule).Assembly)
                .AddApplicationPart(typeof(EnrollmentsModule).Assembly)
                .AddApplicationPart(typeof(BillingModule).Assembly)
                .AddApplicationPart(typeof(ClaimsModule).Assembly);
builder.Services.AddExceptionHandler<ApiExceptionHandler>(); // typed exceptions -> 400/404/409
builder.Services.AddProblemDetails(); // unhandled exceptions -> clean 500 JSON
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Insurance API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT only. Swagger adds the 'Bearer ' prefix."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ---- Modules ----
// Each module registers its own repositories and services. Modules talk to each other
// only through the interfaces in their *.Contracts projects.
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddIdentityModule();
builder.Services.AddCatalogModule();
builder.Services.AddQuotesModule();
builder.Services.AddEnrollmentsModule();
builder.Services.AddBillingModule();
builder.Services.AddClaimsModule();

// ---- JWT Authentication ----
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<32+ random chars>\"");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Account must still exist, be active, and have the same role.
        // Deactivating a customer takes effect immediately, 
        // instead of when their token expires. -- This may be costly when scaling up
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var idClaim = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(idClaim, out var userId))
                {
                    ctx.Fail("Invalid token.");
                    return;
                }

                var users = ctx.HttpContext.RequestServices.GetRequiredService<IUserDirectory>();
                var user = await users.GetAsync(userId);

                if (user is null || !user.IsActive)
                    ctx.Fail("Account is inactive.");
                else if (!ctx.Principal!.IsInRole(user.Role.ToString()))
                    ctx.Fail("Role has changed. Please sign in again.");
            }
        };
    });
builder.Services.AddAuthorization();

// CORS: Allowed Frontend that can call this API
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
                                          .Get<string[]>() ?? Array
                                          .Empty<string>();
builder.Services.AddCors(o => 
    o.AddPolicy("Angular", p =>
        p.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
    )
);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
