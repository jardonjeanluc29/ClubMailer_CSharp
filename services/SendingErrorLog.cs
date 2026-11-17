
internal static class SendingErrorLog
{
    // à décommenter plus tard : private static readonly string cheminLogErreurEnvoi = Path.Combine(AppContext.BaseDirectory,"sending_errors.log");
    private static readonly string cheminLogErreurEnvoi = "sending_errors.log";

    internal static void WriteSendingErrorsLog(Exception ex, Participant p)
    {
        File.AppendAllText(cheminLogErreurEnvoi, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {p.NomComplet} | {ex} |\n");
    }
}