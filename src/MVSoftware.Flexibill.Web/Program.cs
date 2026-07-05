using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using MVSoftware.Flexibill.Application;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Infrastructure;
using MVSoftware.Flexibill.Infrastructure.Authentication;
using MVSoftware.Flexibill.Infrastructure.Persistence;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;
using MVSoftware.Flexibill.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Zie Technisch Ontwerp, hoofdstuk 3.1 en 10.3: de Web App en de Worker delen
// dezelfde Application- en Infrastructure-registraties.
builder.Services.AddFlexibillApplication();
var sqlConnectionString = builder.Configuration.GetConnectionString("FlexibillDatabase")
    ?? throw new InvalidOperationException("Connection string 'FlexibillDatabase' is not configured.");
builder.Services.AddFlexibillInfrastructure(sqlConnectionString);
builder.Services.AddSingleton<IAuditSourceProvider>(new FixedAuditSourceProvider(AuditSource.Web));

// De Web App is de enige host met een HttpContext (Technisch Ontwerp, hoofdstuk 6.3 punt 2) -
// de Worker registreert zijn eigen ICurrentUserContext (SystemCurrentUserContext).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();

// Wachtwoordloze cookie-authenticatie na een geslaagde OTP-validatie
// (Technisch Ontwerp, hoofdstuk 9.1). Geen "onthoud mij" (FO 4.2): een relatief
// korte sliding expiration in plaats van een persistent cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Alleen in Development: migraties toepassen + demodata seeden (Technisch Ontwerp,
// hoofdstuk 18 - in productie lopen migraties als aparte pipeline-stap, nooit impliciet
// bij het opstarten van de app). Rechtstreeks de connection string meegeven i.p.v. via de
// DI-container op te lossen - dat laatste zou de scoped HttpContextCurrentUserContext
// triggeren, die buiten een HTTP-request geen HttpContext heeft.
if (app.Environment.IsDevelopment())
{
    await DbInitializer.MigrateAndSeedAsync(sqlConnectionString, CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API's onder /api (hoofdstuk 8) - registratie per module volgt in een
// volgende stap, bijv. app.MapExpenseEndpoints();

app.Run();
