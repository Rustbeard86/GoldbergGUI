using Microsoft.Extensions.Logging;
using MvvmCross.Platforms.Wpf.Core;
using Serilog;
using Serilog.Extensions.Logging;
using System.IO;

namespace GoldbergGUI.WPF
{
    public class Setup : MvxWpfSetup<Core.App>
    {
        protected override ILoggerFactory CreateLogFactory()
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "goldberg_.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            return new SerilogLoggerFactory(Log.Logger);
        }

        protected override ILoggerProvider CreateLogProvider()
        {
            // Return null since we're using ILoggerFactory
            return null;
        }
    }
}