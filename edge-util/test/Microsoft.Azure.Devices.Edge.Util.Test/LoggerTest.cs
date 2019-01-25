// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Util.Test
{
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Serilog.Events;
    using Xunit;

    [Unit]
    public class LoggerTest
    {
        [Theory]
        [InlineData("", LogEventLevel.Information)]
        [InlineData("info", LogEventLevel.Information)]
        [InlineData("information", LogEventLevel.Information)]
        [InlineData("warning", LogEventLevel.Warning)]
        [InlineData("error", LogEventLevel.Error)]
        [InlineData("debug", LogEventLevel.Debug)]
        public void SetLoggerLevelTest(string logLevel, LogEventLevel targetLogEventLevel)
        {
            if (!string.IsNullOrWhiteSpace(logLevel))
            {
                Logger.SetLogLevel(logLevel);
            }

            LogEventLevel logEventLevel = Logger.GetLogLevel();
            Assert.Equal(logEventLevel, targetLogEventLevel);
        }
    }
}
