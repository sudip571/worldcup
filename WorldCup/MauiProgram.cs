using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using WorldCup.Data;
using WorldCup.Services;
using Microsoft.EntityFrameworkCore;

namespace WorldCup;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(f => f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // ── Supabase PostgreSQL connection ───────────────────────────
        // Store this in appsettings / secrets — never hard-code in production.
        //const string connStr =
        //    "Host=db.<your-project-ref>.supabase.co;" +
        //    "Port=5432;" +
        //    "Database=postgres;" +
        //    "Username=postgres;" +
        //    "Password=<your-supabase-db-password>;" +
        //    "SSL Mode=Require;Trust Server Certificate=true";

        const string connStr = "Data Source=app.db";

        //builder.Services.AddDbContext<AppDbContext>(opt =>
        //    opt.UseSqlServer(connStr));
        builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(connStr));

        // ── App services ─────────────────────────────────────────────
        builder.Services.AddSingleton<MatchService>();   // singleton: shared polling state
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddScoped<PredictionService>();
        builder.Services.AddScoped<UserSessionService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Auto-migrate on startup (POC only — use proper migrations in production)
        //using var scope = app.Services.CreateScope();
        //scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        return app;
    }
}