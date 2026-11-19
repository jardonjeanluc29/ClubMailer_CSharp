internal static class SendingErrorLog
{
    private static readonly string cheminLogErreurEnvoi = "sending_errors.log";

    internal static void WriteSendingErrorsLog(Exception ex, Participant? p = null)
    {
        string qui = p?.NomComplet ?? "(connexion)";
        File.AppendAllText(cheminLogErreurEnvoi, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {qui} | {ex} |\n");
    }
}