using System.Text;
using BCrypt.Net;
using ContactLandingApi.Data;
using ContactLandingApi.Models;
using ContactLandingApi.Requests;
using ContactLandingApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = BuildMySqlConnectionString(builder.Configuration);

var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<ContactCodeService>();
builder.Services.AddScoped<CaptchaValidationService>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Falta Jwt:Secret");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.MapGet("/monitor", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.MapGet("/monitoreo", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "monitoreo.html"));
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.MapGet("/api/debug/db-test", async (AppDbContext db) =>
{
    try
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        var result = await cmd.ExecuteScalarAsync();

        return Results.Ok(new
        {
            ok = true,
            result,
            connectionString = conn.ConnectionString
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            ok = false,
            error = ex.Message,
            inner = ex.InnerException?.Message,
            type = ex.GetType().FullName,
            full = ex.ToString()
        }, statusCode: 500);
    }
});


app.MapPost("/api/contacts/{id:int}/apply-discount", async (
    int id,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var contact = await db.Contacts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (contact is null)
    {
        return Results.NotFound(new { message = "Registro no encontrado." });
    }

    if (contact.UsoCodigo)
    {
        return Results.BadRequest(new { message = "El descuento ya fue aplicado para este registro." });
    }

    contact.UsoCodigo = true;
    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        message = "Descuento aplicado correctamente.",
        contact.Id,
        contact.UsoCodigo
    });
});

app.MapGet("/api/monitor/contacts", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var contacts = await db.Contacts
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new
        {
            x.Id,
            x.Nombre,
            x.Apellidos,
            x.Telefono,
            x.Correo,
            x.Empresa,
            x.Codigo,
            x.UsoCodigo,
            x.CreatedAtUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(contacts);
});

app.MapGet("/api/contacts/check-email", async (string correo, AppDbContext db, CancellationToken cancellationToken) =>
{
    var normalizedEmail = correo.Trim().ToLowerInvariant();
    var exists = await db.Contacts.AnyAsync(x => x.Correo == normalizedEmail, cancellationToken);
    return Results.Ok(new { exists });
});

app.MapPost("/api/contacts", async (
    CreateContactRequest request,
    AppDbContext db,
    ContactCodeService codeService,
    CaptchaValidationService captchaService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("CreateContact");

    var email = request.Correo.Trim().ToLowerInvariant();
    var exists = await db.Contacts.AnyAsync(x => x.Correo == email, cancellationToken);
    if (exists)
    {
        return Results.Conflict(new { message = "El correo ya fue registrado." });
    }

    var captchaOk = await captchaService.ValidateAsync(request.CaptchaToken, cancellationToken);
    if (!captchaOk)
    {
        return Results.BadRequest(new { message = "Captcha inválido." });
    }

    var code = await codeService.GenerateUniqueCodeAsync(cancellationToken);

    var contact = new Contact
    {
        Nombre = request.Nombre.Trim(),
        Apellidos = request.Apellidos.Trim(),
        Telefono = request.Telefono.Trim(),
        Correo = email,
        Empresa = string.IsNullOrWhiteSpace(request.Empresa) ? null : request.Empresa.Trim(),
        Codigo = code,
        UsoCodigo = false,
        CreatedAtUtc = DateTime.UtcNow
    };

    db.Contacts.Add(contact);

    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex)
    {
        logger.LogWarning(ex, "Conflicto al guardar contacto.");
        return Results.Conflict(new { message = "No fue posible guardar el contacto por conflicto de datos únicos." });
    }

    return Results.Created($"/api/contacts/{contact.Id}", new
    {
        contact.Id,
        contact.Correo,
        contact.Codigo,
        contact.UsoCodigo
    });
});

app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration configuration, JwtTokenService jwtTokenService) =>
{
    var adminUser = configuration["Admin:Username"] ?? "admin";
    var adminPasswordHash = configuration["Admin:PasswordHash"]
        ?? throw new InvalidOperationException("Falta Admin:PasswordHash");

    var validUser = string.Equals(request.Username, adminUser, StringComparison.Ordinal);
    var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, adminPasswordHash);

    if (!validUser || !validPassword)
    {
        return Results.Unauthorized();
    }

    var token = jwtTokenService.GenerateToken(request.Username);
    return Results.Ok(new { accessToken = token });
});

app.MapGet("/api/contacts", [Authorize] async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var contacts = await db.Contacts
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToListAsync(cancellationToken);

    return Results.Ok(contacts);
});

app.MapPatch("/api/contacts/{id:int}/use-code", [Authorize] async (
    int id,
    UpdateUseCodeRequest request,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var contact = await db.Contacts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (contact is null)
    {
        return Results.NotFound();
    }

    contact.UsoCodigo = request.UsoCodigo;
    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        contact.Id,
        contact.UsoCodigo
    });
});

app.Run();

static string BuildMySqlConnectionString(IConfiguration configuration)
{
    var explicitConnectionString = configuration.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(explicitConnectionString))
    {
        return explicitConnectionString;
    }

    var host = Environment.GetEnvironmentVariable("MYSQLHOST");
    var port = Environment.GetEnvironmentVariable("MYSQLPORT");
    var database = Environment.GetEnvironmentVariable("MYSQLDATABASE")
        ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE");
    var user = Environment.GetEnvironmentVariable("MYSQLUSER");
    var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD")
        ?? Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD");

    if (!string.IsNullOrWhiteSpace(host) &&
        !string.IsNullOrWhiteSpace(port) &&
        !string.IsNullOrWhiteSpace(database) &&
        !string.IsNullOrWhiteSpace(user) &&
        password is not null)
    {
        return $"server={host};port={port};database={database};user={user};password={password};SslMode=None";
    }

    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    if (!string.IsNullOrWhiteSpace(mysqlUrl))
    {
        return ConvertRailwayMysqlUrl(mysqlUrl);
    }

    throw new InvalidOperationException("No se encontró la configuración de conexión a MySQL.");
}

static string ConvertRailwayMysqlUrl(string railwayMysqlUrl)
{
    if (!railwayMysqlUrl.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
    {
        return railwayMysqlUrl;
    }

    var withoutScheme = railwayMysqlUrl["mysql://".Length..];
    var atIndex = withoutScheme.LastIndexOf('@');
    if (atIndex < 0)
    {
        throw new InvalidOperationException("MYSQL_URL inválida: no contiene host.");
    }

    var credentialsPart = withoutScheme[..atIndex];
    var hostPart = withoutScheme[(atIndex + 1)..];

    var colonIndex = credentialsPart.IndexOf(':');
    var user = colonIndex >= 0 ? credentialsPart[..colonIndex] : credentialsPart;
    var password = colonIndex >= 0 ? credentialsPart[(colonIndex + 1)..] : string.Empty;

    var slashIndex = hostPart.IndexOf('/');
    if (slashIndex < 0)
    {
        throw new InvalidOperationException("MYSQL_URL inválida: no contiene base de datos.");
    }

    var hostAndPort = hostPart[..slashIndex];
    var database = hostPart[(slashIndex + 1)..];

    var hostPortSeparator = hostAndPort.LastIndexOf(':');
    var host = hostPortSeparator >= 0 ? hostAndPort[..hostPortSeparator] : hostAndPort;
    var port = hostPortSeparator >= 0 ? hostAndPort[(hostPortSeparator + 1)..] : "3306";

    user = Uri.UnescapeDataString(user);
    password = Uri.UnescapeDataString(password);
    database = Uri.UnescapeDataString(database);

    return $"server={host};port={port};database={database};user={user};password={password};SslMode=None";
}
