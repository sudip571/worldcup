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

        //builder.Services.AddDbContext<AppDbContext>(opt =>
        //    opt.UseSqlServer(connStr));

        // Database - FIXED CA1416 warning
        var dbPath = GetDatabasePath();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

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

        // Initialize Database
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureCreated();
            // SeedData(context);
        }

        return app;
    }
    private static string GetDatabasePath()
    {
        string dbPath;
#if ANDROID || IOS || MACCATALYST
        dbPath = Path.Combine(FileSystem.AppDataDirectory, "worldcup.db");
#elif WINDOWS
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var folder = Path.Combine(appDataPath, "worldcup");
    Directory.CreateDirectory(folder);
    dbPath = Path.Combine(folder, "worldcup.db");
#else
    dbPath = "worldcup.db";
#endif

        // ADD THIS LINE TO SEE THE PATH
        System.Diagnostics.Debug.WriteLine($"📁 Database path: {dbPath}");

        return dbPath;
    }
    //private static void SeedData(AppDbContext context)
    //{
    //    if (!context.Users.Any())
    //    {
    //        context.Users.Add(new Models.AppUser
    //        {
    //            Username = "admin",
    //            Password = "Admin123",
    //            CreatedAt = DateTime.Now
    //        });
    //        context.SaveChanges();
    //    }

    //    if (!context.ExpenseCategories.Any())
    //    {
    //        var categories = new[]
    //        {
    //            "Food & Dining", "Transportation", "Shopping", "Entertainment",
    //            "Bills & Utilities", "Healthcare", "Education", "Travel", "Other"
    //        };

    //        foreach (var cat in categories)
    //        {
    //            context.ExpenseCategories.Add(new Models.ExpenseCategory { Name = cat });
    //        }
    //        context.SaveChanges();
    //    }

    //    if (!context.Expenses.Any())
    //    {
    //        var user = context.Users.First();
    //        var categories = context.ExpenseCategories.ToList();
    //        var random = new Random();

    //        for (int i = 0; i < 20; i++)
    //        {
    //            var date = DateTime.Now.AddDays(-random.Next(0, 30));
    //            context.Expenses.Add(new Models.Expense
    //            {
    //                UserId = user.Id,
    //                CategoryId = categories[random.Next(categories.Count)].Id,
    //                Amount = random.Next(10, 500),
    //                Description = $"Sample expense {i + 1}",
    //                Date = date,
    //                IsIncome = random.Next(0, 10) > 7
    //            });
    //        }
    //        context.SaveChanges();
    //    }

    //}
}