using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AutoMarket.ArchitectureTests.AutoMarketArchitecture;

namespace AutoMarket.ArchitectureTests;

// Modul daxilində qatlar (ARCHITECTURE §2.3, §10.4). Qatlar hələ boş ola bilər, ona görə
// WithoutRequiringPositiveResults: tip əlavə olunan kimi qayda real yoxlamaya keçir
public sealed class LayerTests
{
    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void Domain_Dependencies_ShouldNotDependOnOtherLayersOrFrameworks(string module)
    {
        // Yeganə istisna (ARCHITECTURE §10.4, ADR-0003): Identity.Domain-də User/Role Identity Core-un EF store-ları üçün
        // IdentityUser/IdentityRole-dan törəyir. Microsoft.AspNetCore.Identity EF Core-dan və HTTP-dən asılı deyil
        var aspNetCore = module == "Identity"
            ? @"Microsoft\.AspNetCore(?!\.Identity(\.|$))"
            : @"Microsoft\.AspNetCore";
        var forbidden =
            $@"^(AutoMarket\.{module}\.(Application|Infrastructure|Api)|Microsoft\.EntityFrameworkCore|{aspNetCore}|RabbitMQ)(\..+)?$";

        Types().That().ResideInNamespaceMatching(NamespaceTree($"AutoMarket.{module}.Domain"))
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(forbidden)
            .Because("domain rules must not depend on other layers or frameworks")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void Application_Dependencies_ShouldNotDependOnInfrastructureOrApi(string module)
    {
        var forbidden = $@"^AutoMarket\.{module}\.(Infrastructure|Api)(\..+)?$";

        Types().That().ResideInNamespaceMatching(NamespaceTree($"AutoMarket.{module}.Application"))
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(forbidden)
            .Because("use cases must not depend on transport or technology details")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void Api_Dependencies_ShouldNotDependOnInfrastructure(string module)
    {
        Types().That().ResideInNamespaceMatching(NamespaceTree($"AutoMarket.{module}.Api"))
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(NamespaceTree($"AutoMarket.{module}.Infrastructure"))
            .Because("endpoints call application handlers, not infrastructure")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
