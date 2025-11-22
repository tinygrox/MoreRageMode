using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace tinygrox.DuckovMods.MoreRageMode.SharedCode;

public class ModLogger
{
    private static readonly Lazy<ModLogger> s_instance = new(() => new ModLogger());
    public static ModLogger Instance => s_instance.Value;

    private ModLogger()
    {
#if DEBUG
        // 在 DEBUG 构建中，默认记录所有日志，包括 Debug 级别
        MinimumLogLevel = Level.Debug;
#else
        // 在非 DEBUG 构建中，默认只记录 Info、Warning、Error 级别日志
        MinimumLogLevel = Level.Info;
#endif
    }

    public enum Level
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public Level MinimumLogLevel { get; set; }

    // public bool IsLoggingEnabled { get; set; }
    public string DefaultModName { get; set; } = "tinygrox.DuckovMods";
    public static LogBuilder Log => new(Instance);

    private void PerformLog(Level level, string message, string modName)
    {
        if (level < MinimumLogLevel)
        {
            return;
        }

        string prefix = $"[{modName ?? DefaultModName}]";

        switch (level)
        {
            case Level.Info:
                Debug.Log($"{prefix} {message}");
                break;
            case Level.Warning:
                Debug.LogWarning($"{prefix} {message}");
                break;
            case Level.Error:
                Debug.LogError($"{prefix} {message}");
                break;
            case Level.Debug:
#if DEBUG
                Debug.Log($"{prefix}[DEBUG] {message}");
#endif
                break;
        }
    }

    public class LogBuilder
    {
        private readonly ModLogger _logger;
        private string _modName;
        private bool _shouldLog = true; // 用于条件日志

        internal LogBuilder(ModLogger logger)
        {
            _logger = logger;
            _modName = logger.DefaultModName; // 继承默认 Mod 名称
        }

        public LogBuilder WithModName(string modName)
        {
            _modName = modName;
            return this;
        }

        public LogBuilder When(bool condition)
        {
            if (!condition)
            {
                _shouldLog = false;
            }

            return this;
        }

        public LogBuilder Info(string message)
        {
            if (_shouldLog)
            {
                _logger.PerformLog(Level.Info, message, _modName);
            }

            return this;
        }

        public LogBuilder Warning(string message)
        {
            if (_shouldLog)
            {
                _logger.PerformLog(Level.Warning, message, _modName);
            }

            return this;
        }

        public LogBuilder Error(string message)
        {
            if (_shouldLog)
            {
                _logger.PerformLog(Level.Error, message, _modName);
            }

            return this;
        }

        public LogBuilder Debug(string message)
        {
            if (_shouldLog)
            {
                _logger.PerformLog(Level.Debug, message, _modName);
            }

            return this;
        }
    }
}

