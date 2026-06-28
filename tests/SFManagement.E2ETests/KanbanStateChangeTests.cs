using FluentAssertions;
using Microsoft.Playwright;

namespace SFManagement.E2ETests;

[Collection("E2E")]
public sealed class KanbanStateChangeTests(SfManagementE2eFixture fixture)
{
    [Fact]
    public async Task Dashboard_ShowsKanbanColumns()
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(fixture.ServerUrl + "/Dashboard/Index?projectId=1");
        await page.WaitForSelectorAsync(".kanban-column");

        var columns = await page.Locator(".kanban-column").CountAsync();
        columns.Should().Be(4);
    }

    [Fact]
    public async Task ChangeStatus_FromQueuedToInProgress_UpdatesColumn()
    {
        await fixture.ResetDatabaseAsync();

        using var httpClient = new HttpClient { BaseAddress = new Uri(fixture.ServerUrl) };
        var formData = new Dictionary<string, string>
        {
            ["taskId"] = "1",
            ["newStatus"] = "InProgress"
        };
        var response = await httpClient.PostAsync("/Dashboard/ChangeStatus",
            new FormUrlEncodedContent(formData));
        response.EnsureSuccessStatusCode();

        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(fixture.ServerUrl + "/Dashboard/Index?projectId=1");

        var movedCard = page.Locator(".task-card[data-task-id='1']");
        await movedCard.WaitForAsync();
        var status = await movedCard.GetAttributeAsync("data-status");
        status.Should().Be("InProgress");
    }
}
