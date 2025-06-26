using PortProxyAgent.Controllers;
using PortProxyAgent.Models;
using PortProxyAgent.Services;

namespace PortProxyAgent;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure for Windows Service
        builder.Host.UseWindowsService();

        // Configure agent settings
        builder.Services.Configure<AgentConfiguration>(
            builder.Configuration.GetSection(AgentConfiguration.SectionName));

        // Get agent configuration for port setup
        var agentConfig = builder.Configuration.GetSection(AgentConfiguration.SectionName).Get<AgentConfiguration>() 
                         ?? new AgentConfiguration();

        // Configure Kestrel to listen on configured port
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(agentConfig.Port); // HTTP only for internal network
        });

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
        builder.Services.AddSingleton<INetshExecutor, NetshExecutor>();
        builder.Services.AddSingleton<IFailoverService, FailoverService>();
        builder.Services.AddSingleton<IAgentService, AgentService>();
        
        // Add HTTP client for central manager communication
        builder.Services.AddHttpClient<IRegistrationService, RegistrationService>();
        builder.Services.AddHttpClient<IUpdateService, UpdateService>();
        builder.Services.AddHttpClient<IFailoverService, FailoverService>();
        
        // Add background services
        builder.Services.AddHostedService<AgentBackgroundService>();
        builder.Services.AddHostedService<FailoverBackgroundService>();

        // Add logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            if (OperatingSystem.IsWindows())
            {
                logging.AddEventLog(); // Windows Event Log for service
            }
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.MapControllers();

        // Simple health check endpoint
        app.MapGet("/health", () => new
        {
            Status = "PortProxy Agent is running",
            Name = agentConfig.Name,
            Port = agentConfig.Port,
            Timestamp = DateTime.UtcNow
        });

        app.Run();
    }
}