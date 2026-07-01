using ShiftSync.Web.Components;
using ShiftSync.Web.Services;
using ShiftSync.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    
builder.Services.AddScoped<AuthService>();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=shiftsync.db"));

builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<DataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();