using FluentAssertions;
using Microsoft.Playwright;

namespace SFManagement.E2ETests;

[Collection("E2E")]
public sealed class SkillsCrudTests(SfManagementE2eFixture fixture)
{
    [Fact]
    public async Task CreateSkill_AppearsInIndex()
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(fixture.ServerUrl + "/Skill/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='name']", "E2ETestSkill");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var content = await page.TextContentAsync("body");
        content.Should().Contain("E2ETestSkill");
    }

    [Fact]
    public async Task ToggleActive_DeactivatesAndReactivatesSkill()
    {
        await fixture.ResetDatabaseAsync();

        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(fixture.ServerUrl + "/Skill/Index");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the JavaScript row — exists in seed data (skill id=1, position=0)
        var skillRow = page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "JavaScript" });
        await skillRow.Locator("text=Active").WaitForAsync();

        // Deactivate
        await skillRow.Locator("button", new LocatorLocatorOptions { HasText = "Deactivate" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Re-fetch row since page navigated (POST redirect)
        skillRow = page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "JavaScript" });
        await skillRow.Locator("text=Inactive").WaitForAsync();

        // Reactivate
        await skillRow.Locator("button", new LocatorLocatorOptions { HasText = "Activate" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify Active badge
        skillRow = page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "JavaScript" });
        await skillRow.Locator("text=Active").WaitForAsync();
    }
}
