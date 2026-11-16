using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

static class ValidationEmail
{
    public static ResultatEmail IsEmailValid(string? pEmail)
    {
        if (string.IsNullOrWhiteSpace(pEmail)) { 
            return ResultatEmail.Vide;
             }
        try
        {
            _ = new MailAddress(pEmail);
            return ResultatEmail.Valide;
        }
        catch (FormatException)
        {
            return ResultatEmail.FormatInvalide;
        }
    }
}

