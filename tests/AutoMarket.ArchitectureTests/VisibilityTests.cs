using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AutoMarket.ArchitectureTests.AutoMarketArchitecture;

namespace AutoMarket.ArchitectureTests;

// Modul proyektində yeganə public tip <M>Module-dur (ARCHITECTURE §3.3, §10.4)
public sealed class VisibilityTests
{
    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void ModuleAssembly_PublicTypes_OnlyModuleClass(string module)
    {
        // Pozitiv nəticə tələb olunur: <M>Module mövcud və public olmalıdır
        Types().That().ResideInAssembly(Module(module)).And().ArePublic()
            .Should().HaveFullName($"AutoMarket.{module}.{module}Module")
            .Because("all module types except the module entry point must be internal")
            .Check(Architecture);
    }
}
