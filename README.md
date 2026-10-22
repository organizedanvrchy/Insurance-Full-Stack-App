# Insurance Management System: starter (auth + Policies CRUD)

## 0. Prereqs
- .NET SDK (current LTS), Node 20+, Angular CLI (`npm i -g @angular/cli`), VS Code
- SQL Server. On Mac/Linux, or if you don't want a full install, use Docker:
  `docker run -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`
  Then change the connection string to
  `Server=localhost,1433;Database=InsuranceDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;`
- VS Code extensions: C# Dev Kit, SQL Server (mssql), Angular Language Service

## 1. Database
Run `Database/01_schema.sql` then `Database/02_stored_procedures.sql` (in the mssql extension: open file -> Run).

## 2. API
```bash
dotnet new webapi -n InsuranceApi --use-controllers
cd InsuranceApi
dotnet add package Microsoft.Data.SqlClient
dotnet add package BCrypt.Net-Next
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore --version 6.9.0
```
- Delete the template's WeatherForecast files, then copy the contents of `Api/` over the project (replace Program.cs and appsettings.json).
- Set your signing key (never commit it):
```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-random-characters!!"
dotnet run --urls http://localhost:5080
```
- Open http://localhost:5080/swagger. Try POST /api/auth/register, copy the token, click Authorize, paste it, call GET /api/auth/me.
- Make yourself admin: `UPDATE dbo.Users SET Role='Admin' WHERE Email='you@example.com';` then log in again (the role lives inside the token).

## 3. Angular
```bash
ng new insurance-web --routing --style=css
cd insurance-web
```
- Copy `Frontend/src/app/*` into `src/app/` (merge app.config.ts with the generated one; keep the generated root component, make sure it contains `<router-outlet />`).
- Append `styles-snippet.css` to `src/styles.css`.
- `ng serve` -> http://localhost:4200

## Request flow
Angular form -> AuthService (HttpClient) -> interceptor adds JWT -> AuthController -> AuthService (C#)
-> UserRepository -> `usp_User_Register` -> Users table
