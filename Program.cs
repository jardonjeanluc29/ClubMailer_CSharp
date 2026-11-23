using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
ILogger logger = loggerFactory.CreateLogger("ClubMailer");
ILogger<LectureCSV> logger2 = loggerFactory.CreateLogger<LectureCSV>();
LectureCSV classLecteur = new LectureCSV(logger2);

logger.LogInformation("Welcome in ClubMailer v0.1, nous sommes le : {Date}", DateTime.Now);



try
{
    // Si on passe la fonction le fichier existe et n'est pas vide
    await classLecteur.ReadCsv();
}
catch  (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
{
    logger.LogError(ex, "[Application]Impossible de lire le fichier CSV.");
    Environment.ExitCode = 1;
}