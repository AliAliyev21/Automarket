using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AutoMarket.ArchitectureTests.AutoMarketArchitecture;

namespace AutoMarket.ArchitectureTests;

// Contracts modulun sabit açıq kontraktıdır (ADR-0002, ARCHITECTURE §10.4). Contracts hələ boş ola bilər
public sealed class ContractsTests
{
    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void ContractsTypes_DependOnModuleImplementation_ShouldNotDepend(string module)
    {
        var modules = AllModules();

        Types().That().ResideInAssembly(Contracts(module))
            .Should().NotDependOnAnyTypesThat().ResideInAssembly(modules[0], [.. modules[1..], BuildingBlocksWeb])
            .Because("a contract must not depend on any module implementation or on HTTP")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void ContractsTypes_Dependencies_OnlySystemAndBuildingBlocks(string module)
    {
        var allowed =
            $@"^(System|AutoMarket\.{module}\.Contracts|AutoMarket\.BuildingBlocks(?!\.Web(\.|$)))(\..+)?$";

        // OnlyDependOnTypesThat() yüklənməmiş (framework) tipləri yoxlamır, ona görə inkar forması istifadə olunur
        Types().That().ResideInAssembly(Contracts(module))
            .Should().NotDependOnAnyTypesThat().DoNotResideInNamespaceMatching(allowed)
            .Because("contracts may depend only on System and BuildingBlocks primitives")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
