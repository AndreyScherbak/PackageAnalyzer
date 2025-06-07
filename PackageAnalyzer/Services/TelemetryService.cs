using PackageAnalyzer.Telemetry;
using PSS.Telemetry;
using Serilog;
using System;

namespace PackageAnalyzer.Services
{
    public class TelemetryService
    {
        private TelemetryManager telemetryManager;

        public TelemetryService()
        {
            try
            {
                // Initialize the telemetry components
                var metadataProvider = new TelemetryMetadataProvider();
                var logger = new TelemetryLogger();
                telemetryManager = new TelemetryManager(metadataProvider, logger);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing TelemetryService");
            }
        }

        public void TrackAppRun()
        {
            telemetryManager?.TrackAppRun();
        }

        public void TrackAppFinish()
        {
            throw new NotImplementedException();
        }
    }
}
