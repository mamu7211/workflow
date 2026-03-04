using MailKit;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace Workflow.Engine.Activities;

public sealed record SmtpSettings(string Host, int Port);

public sealed class SendEmailActivity : ActivityBase
{
    public override string Type => "SendEmail";

    public override async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var to = context.GetProperty<string>("to");
        var subject = context.GetProperty<string>("subject") ?? string.Empty;
        var body = context.GetProperty<string>("body") ?? string.Empty;
        var isHtml = context.GetProperty<bool>("isHtml");

        if (string.IsNullOrEmpty(to))
            return ActivityResult.Faulted("Property 'to' is required.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("workflow@localhost"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = isHtml
            ? new TextPart("html") { Text = body }
            : new TextPart("plain") { Text = body };

        try
        {
            // Try to get injected transport (for testing / custom SMTP setup)
            var transport = context.ServiceProvider.GetService<IMailTransport>();
            if (transport is not null)
            {
                var messageId = await transport.SendAsync(message, cancellationToken);
                return ActivityResult.Completed(new Dictionary<string, object?> { ["messageId"] = messageId });
            }

            // Default: create SmtpClient with configured settings
            var settings = context.ServiceProvider.GetService<SmtpSettings>();
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(settings?.Host ?? "localhost", settings?.Port ?? 1025, false, cancellationToken);
            var id = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return ActivityResult.Completed(new Dictionary<string, object?> { ["messageId"] = id });
        }
        catch (Exception ex)
        {
            return ActivityResult.Faulted($"Failed to send email: {ex.Message}");
        }
    }
}
