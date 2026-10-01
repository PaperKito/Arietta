using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Arietta.Features.Sync;
using Arietta.Features.PlayTime;
using Arietta.Helpers;
using System;

namespace Arietta;






public static partial class AppConfig
{

    private static readonly object _serviceGate = new();

    private static ILoggerFactory? _loggerFactory;
    private static PlayTimeStatsService? _playTimeStatsService;
    private static CloudSyncService? _cloudSyncService;


    
    public static T GetService<T>() where T : notnull
        => typeof(T) == typeof(PlayTimeStatsService) ? (T)(object)PlayTimeStatsServiceInstance
         : typeof(T) == typeof(CloudSyncService) ? (T)(object)CloudSyncServiceInstance
         : throw new InvalidOperationException($"No service for type '{typeof(T)}' has been registered.");


    
    public static ILogger<T> GetLogger<T>()
    {
        try
        {
            return LogFactory.CreateLogger<T>();
        }
        catch
        {
            return NullLogger<T>.Instance;
        }
    }


    
    public static Microsoft.Extensions.Logging.ILogger GetLogger(string category)
    {
        try
        {
            return LogFactory.CreateLogger(category);
        }
        catch
        {
            return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }
    }


    
    private static ILoggerFactory LogFactory
    {
        get
        {
            if (_loggerFactory is null)
            {
                lock (_serviceGate)
                {
                    _loggerFactory ??= LoggerFactory.Create(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddProvider(new FileLoggerProvider(LogFolder));
                    });
                }
            }
            return _loggerFactory;
        }
    }


    
    internal static PlayTimeStatsService PlayTimeStatsServiceInstance
    {
        get
        {
            if (_playTimeStatsService is null)
            {
                lock (_serviceGate)
                {
                    _playTimeStatsService ??= new PlayTimeStatsService(GetLogger<PlayTimeStatsService>());
                }
            }
            return _playTimeStatsService;
        }
    }


    
    internal static CloudSyncService CloudSyncServiceInstance
    {
        get
        {
            if (_cloudSyncService is null)
            {
                lock (_serviceGate)
                {
                    _cloudSyncService ??= new CloudSyncService(GetLogger<CloudSyncService>());
                }
            }
            return _cloudSyncService;
        }
    }

}
