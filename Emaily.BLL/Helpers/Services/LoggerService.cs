using Emaily.BLL.DTOs;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Services
{
    public class LogQueue
    {
        // قناة (Channel) تتسع لـ 10,000 خطأ في الذاكرة لتجنب أي ضغط
        private readonly Channel<SystemLog> _queue;

        public LogQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropOldest // لو السيرفر انهار تماماً، يتجاهل الأقدم
            };
            _queue = Channel.CreateBounded<SystemLog>(options);
        }

        public void Enqueue(SystemLog log) => _queue.Writer.TryWrite(log);

        public IAsyncEnumerable<SystemLog> ReadAllAsync(CancellationToken ct) => _queue.Reader.ReadAllAsync(ct);

        public bool TryRead(out SystemLog? log) => _queue.Reader.TryRead(out log);
    }

    public class LoggerService(
        IHttpContextAccessor httpContextAccessor,
        LogQueue logQueue, // حقننا الطابور هنا
        ILogger<LoggerService> logger) : ILoggerService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LogQueue _logQueue = logQueue;
        private readonly ILogger<LoggerService> _logger = logger;

        public void LogDebug(Log dto, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)

           => ProcessLog(dto, SystemLog.LogLevels.Debug, callerName, filePath, lineNumber);

        public void LogInfo(Log dto, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)

            => ProcessLog(dto, SystemLog.LogLevels.Info, callerName, filePath, lineNumber);

        public void LogWarning(Log dto, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)

            => ProcessLog(dto, SystemLog.LogLevels.Warning, callerName, filePath, lineNumber);

        public void LogError(Log dto, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)

            => ProcessLog(dto, SystemLog.LogLevels.Error, callerName, filePath, lineNumber);

        public void LogCritical(Log dto, [CallerMemberName] string callerName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)

            => ProcessLog(dto, SystemLog.LogLevels.Critical, callerName, filePath, lineNumber);

        private void ProcessLog(Log dto, string level, string callerName, string filePath, int lineNumber)
        {
            var context = _httpContextAccessor.HttpContext;
            var traceId = context?.TraceIdentifier ?? "No-Trace-Id";
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Internal-System";

            var fileName = Path.GetFileName(filePath);
            string fullTrace = $"[TraceId: {traceId}] [Caller: {callerName} in {fileName}:{lineNumber}]";

            _logger.Log(GetLogLevel(level), "[IP: {Ip}] {Trace} => {Message}", ipAddress, fullTrace, dto.Message);

            var logEntry = new SystemLog
            {
                Message = dto.Message[..Math.Min(dto.Message?.Length ?? 0, 500)],
                UserId = dto.UserId,
                ExecutionTrace = fullTrace,
                ProjectId = dto.ProjectId,
                ExceptionDetails = dto.ExceptionDetails?[..Math.Min(dto.ExceptionDetails?.Length ?? 0, 4000)],
                IpAddress = ipAddress,
                LogLevel = level,
                CreatedAt = DateTime.UtcNow
            };

            _logQueue.Enqueue(logEntry);
        }

        private static LogLevel GetLogLevel(string level) => level switch
        {
            SystemLog.LogLevels.Debug => LogLevel.Debug,
            SystemLog.LogLevels.Info => LogLevel.Information,
            SystemLog.LogLevels.Warning => LogLevel.Warning,
            SystemLog.LogLevels.Error => LogLevel.Error,
            SystemLog.LogLevels.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    public class LogDatabaseProcessor(LogQueue logQueue, IServiceScopeFactory scopeFactory, ILogger<LogDatabaseProcessor> logger) : BackgroundService
    {
        private readonly LogQueue _logQueue = logQueue;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<LogDatabaseProcessor> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var log in _logQueue.ReadAllAsync(stoppingToken))
                {
                    var batch = new List<SystemLog> { log };

                    while (batch.Count < 100 && _logQueue.TryRead(out var nextLog) && nextLog != null)
                    {
                        batch.Add(nextLog);
                    }

                    await SaveBatchAsync(batch);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogCritical("Server shutting down... Flushing remaining logs to database.");
                await FlushRemainingLogsAsync();
            }
        }

        private async Task SaveBatchAsync(List<SystemLog> logs)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await uow.SystemLogs.AddRangeAsync(logs);
                await uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "CRITICAL: Failed to save batch of {Count} logs to SQL Database.", logs.Count);
            }
        }

        private async Task FlushRemainingLogsAsync()
        {
            var finalBatch = new List<SystemLog>();
            while (_logQueue.TryRead(out var log) && log != null)
            {
                {
                    finalBatch.Add(log);
                }

                if (finalBatch.Count != 0)
                {
                    await SaveBatchAsync(finalBatch);
                }
            }
        }
    }
}