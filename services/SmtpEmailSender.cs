using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using ClubMailer.Config;
using MailKit.Security;
internal class SmtpEmailSender : IDisposable

{

    private readonly SmtpSettings _configSmtpClient;
    private readonly SmtpClient _client = new SmtpClient();

    public SmtpEmailSender(SmtpSettings psmtpSetting)
    {
        _configSmtpClient = psmtpSetting;
    }
    private async Task AuthenticateConnectionAsync()
    {
        if (!string.IsNullOrWhiteSpace(_configSmtpClient.User) && !string.IsNullOrWhiteSpace(_configSmtpClient.Password))
        {
            await _client.AuthenticateAsync(_configSmtpClient.User, _configSmtpClient.Password);
        }
    }
    public async Task ConnexionAsync()
    {
        SecureSocketOptions socketOptions = _configSmtpClient.SecureSocketOptions ?? SecureSocketOptions.StartTls;
        await _client.ConnectAsync(_configSmtpClient.Host, _configSmtpClient.Port, socketOptions);
        await AuthenticateConnectionAsync();
    }

    public bool IsConnected()
    {
        return _client.IsConnected;
    }

    public async Task CloseConnectionAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(true);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task<SendResult> SendEmail(InternetAddress precipient, InternetAddress psender, string pSubject, MimeEntity pBody)
    {

        MimeMessage email = new MimeMessage();
        email.From.Add(psender);
        email.To.Add(precipient);
        email.Subject = pSubject;
        email.Body = pBody;


        FormatOptions emailOptions = FormatOptions.Default;

        SendResult resultat = await _client.SendAsync(emailOptions, email);

        return resultat;
    }
}

