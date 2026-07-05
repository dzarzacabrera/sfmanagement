using System.Net;
using FluentAssertions;

namespace SFManagement.E2ETests;

[Collection("E2E")]
public sealed class PerformanceEvaluationFlowTests(SfManagementE2eFixture fixture)
{
    [Fact]
    public async Task EvaluateTask_AfterStatusFinish_Succeeds()
    {
        await fixture.ResetDatabaseAsync();

        using var httpClient = new HttpClient { BaseAddress = new Uri(fixture.ServerUrl) };

        var assignResult = await httpClient.PostAsync("/Dashboard/AssignWorker",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["taskId"] = "1",
                ["workerId"] = "1"
            }));
        assignResult.StatusCode.Should().Be(HttpStatusCode.OK);

        var toInProgress = await httpClient.PostAsync("/Dashboard/ChangeStatus",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["taskId"] = "1",
                ["newStatus"] = "InProgress"
            }));
        toInProgress.StatusCode.Should().Be(HttpStatusCode.OK);

        var toFinish = await httpClient.PostAsync("/Dashboard/ChangeStatus",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["taskId"] = "1",
                ["newStatus"] = "Finish"
            }));
        toFinish.StatusCode.Should().Be(HttpStatusCode.OK);

        var evalResult = await httpClient.PostAsync("/Dashboard/SubmitEvaluation",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["taskId"] = "1",
                ["skillPositions"] = "0",
                ["basePoints"] = "0.5"
            }));

        evalResult.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
