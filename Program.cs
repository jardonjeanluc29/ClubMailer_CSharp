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

await classLecteur.ReadCsv();
