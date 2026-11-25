using System.Net;

internal static class MailBodyBuilder
{
    public static string BuildHtml(Participant participant)
    {
        string participantNomComplet = WebUtility.HtmlEncode(participant.NomComplet);
        string participantCourse = WebUtility.HtmlEncode(participant.Epreuve);
        string html = $"""
            <!DOCTYPE html>
                <html lang="fr">
                    <head>
                        <meta charset="UTF-8">
                        <title> Inscription aux Balades Bigoudènes </title>
                    </head>
                    
                    <body>
                        <hr><h1>Bonjour cher <strong>{participantNomComplet}</strong></h1><hr>
                        <p>
                            Ceci est le mail de rappel de votre inscription à l'épreuve' : <strong>{participantCourse}</strong>.
                        </p>
                    </body>
                </html>
            """;
        return html;
    }


    public static string BuildText(Participant participant)
    {
        string text = $"Bonjour cher {participant.NomComplet}.\nCeci est le mail de rappel de votre inscription à la course : {participant.Epreuve}";
        return text;
    }
}