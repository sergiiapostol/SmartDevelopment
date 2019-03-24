using System.Collections.Generic;

namespace SmartDevelopment.DependencyTracking.MongoDb
{
    public class ProfilingSettings
    {
        public string DependencyName { get; set; } = "MongoDb";

        public List<string> IgnoredCommands { get; set; } = new List<string> { "isMaster", "buildInfo", "getLastError", "saslStart", "saslContinue" };
    }
}
