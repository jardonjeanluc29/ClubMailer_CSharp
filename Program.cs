using System.Text;
using System.Text.Json;
using System.Globalization;
using CsvHelper.Configuration;
using ClubMailer.Config;
using MimeKit;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;



Console.WriteLine($"Welcome in ClubMailer v0.1, nous sômmes le : {DateTime.Now}");

//string? premiereLigne = fichierStreamReader.ReadLine();
string cheminCsv = "participants.csv";
if (!File.Exists(cheminCsv))
{
    throw new FileNotFoundException($"Le fichier {cheminCsv} est introuvable.");
}

Console.WriteLine("Fichier Existant !");
using var fichierStreamReader = new StreamReader(cheminCsv);

if (fichierStreamReader.Peek() == -1)
{
    Console.WriteLine("Fichier Vide !");
    return;
}
Console.WriteLine("Fichier Non NULL");
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    // Supprime automatiquement les espaces avant et après chaque champ
    TrimOptions = TrimOptions.Trim
};
using var csv = new CsvHelper.CsvReader(fichierStreamReader, config, false);



List<Participant> listeParticipants = csv.GetRecords<Participant>().ToList();
/*foreach (var ligne in listeParticipants)
{
    //Console.WriteLine("Foreach Finale, extraction de la liste crée");
    Console.WriteLine(ligne);
}*/

// Ancienne version sans contrôle 
// List<Participant> listeNoDoublons = listeParticipants.DistinctBy<Participant, string?>(participant => participant.Email).ToList();
/*List<Participant> listeAvecEmailNoDoublons = listeParticipants
.Where<Participant>(participant => !string.IsNullOrWhiteSpace(participant.Email))
.DistinctBy<Participant, string?>(participant => participant.Email, StringComparer.OrdinalIgnoreCase)
.ToList();
/*foreach (var ligne in listeNoDoublons)
{
    Console.WriteLine($"{ligne} inscrit à la course : {ligne.Course}");
}*/




// Je veux lmettre les "sans temps" en dernier : 
Console.WriteLine($"NOMBRE PARTICIPANTS DEPART : {listeParticipants.Count}");



/*List<Participant> listeParticipantsAvecEmail = listeAvecEmailNoDoublons
.Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Email))
.OrderBy(p => p.Temps.HasValue ? 0 : 1)
.ThenBy(p => p.Temps)
.ToList();
Console.WriteLine($"Liste Participants (il y en a : {listeParticipantsAvecEmail.Count}) avec Emails : ");
/*foreach (var ligne in listeParticipantsAvecEmail)
{
    Console.WriteLine($"|--{ligne}--|--COURSE : |--{ligne.Course}--|-- TEMPS : --|-- {ligne.Temps}--|-- {ligne.Email}--|");
}*/
/*List<Participant> listeParticipantsSansEmail = listeParticipants
.Where<Participant>(p => string.IsNullOrWhiteSpace(p.Email))
.OrderBy(p => p.Temps.HasValue ? 0 : 1)
.ThenBy(p => p.Temps)
.ToList();
Console.WriteLine($"Liste Participants (il y en a : {listeParticipantsSansEmail.Count}) sans Emails : ");
/*foreach (var ligne in listeParticipantsSansEmail)
{
    int nbreChamps = ligne.GetType().GetProperties().Count();
    Console.WriteLine($"|--Nbre Champs : {nbreChamps} --|-- {ligne}--|--COURSE : |--{ligne.Course}--|-- TEMPS : --|-- {ligne.Temps}--|-- {ligne.Email}--|");
}*/

// On va utiliser la méthode native de .net pour valider la presence d'email (valide) ou non 

List<Participant> listeAvecEmailValide = listeParticipants
// est devenu facultatif, la methode le fait déjà ensuite 
// .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Email))
.Where<Participant>(p => ValidationEmail.IsEmailValid(p.Email) == ResultatEmail.Valide)
.ToList();

List<Participant> participantsRejetes = listeParticipants
.Where<Participant>(p => ValidationEmail.IsEmailValid(p.Email) != ResultatEmail.Valide)
.ToList();

List<string> logging = [];

string chemingLogRejet = Path.Combine(AppContext.BaseDirectory,"email_rejets.log");

