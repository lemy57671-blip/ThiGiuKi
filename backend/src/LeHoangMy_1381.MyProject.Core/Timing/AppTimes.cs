using Abp.Dependency;
using System;

namespace LeHoangMy_1381.MyProject.Timing;

public class AppTimes : ISingletonDependency
{
    /// <summary>
    /// Gets the startup time of the application.
    /// </summary>
    public DateTime StartupTime { get; set; }
}
