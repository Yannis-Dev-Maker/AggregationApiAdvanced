using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AgileActors.Controllers;
using AgileActors.Models;
using AgileActors.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AgileActors.Tests.Controllers
{
    public class AggregationControllerTests
    {
        [Fact]
        public async Task GetAggregatedData_WithMockedServices_ReturnsExpectedResults()
        {
            // Arrange: Mock News service
            var mockNews = new Mock<IAggregatorService>();
            mockNews.Setup(x => x.Name).Returns("news");
            mockNews.Setup(x => x.FetchDataAsync("Athens", 5, 1, "date", It.IsAny<DateTime?>()))
                .ReturnsAsync(new AggregatedResponse
                {
                    Results = new List<AggregatedResult>
                    {
                        new AggregatedResult
                        {
                            Source = "News",
                            Title = "Mock News Title",
                            Description = "Mock Description",
                            Date = DateTime.Today,
                            Url = "https://example.com/news"
                        }
                    },
                    TotalCount = 1
                });

            // Arrange: Mock Weather service
            var mockWeather = new Mock<IAggregatorService>();
            mockWeather.Setup(x => x.Name).Returns("weather");
            mockWeather.Setup(x => x.FetchDataAsync("Athens", 5, 1, "date", It.IsAny<DateTime?>()))
                .ReturnsAsync(new AggregatedResponse
                {
                    Results = new List<AggregatedResult>
                    {
                        new AggregatedResult
                        {
                            Source = "Weather",
                            Title = "Sunny Day",
                            Description = "25°C and clear",
                            Date = DateTime.Today,
                            Url = "https://example.com/weather"
                        }
                    },
                    TotalCount = 1
                });

            var services = new List<IAggregatorService> { mockNews.Object, mockWeather.Object };
            var controller = new AggregationController(services);

            // Act
            var result = await controller.GetAggregatedData(
                keyword: "Athens",
                count: 5,
                page: 1,
                sortBy: "date",
                fromDate: "2025-05-01",  // older date to avoid live data dependency
                services: "news,weather");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Dictionary<string, AggregatedResponse>>(okResult.Value);

            Assert.True(response.ContainsKey("news"));
            Assert.True(response.ContainsKey("weather"));
            Assert.Single(response["news"].Results);
            Assert.Single(response["weather"].Results);
        }
    }
}
