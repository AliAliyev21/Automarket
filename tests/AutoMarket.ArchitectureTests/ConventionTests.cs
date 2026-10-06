using System.Reflection;
using ArchUnitNET.xUnitV3;
using AutoMarket.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static AutoMarket.ArchitectureTests.AutoMarketArchitecture;

namespace AutoMarket.ArchitectureTests;

// ARCHITECTURE §10.4: persistence izolyasiyası, event konvensiyası, mass assignment (SEC-INP-03)
public sealed class ConventionTests
{
    // Request modellərində yazıla bilən sistem sahəsi olmamalıdır (istisnalar açıq siyahı ilə)
    private static readonly string[] SystemProperties = ["Id", "OwnerId", "UserId", "Status", "Role", "Roles", "CreatedAt", "PriceAzn"];

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void DbContexts_Namespace_OnlyInInfrastructure(string module)
    {
        Classes().That().ResideInAssembly(Module(module)).And().AreAssignableTo(typeof(DbContext))
            .Should().ResideInNamespaceMatching(NamespaceTree($"AutoMarket.{module}.Infrastructure"))
            .Because("persistence details belong to the Infrastructure layer")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void IntegrationEvents_Contracts_InEventsNamespaceAndVersioned(string module)
    {
        var eventTypes = Contracts(module).GetTypes()
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var eventType in eventTypes)
        {
            Assert.Equal($"AutoMarket.{module}.Contracts.Events", eventType.Namespace);
            Assert.NotNull(eventType.GetCustomAttribute<IntegrationEventAttribute>());
            Assert.StartsWith($"{module.ToLowerInvariant()}.", IntegrationEventMetadata.Of(eventType).RoutingKey, StringComparison.Ordinal);
        }

        // Events namespace-indəki hər record integration event-dir
        Assert.All(
            Contracts(module).GetTypes()
                .Where(type => type.Namespace == $"AutoMarket.{module}.Contracts.Events" && type is { IsClass: true } && !type.IsNested),
            type => Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(type), $"{type.Name} must implement IIntegrationEvent"));
    }

    [Theory]
    [MemberData(nameof(Modules), MemberType = typeof(AutoMarketArchitecture))]
    public void RequestModels_Properties_NoSystemFields(string module)
    {
        var requests = Module(module).GetTypes().Where(type => type.Name.EndsWith("Request", StringComparison.Ordinal));

        foreach (var request in requests)
        {
            var systemFields = request.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => SystemProperties.Contains(property.Name, StringComparer.Ordinal))
                .Select(property => property.Name)
                .ToList();

            Assert.True(systemFields.Count == 0, $"{request.Name} must not expose system fields (SEC-INP-03): {string.Join(", ", systemFields)}");
        }
    }
}
