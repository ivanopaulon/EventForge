using EventForge.Components;

var builder = WebApplication.CreateBuilder(args);

// builder.AddCustomSerilogLogging();
builder.Services.AddConfiguredHttpClient(builder.Configuration);
builder.Services.AddConfiguredDbContext(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.Services.EnsureDatabaseMigrated(); // opzionale, per applicare le migrazioni

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
