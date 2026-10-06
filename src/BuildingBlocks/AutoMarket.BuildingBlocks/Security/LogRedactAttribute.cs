namespace AutoMarket.BuildingBlocks.Security;

// SEC-LOG-01: şifrə, token, telefon və ya mesaj mətni olan tiplər işarələnir; destructuring zamanı *** ilə əvəz olunur (ARCHITECTURE §8.3)
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, Inherited = false)]
public sealed class LogRedactAttribute : Attribute;
