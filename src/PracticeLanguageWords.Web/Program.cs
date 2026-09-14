using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Infrastructure;
using PracticeLanguageWords.Infrastructure.Persistence;
using PracticeLanguageWords.Web.Middleware;
using PracticeLanguageWords.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Uygulama & altyapi servisleri (Infrastructure/DependencyInjection.cs) ---
builder.Services.AddPracticeLanguageWords(builder.Configuration);

// --- MVC ---
builder.Services.AddControllersWithViews(options =>
{
    // Tum POST istekleri varsayilan olarak CSRF korumali.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// AJAX istekleri token'i bu header ile gonderir (bkz. wwwroot/js/practice.js).
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRecentWordsTracker, SessionRecentWordsTracker>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".PracticeLanguageWords.Session";
});

// --- Kimlik dogrulama (Cookie tabanli) ---
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".PracticeLanguageWords.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Uc durum: oturum kart calisirken duserse AJAX istegi 302 yerine 401 almali,
        // boylece istemci giris sayfasina temiz sekilde yonlendirebilir.
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/hata");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/hata/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Application katmanindan gelen NotFound/BusinessRule hatalarini uygun HTTP yanitina cevirir.
app.UseMiddleware<ApiExceptionMiddleware>();

app.MapControllers();

// Kok adres kullaniciyi varsayilan dilin ana sayfasina yonlendirir (/en gibi).
app.MapGet("/", async (ILanguageService languageService, CancellationToken ct) =>
{
    var language = await languageService.GetDefaultAsync(ct);
    return Results.Redirect(language is null ? "/hata/404" : $"/{language.Code}");
});

// --- Veritabani migration + baslangic verisi ---
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

app.Run();

static bool IsApiRequest(HttpRequest request) =>
    request.Path.StartsWithSegments("/api")
    || request.Headers["X-Requested-With"] == "XMLHttpRequest"
    || (request.Headers.Accept.Count > 0 && request.Headers.Accept.ToString().Contains("application/json"));
