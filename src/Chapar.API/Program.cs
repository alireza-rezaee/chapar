using EFCoreSecondLevelCacheInterceptor;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Chapar.API.Data;
using Chapar.API.Extensions;
using Chapar.API.Options;
using Chapar.API.Services;
using Chapar.API.Services.Repositories;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddKeyPerFile("/run/secrets", optional: true);

string dbConnectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("ConnectionString 'Database' not found.");

string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionString 'Redis' not found.");

ConfigurationOptions redisOptions = ConfigurationOptions.Parse(redisConnectionString);

RabbitMqOptions rabbitMqOptions = builder.Configuration
    .GetRequiredSection("RabbitMQ")
    .Get<RabbitMqOptions>()
    ?? throw new InvalidOperationException("RabbitMQ Configration Options not found.");

// Add services to the container.
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IMessageDispatcher, MessageDispatcher>();
builder.Services.AddScoped<IOTPDispatcher, OTPDispatcher>();

builder.Services.AddScoped(typeof(ITenantBasedRepository<,>), typeof(TenantBasedRepository<,>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(BasicRepository<,>));
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<OTPRepository>();

builder.Services.AddScoped<ITemplateService, ScribanTemplateService>();

builder.Services.AddScoped<MelipayamakService>();
builder.Services.AddScoped<SmtpEmailService>();

builder.Services.AddScoped<ISMSService>(sp => sp.GetRequiredService<MelipayamakService>());
builder.Services.AddScoped<IMessageChannelService>(sp => sp.GetRequiredService<MelipayamakService>());
builder.Services.AddScoped<IOTPChannelService>(sp => sp.GetRequiredService<MelipayamakService>());

builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<SmtpEmailService>());
builder.Services.AddScoped<IMessageChannelService>(sp => sp.GetRequiredService<SmtpEmailService>());
builder.Services.AddScoped<IOTPChannelService>(sp => sp.GetRequiredService<SmtpEmailService>());

builder.Services.AddEFSecondLevelCache(options =>
    options
        .UseStackExchangeRedisCacheProvider(redisOptions, TimeSpan.FromMinutes(5))
        //.UseMemoryCacheProvider()
        .ConfigureLogging(true)
        .UseCacheKeyPrefix("EF_")
        .UseDbCallsIfCachingProviderIsDown(TimeSpan.FromMinutes(1)));
builder.Services.AddConfiguredMsSqlDbContext(dbConnectionString);

builder.Services.AddControllers();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            rabbitMqOptions.Host,
            rabbitMqOptions.VirtualHost, h =>
            {
                h.Username(rabbitMqOptions.Username);
                h.Password(rabbitMqOptions.Password);
            });

        cfg.ConfigureEndpoints(context);
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
