using NetStatInfoWin.Services;

namespace NetStatInfoWin.Tests.Services;

[TestClass]
public sealed class AppUsageDisplayNameResolverTests
{
    [TestMethod]
    public void ResolveDisplayName_UsesAttributedName_WhenPresent()
    {
        var resolver = new AppUsageDisplayNameResolver(
            appInfoLookup: _ => "Resolved app",
            packageLookup: _ => "Resolved package");

        string displayName = resolver.ResolveDisplayName("Contoso.App_123!App", "Browser");

        Assert.AreEqual("Browser", displayName);
    }

    [TestMethod]
    public void ResolveDisplayName_UsesAppInfoLookup_WhenAttributedNameMissing()
    {
        var resolver = new AppUsageDisplayNameResolver(
            appInfoLookup: id => id == "Contoso.App_123!App" ? "Contoso Browser" : null,
            packageLookup: _ => null);

        string displayName = resolver.ResolveDisplayName("Contoso.App_123!App", string.Empty);

        Assert.AreEqual("Contoso Browser", displayName);
    }

    [TestMethod]
    public void ResolveDisplayName_UsesPackageFamilyFallback_ForAppUserModelIds()
    {
        var resolver = new AppUsageDisplayNameResolver(
            appInfoLookup: _ => null,
            packageLookup: id => id == "Contoso.App_123" ? "Contoso Browser" : null);

        string displayName = resolver.ResolveDisplayName("Contoso.App_123!App", null);

        Assert.AreEqual("Contoso Browser", displayName);
    }

    [TestMethod]
    public void ResolveDisplayName_UsesReadableIdentifierFallback_WhenLookupsFail()
    {
        var resolver = new AppUsageDisplayNameResolver(
            appInfoLookup: _ => null,
            packageLookup: _ => null);

        string displayName = resolver.ResolveDisplayName("Contoso.App_123!App", null);

        Assert.AreEqual("Contoso App", displayName);
    }
}
