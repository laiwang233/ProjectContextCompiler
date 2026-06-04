using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pcc.Application.Generation;
using Pcc.Application.Services;
using Pcc.Infrastructure.Generation;
using Pcc.Infrastructure.Persistence;
using Pcc.Infrastructure.Services;
using Pcc.Infrastructure.Storage;

namespace Pcc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPccInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"] ?? "Postgres";
        if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<PccDbContext>(options => options.UseInMemoryDatabase("pcc-dev"));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Pcc")
                ?? "Host=localhost;Port=5432;Database=pcc;Username=pcc;Password=pcc";
            services.AddDbContext<PccDbContext>(options => options.UseNpgsql(connectionString));
        }
        services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        services.AddScoped<ICompilerWorkflow, CompilerWorkflowService>();
        services.AddScoped<CompilerWorkflowService>();

        var storageRoot = configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRoot));

        services.Configure<LocalCodexCliOptions>(options =>
        {
            options.ExecutablePath = configuration["LocalCodexCli:ExecutablePath"] ?? options.ExecutablePath;
            options.WorkingRoot = configuration["LocalCodexCli:WorkingRoot"] ?? options.WorkingRoot;
            if (int.TryParse(configuration["LocalCodexCli:TimeoutSeconds"], out var timeoutSeconds))
            {
                options.TimeoutSeconds = timeoutSeconds;
            }
        });
        var providerName = configuration["Generation:Provider"] ?? "Mock";
        if (providerName.Equals("LocalCodexCli", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IStructuredGenerationProvider, LocalCodexCliProvider>();
        }
        else
        {
            services.AddScoped<IStructuredGenerationProvider, MockStructuredGenerationProvider>();
        }

        return services;
    }
}
