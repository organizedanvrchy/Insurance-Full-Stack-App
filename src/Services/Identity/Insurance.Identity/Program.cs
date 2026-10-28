using Insurance.BuildingBlocks.Hosting;
using Insurance.Identity.Contracts;
using Insurance.Identity.Repositories;
using Insurance.Identity.Services;

// Identity service: users, sign-in and the JWTs every other service trusts.
// Calls no other service: the gateway's BFF adds policy and claim counts to the admin customer list.
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("Identity service");

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserDirectory, UserDirectory>();

var app = builder.Build();
await app.UseServiceDefaultsAsync();
app.Run();
