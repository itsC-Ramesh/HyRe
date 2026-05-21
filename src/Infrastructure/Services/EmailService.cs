using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using RC.HyRe.Application.Common.Interfaces;

namespace RC.HyRe.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var emailSection = _configuration.GetSection("Email");
        var host = emailSection["SmtpHost"] ?? "localhost";
        var portStr = emailSection["SmtpPort"] ?? "587";
        var fromAddress = emailSection["FromAddress"] ?? "noreply@hiringtool.local";
        var fromName = emailSection["FromName"] ?? "Hiring Tool";
        var username = emailSection["SmtpUsername"];
        var password = emailSection["SmtpPassword"];

        if (!int.TryParse(portStr, out var port))
        {
            port = 587;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(to, to));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        try
        {
            _logger.LogInformation("Connecting to SMTP server at {Host}:{Port}", host, port);
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls, ct);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password, ct);
            }

            await client.SendAsync(message, ct);
            _logger.LogInformation("Email successfully sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via {Host}:{Port}", to, host, port);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
