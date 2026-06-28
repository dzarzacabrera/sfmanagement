using FluentAssertions;
using Microsoft.Playwright;

namespace SFManagement.E2ETests;

[Collection("E2E")]
public sealed class WorkerAssignmentFlowTests(SfManagementE2eFixture fixture)
{
    [Fact]
    public async Task AssignPopup_ShowsRecommendedWorkers()
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(fixture.ServerUrl + "/Dashboard/Index?projectId=1");

        var assignBtn = page.Locator(".btn-assign").Nth(0);
        await assignBtn.ClickAsync();

        await page.WaitForSelectorAsync("#assignForm");
        var workerRadios = page.Locator("input[name='workerId']");
        var count = await workerRadios.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AssignWorker_UpdatesDashboard()
    {
        await fixture.ResetDatabaseAsync();

        using var httpClient = new HttpClient { BaseAddress = new Uri(fixture.ServerUrl) };
        var formData = new Dictionary<string, string>
        {
            ["taskId"] = "1",
            ["workerId"] = "1"
        };
        var response = await httpClient.PostAsync("/Dashboard/AssignWorker",
            new FormUrlEncodedContent(formData));
        response.EnsureSuccessStatusCode();

        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(fixture.ServerUrl + "/Dashboard/Index?projectId=1");
        var content = await page.TextContentAsync("body");
        content.Should().Contain("Oriol");
    }
}
