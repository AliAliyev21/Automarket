using System.Reflection;
using ArchUnitNET.Loader;
using AutoMarket.Catalog;
using AutoMarket.Engagement;
using AutoMarket.Identity;
using AutoMarket.Listings;
using AutoMarket.Messaging;
using AutoMarket.Moderation;
using AutoMarket.Notifications;
using AutoMarket.Search;

namespace AutoMarket.ArchitectureTests;

// Bütün src assembly-ləri bir dəfə yüklənir; testlər eyni Architecture obyektini istifadə edir
internal static class AutoMarketArchitecture
{
    private static readonly Dictionary<string, Assembly> ModuleAssemblies = new(StringComparer.Ordinal)
    {
        ["Identity"] = typeof(IdentityModule).Assembly,
        ["Catalog"] = typeof(CatalogModule).Assembly,
        ["Listings"] = typeof(ListingsModule).Assembly,
        ["Search"] = typeof(SearchModule).Assembly,
        ["Moderation"] = typeof(ModerationModule).Assembly,
        ["Engagement"] = typeof(EngagementModule).Assembly,
        ["Messaging"] = typeof(MessagingModule).Assembly,
        ["Notifications"] = typeof(NotificationsModule).Assembly,
    };

    // Contracts proyektlərinin hələ tipi ola bilməz (ARCHITECTURE §2.1), ona görə ad ilə yüklənir
    private static readonly Dictionary<string, Assembly> ContractsAssemblies = ModuleAssemblies.Keys
        .ToDictionary(name => name, name => Assembly.Load($"AutoMarket.{name}.Contracts"), StringComparer.Ordinal);

    public static IReadOnlyCollection<string> ModuleNames => ModuleAssemblies.Keys;

    public static Assembly BuildingBlocks { get; } = Assembly.Load("AutoMarket.BuildingBlocks");

    public static Assembly BuildingBlocksWeb { get; } = Assembly.Load("AutoMarket.BuildingBlocks.Web");

    public static ArchUnitNET.Domain.Architecture Architecture { get; } = new ArchLoader()
        .LoadAssemblies([BuildingBlocks, BuildingBlocksWeb, .. ModuleAssemblies.Values, .. ContractsAssemblies.Values])
        .Build();

    public static TheoryData<string> Modules => [.. ModuleNames];

    public static Assembly Module(string name) => ModuleAssemblies[name];

    public static Assembly Contracts(string name) => ContractsAssemblies[name];

    public static Assembly[] ModulesExcept(string name) =>
        [.. ModuleAssemblies.Where(pair => pair.Key != name).Select(pair => pair.Value)];

    public static Assembly[] AllModules() => [.. ModuleAssemblies.Values];

    public static Assembly[] AllModulesAndContracts() =>
        [.. ModuleAssemblies.Values, .. ContractsAssemblies.Values];

    // Namespace və onun alt namespace-ləri üçün regex
    public static string NamespaceTree(string ns) => $@"^{ns.Replace(".", @"\.", StringComparison.Ordinal)}(\..+)?$";
}