foreach (var ligne in participantsRejetes)
{
    ResultatEmail enumResult = ValidationEmail.IsEmailValid(ligne.Email);
    string result = enumResult switch
    {
        ResultatEmail.FormatInvalide => "Format d'email invalide !",
        ResultatEmail.Vide => "Email Absent !",
        _ => "Cas Inconnu"
    };
    Console.WriteLine($"{ligne.NomComplet} Motif rejet Email : {result}");
    logging.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {enumResult} | {ligne.NomComplet} | {result}");
}
File.AppendAllLines(chemingLogRejet, logging);
/*foreach (var ligne in listeParticipants)
{
    ResultatEmail resultat = ValidationEmail.IsEmailValid(ligne.Email);
    string resultatEmail =  resultat switch 
    {
        ResultatEmail.FormatInvalide    => "Format d'email invalide !",
        ResultatEmail.Valide            => "Email Valide !",
        ResultatEmail.Vide              => "Email Absent !",
        _                               => "Cas Inconnu"
    };
    
            Console.WriteLine($"{ligne.NomComplet}{resultat} {resultatEmail}");
}*/

/*
Console.WriteLine($"Liste Participants avec Methode EMAILVALID (NBRE : {listeAvecEmailValide.Count}) AVEC Emails VALIDER : ");
foreach (var ligne in listeAvecEmailValide)
{
    int nbreChamps = ligne.GetType().GetProperties().Count();
    Console.WriteLine($"|--Nbre Champs : {nbreChamps} --|-- {ligne}--|--COURSE : |--{ligne.Course}--|-- TEMPS : --|-- {ligne.Temps}--|-- {ligne.Email}--|");
}*/

List<List<Participant>> listeParCourse = listeParticipants
.Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Course))
.GroupBy(p => p.Course, StringComparer.OrdinalIgnoreCase)
.Select(gr => gr.ToList())
.ToList();
Console.WriteLine($"\n\n\n\\");
/*foreach (var liste in listeParCourse)
{
    // Je me sert du premier de chaque sous liste pour recuperer le nom de la course (chaque sous liste est un groupe par course)
    Console.WriteLine($"VARIABLE LISTE : {liste[0].Course}");
    foreach (var p in liste)
    {
        Console.WriteLine($"PARTICIPANT LISTE : {p}");
    }
}*/

List<Participant> listeParCourseSimple = listeParticipants
.Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Course))
.OrderBy(p => p.Course, StringComparer.OrdinalIgnoreCase)
.ThenBy(p => p.Nom)
.ToList();
Console.WriteLine($"\n\n\\");

/*foreach (var p in listeParCourseSimple)
{
    Console.WriteLine($"Ma liste Plus Simple {p} et {p.Course} et");
}*/

// GroupBy permet d'avoir .Key (étiquette des groupes) et renvoi des groupes et non des participants
List<IGrouping<string, Participant>> listeGroupBy = listeAvecEmailValide
.Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Course))
.GroupBy(p => p.Course, StringComparer.OrdinalIgnoreCase)
.ToList();
foreach (var p in listeGroupBy)
{
    Console.WriteLine($"KEYYYYYYYYY |  {p.Key}  | NOMBRE : {p.Count()}");
    foreach (var t in p)
    {
        Console.WriteLine($"TTTTTTTTT | {t} |  {t.Nom}");
    }
}

// Si je reveux tous les participant de ma liste de groupe : 
IEnumerable<Participant> tousLesParticipants =
    listeGroupBy.SelectMany<IGrouping<string, Participant>, Participant>(groupe => groupe);


List<Participant> listeEmails = listeGroupBy
.SelectMany(groupe => groupe)
.Where(p => !string.IsNullOrWhiteSpace(p.Email))
.DistinctBy(p => (p.Email!.ToLowerInvariant(), p.Course.ToLowerInvariant()))
.ToList();

foreach (var p in listeEmails)
{
    Console.WriteLine($"EMAIL | {p} | \n | {p.Course}");
}


string cheminListeDiffusion = "liste_diffusion.json";
var optionSerialisation = new JsonSerializerOptions
{
    WriteIndented = true   // propriété de type bool : true = JSON sur plusieurs lignes, indenté
};
string jsonSerialisationListe = JsonSerializer.Serialize(listeEmails, optionSerialisation);
File.WriteAllText(cheminListeDiffusion, jsonSerialisationListe);



