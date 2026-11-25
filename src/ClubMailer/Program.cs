using ClubMailer;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
ILogger logger = loggerFactory.CreateLogger("ClubMailer");
ILogger<LectureCSV> loggerLecture = loggerFactory.CreateLogger<LectureCSV>();
ILogger<LecteurParticipants> loggerLecteurParticipants = loggerFactory.CreateLogger<LecteurParticipants>();

try
{
    LecteurParticipants lecteurCsv = new LecteurParticipants(loggerLecteurParticipants);
    LectureCSV classLecteur = new LectureCSV(loggerLecture,lecteurCsv);
    logger.LogInformation("Welcome in ClubMailer v0.1, nous sommes le : {Date}", DateTime.Now);
    // Si on passe la fonction le fichier existe et n'est pas vide
    await classLecteur.ReadCsv();
}
catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or InvalidOperationException)
{
    logger.LogError(ex, "[Application]Impossible de lire le fichier CSV.");
    Environment.ExitCode = 1;
}