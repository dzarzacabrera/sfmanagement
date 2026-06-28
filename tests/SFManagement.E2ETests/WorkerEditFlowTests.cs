using FluentAssertions;
using Microsoft.Playwright;

namespace SFManagement.E2ETests;

[Collection("E2E")]
public sealed class WorkerEditFlowTests(SfManagementE2eFixture fixture)
{
    [Fact]
    public async Task EditWorkerName_UpdatesWorkerList()
    {
        await fixture.ResetDatabaseAsync();

        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        // Navigate to Edit page for worker 1
        await page.GotoAsync(fixture.ServerUrl + "/Worker/Edit?workerId=1");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change the name
        await page.FillAsync("input[name='name']", "E2ETestWorker");

        // Submit — redirects to Worker/Detail
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to Worker index to verify updated name
        await page.GotoAsync(fixture.ServerUrl + "/Worker/Index");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var content = await page.TextContentAsync("body");
        content.Should().Contain("E2ETestWorker");
    }
}