IConfiguration configuration = new ConfigurationBuilder()
.SetBasePath(AppContext.BaseDirectory)
.AddJsonFile("appsettings.json", optional: false)// optional: false Rend le fichier obligatoire.
.AddUserSecrets<Program>()
.AddEnvironmentVariables()
.Build();

SmtpSettings? smtpSettings = configuration
.GetSection("Smtp")
.Get<SmtpSettings>() ?? throw new InvalidOperationException("La section Smtp est manquante dans appsettings.json !");


if (smtpSettings is null
    || string.IsNullOrWhiteSpace(smtpSettings.Host)
    || smtpSettings.Port <= 0
    || smtpSettings.SecureSocketOptions == SecureSocketOptions.Auto
    || smtpSettings.SecureSocketOptions == SecureSocketOptions.None
    || smtpSettings.SecureSocketOptions == SecureSocketOptions.StartTlsWhenAvailable)
{
    throw new InvalidOperationException(
        "Configuration SMTP invalide : la section \"Smtp\" doit contenir un Host et un Port valides (appsettings.json) et un mode d'envoi sécurisé.");
}

if (string.IsNullOrWhiteSpace(smtpSettings.User))
{
    throw new Exception($"La section Smtp.User est manquante dans appsettings.json !");
}
if (string.IsNullOrWhiteSpace(smtpSettings.Password))
{
    throw new Exception("La section Smtp.Password est manquante dans appsettings.json !");
}
Console.WriteLine($"Serveur {smtpSettings.Host} sur le PORT : {smtpSettings.Port} et avec la méthode : {smtpSettings.SecureSocketOptions?.ToString() ?? "Non Défini"}.");

SmtpEmailSender facteur = new SmtpEmailSender(smtpSettings);

MailboxAddress messageSender = new MailboxAddress(Encoding.UTF8, "Melody", smtpSettings.User);

foreach (var p in listeEmails)
{
    if (p.Email is null) { Console.WriteLine($"[Erreur]Le mail du participant {p.NomComplet} est absent."); continue; }
    MailboxAddress messageRecipient = new MailboxAddress(Encoding.UTF8, p.NomComplet, p.Email);

    BodyBuilder bodybuilder = new BodyBuilder();
    bodybuilder.TextBody = MailBodyBuilder.BuildText(p);
    bodybuilder.HtmlBody = MailBodyBuilder.BuildHtml(p);
    string subject = "Les Balades Bigoudènes 2027";
    MimeEntity body = bodybuilder.ToMessageBody();
    try
    {
        SendResult response = await facteur.SendEmail(messageRecipient, messageSender, subject, body);
        Console.WriteLine($"Réponse du serveur : {response.ResponseText}");
        if (response.ResponseText.Split(" ").Contains("Accepted"))
        {
            string[] responseParts = response.ResponseText.Split("MSGID=");
            if (responseParts.Count() > 1)
            {
                Console.WriteLine($"https://ethereal.email/message/{responseParts[1].TrimEnd(']')}");
                /** Variante :  string resultat = texte.Length > 0 ? texte[..^1] : texte; **/
            }
        }
        if (response.RejectedRecipients.Count == 0)
        {
            string acceptes = string.Join(", ", response.AcceptedRecipients.Select(destinataire => destinataire.Address));
            Console.WriteLine($"Envoi effectué pour : {acceptes}");
        }
        else
        {
            string refuses = string.Join(", ", response.RejectedRecipients.Select(destinataire => destinataire.Address));
            Console.WriteLine($"Destinataire(s) refusé(s) : {refuses}");
        }
    } catch (AuthenticationException authEx)
    {
        Console.WriteLine($"Identifiants SMTP refusés (User ou Password incorrect(s)) : {authEx.Message}.");
        SendingErrorLog.WriteSendingErrorsLog(authEx, p);
        break;
    } catch (SocketException sockEx)
    {
        Console.WriteLine($"Serveur non accessible : {sockEx.Message}.");
        SendingErrorLog.WriteSendingErrorsLog(sockEx, p);
        break;
    } catch (Exception ex)
    {
        Console.WriteLine($"La procédure d'envoi d'email n'a pas pu aboutir : {ex.Message}.");
        SendingErrorLog.WriteSendingErrorsLog(ex, p);
    }
    finally
    {
        Console.WriteLine($"Fin de la procédure d'envoi d'email pour {p.NomComplet}.");
    }

}



