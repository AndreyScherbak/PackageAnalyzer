using System;
using Serilog;

namespace PackageAnalyzer.Telemetry
{
    internal class TelemetryLogger : PSS.Telemetry.ILogger
    { 
        public void Info(string message)
        {
            Log.Information(message);
        }

        public void Error(Exception ex, string message)
        {
            Log.Error(message, ex);
        }

        public void Warn(Exception ex, string message)
        {
            Log.Warning(message, ex);
        }

        public void Warn(string message)
        {
            Log.Warning(message);
        }
    }
}
