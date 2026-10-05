using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AutoMarket.ArchitectureTests.AutoMarketArchitecture;

namespace AutoMarket.ArchitectureTests;

// Modullar arası sərhəd: proyekt reference-lərinə əlavə ikinci qoruma (ADR-0002, ARCHITECTURE §10.4)
public sealed class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void ModuleTypes_DependOnOtherModule_ShouldNotDepend(string module)
    {
        var others = ModulesExcept(module);

        Types().That().ResideInAssembly(Module(module))
            .Should().NotDependOnAnyTypesThat().ResideInAssembly(others[0], others[1..])
            .Because("a module may use another module only through its Contracts project")
            .Check(Architecture);
    }

    [Fact]
    public void BuildingBlocks_DependOnModules_ShouldNotDepend()
    {
        var modules = AllModulesAndContracts();

        Types().That().ResideInAssembly(BuildingBlocks, BuildingBlocksWeb)
            .Should().NotDependOnAnyTypesThat().ResideInAssembly(modules[0], modules[1..])
            .Because("BuildingBlocks are shared by all modules and must not know any of them")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
