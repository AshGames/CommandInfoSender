using System.Collections.Generic;
using System.Globalization;
using Expediteur.Infrastructure;
using Expediteur.Web.Background;
using Expediteur.Web.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var hangfireConnectionString = builder.Configuration.GetConnectionString("Commandes")
    ?? throw new InvalidOperationException("La chaîne de connexion 'Commandes' est manquante dans la configuration.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15)
    }));

builder.Services.AddHangfireServer(options => options.WorkerCount = Environment.ProcessorCount);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culture = new CultureInfo("fr-FR");
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = new List<CultureInfo> { culture };
    options.SupportedUICultures = new List<CultureInfo> { culture };
});

builder.Services.AddSingleton<ScheduleInitializer>();
builder.Services.AddTransient<OrderAcknowledgementJob>();

var app = builder.Build();

var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Expéditeur d'accusé de commande v1"));
}
else
{
    app.UseExceptionHandler("/Erreur");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHangfireDashboard("/tableau-hangfire", new DashboardOptions
{
    DashboardTitle = "Expéditeur d'accusé de commande",
    AppPath = "/"
});

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ScheduleInitializer>();
    await initializer.EnsureRecurringJobAsync();
}

await app.RunAsync();
