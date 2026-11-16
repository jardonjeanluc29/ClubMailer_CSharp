using ClubMailer.Config;
namespace ClubMailer.Config;

public record ConfigEthereal
{
    public readonly static SmtpSettings configEthereal = new SmtpSettings
    {
        Host = "smtp.ethereal.email",
        Port = 587,
        User = "melody97@ethereal.email",
        Password = "u5ps2nk24fjmu9KYmC"
    };
}