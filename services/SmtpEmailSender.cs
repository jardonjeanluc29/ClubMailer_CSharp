using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using ClubMailer.Config;
using MailKit.Security;
internal class SmtpEmailSender

{

    private readonly SmtpSettings _configSmtpClient;

    public SmtpEmailSender(SmtpSettings psmtpSetting)
    {
        _configSmtpClient = psmtpSetting;
    }
    public async Task<SendResult> SendEmail(InternetAddress precipient, InternetAddress psender, string pSubject, MimeEntity pBody)
    {

        MimeMessage email = new MimeMessage();
        email.From.Add(psender);
        email.To.Add(precipient);
        email.Subject = pSubject;
        email.Body = pBody;


        using var client = new SmtpClient();

        SecureSocketOptions socketOptions = SecureSocketOptions.StartTls;

        await client.ConnectAsync(_configSmtpClient.Host, _configSmtpClient.Port, socketOptions);

        if (!string.IsNullOrWhiteSpace(_configSmtpClient.User) && !string.IsNullOrWhiteSpace(_configSmtpClient.Password))
        {
            await client.AuthenticateAsync(_configSmtpClient.User, _configSmtpClient.Password);
        }
        FormatOptions emailOptions = FormatOptions.Default;

        SendResult resultat = await client.SendAsync(emailOptions, email);

        await client.DisconnectAsync(true);

        return resultat;
    }
}

