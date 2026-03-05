using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Workflow.Engine.Activities;

public sealed class HttpRequestActivity : ActivityBase
{
    public override string Type => "HttpRequest";

    public override async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var url = context.GetProperty<string>("url");
        var method = context.GetProperty<string>("method") ?? "GET";
        var body = context.GetProperty<string>("body");
        var headers = context.GetProperty<Dictionary<string, string>>("headers");

        if (string.IsNullOrEmpty(url))
            return ActivityResult.Faulted("Property 'url' is required.");

        try
        {
            var factory = context.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient();

            var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            }

            if (!string.IsNullOrEmpty(body))
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            foreach (var header in response.Content.Headers)
                responseHeaders[header.Key] = string.Join(", ", header.Value);

            return ActivityResult.Completed(new Dictionary<string, object?>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["responseBody"] = responseBody,
                ["responseHeaders"] = responseHeaders
            });
        }
        catch (Exception ex)
        {
            return ActivityResult.Faulted($"HTTP request failed: {ex.Message}");
        }
    }
}
