using System.Net;
using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class HttpRequestActivityTests
{
    [TestMethod]
    public async Task HttpRequest_GetRequest_ReturnsStatusCodeAndBody()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\":\"ok\"}")
            }
        };

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["url"] = "https://api.example.com/data",
                ["method"] = "GET"
            },
            handler);

        var activity = new HttpRequestActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(200, result.Output!["statusCode"]);
        Assert.AreEqual("{\"result\":\"ok\"}", result.Output["responseBody"]);
        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
    }

    [TestMethod]
    public async Task HttpRequest_PostWithBody_SendsBody()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"id\":1}")
            }
        };

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["url"] = "https://api.example.com/items",
                ["method"] = "POST",
                ["body"] = "{\"name\":\"test\"}"
            },
            handler);

        var activity = new HttpRequestActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(201, result.Output!["statusCode"]);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);

        var requestBody = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.AreEqual("{\"name\":\"test\"}", requestBody);
    }

    [TestMethod]
    public async Task HttpRequest_WithHeaders_SetsHeaders()
    {
        var handler = new TestHttpMessageHandler();

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["url"] = "https://api.example.com/data",
                ["method"] = "GET",
                ["headers"] = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer token123",
                    ["X-Custom"] = "custom-value"
                }
            },
            handler);

        var activity = new HttpRequestActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.IsTrue(handler.LastRequest!.Headers.Contains("Authorization"));
        Assert.AreEqual("Bearer token123", handler.LastRequest.Headers.GetValues("Authorization").First());
    }

    [TestMethod]
    public async Task HttpRequest_ResponseHeaders_IncludedInOutput()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("body")
        };
        response.Headers.Add("X-Response-Header", "response-value");

        var handler = new TestHttpMessageHandler { Response = response };

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["url"] = "https://api.example.com/data",
                ["method"] = "GET"
            },
            handler);

        var activity = new HttpRequestActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        var responseHeaders = (Dictionary<string, string>)result.Output!["responseHeaders"]!;
        Assert.AreEqual("response-value", responseHeaders["X-Response-Header"]);
    }

    [TestMethod]
    public async Task HttpRequest_MissingUrl_ReturnsFaulted()
    {
        var context = CreateContext(
            new Dictionary<string, object?> { ["method"] = "GET" },
            new TestHttpMessageHandler());

        var activity = new HttpRequestActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("'url'"));
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, TestHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        return new ActivityContext(
            "test-httprequest",
            properties,
            new Dictionary<string, object?>(),
            new TestServiceProvider(new TestHttpClientFactory(client)));
    }

    private sealed class TestServiceProvider(IHttpClientFactory factory) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IHttpClientFactory)) return factory;
            return null;
        }
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }
}
