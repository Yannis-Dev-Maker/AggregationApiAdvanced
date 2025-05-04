using AgileActors.Models;
using AgileActors.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class GenericApiServiceTests
{
    private GenericApiService CreateService(ApiServiceDefinition definition, string jsonResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var loggerMock = new Mock<ILogger<GenericApiService>>();

        var service = new GenericApiService(definition, loggerMock.Object);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(service, httpClient);

        return service;
    }

    [Fact]
    public async Task FetchDataAsync_ReturnsResult_WhenValidResponse()
    {
        var definition = new ApiServiceDefinition
        {
            ServiceName = "test",
            BaseUrl = "https://api.test.com",
            QueryTemplate = "/data?q={keyword}",
            ItemPath = "",
            TitlePath = "title",
            DescriptionPath = "desc"
        };

        var json = "{ \"title\": \"Sample Title\", \"desc\": \"Sample Description\" }";

        var service = CreateService(definition, json);
        var result = await service.FetchDataAsync("query");

        Assert.Single(result.Results);
        Assert.Equal("Sample Title", result.Results[0].Title);
        Assert.Equal("Sample Description", result.Results[0].Description);
    }

    [Fact]
    public async Task FetchDataAsync_ReturnsError_WhenHttpFails()
    {
        var definition = new ApiServiceDefinition
        {
            ServiceName = "test",
            BaseUrl = "https://api.fail.com",
            QueryTemplate = "/fail?q={keyword}"
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<GenericApiService>>();
        var service = new GenericApiService(definition, loggerMock.Object);
        typeof(GenericApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(service, httpClient);

        var result = await service.FetchDataAsync("fail");

        Assert.Single(result.Results);
        Assert.Contains("Error", result.Results[0].Title);
    }

    [Fact]
    public async Task FetchDataAsync_ParsesArray_WhenItemPathIsDefined()
    {
        var definition = new ApiServiceDefinition
        {
            ServiceName = "test",
            BaseUrl = "https://api.test.com",
            QueryTemplate = "/array?q={keyword}",
            ItemPath = "data.items",
            TitlePath = "title",
            DescriptionPath = "desc"
        };

        var json = @"
        {
            ""data"": {
                ""items"": [
                    { ""title"": ""Item 1"", ""desc"": ""Desc 1"" },
                    { ""title"": ""Item 2"", ""desc"": ""Desc 2"" }
                ]
            }
        }";

        var service = CreateService(definition, json);
        var result = await service.FetchDataAsync("demo");

        Assert.Equal(2, result.Results.Count);
        Assert.Equal("Item 1", result.Results[0].Title);
        Assert.Equal("Item 2", result.Results[1].Title);
    }

    [Fact]
    public async Task FetchDataAsync_ExtractsTotalCount_WhenTotalCountPathProvided()
    {
        var definition = new ApiServiceDefinition
        {
            ServiceName = "test",
            BaseUrl = "https://api.test.com",
            QueryTemplate = "/count?q={keyword}",
            ItemPath = "results",
            TitlePath = "title",
            DescriptionPath = "desc",
            TotalCountPath = "meta.total"
        };

        var json = @"
        {
            ""results"": [
                { ""title"": ""T1"", ""desc"": ""D1"" },
                { ""title"": ""T2"", ""desc"": ""D2"" }
            ],
            ""meta"": { ""total"": 42 }
        }";

        var service = CreateService(definition, json);
        var result = await service.FetchDataAsync("count");

        Assert.Equal(2, result.Results.Count);
        Assert.Equal(42, result.TotalCount);
    }
}
