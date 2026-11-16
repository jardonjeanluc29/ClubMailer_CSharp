using System.Text;
using System.Text.Json;
using CsvHelper;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Diagnostics;
using Microsoft.VisualBasic;
using CsvHelper.Configuration;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using ClubMailer.Config;
using MimeKit;
using System.Text.Unicode;
using MailKit;


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

string chemingLogRejet = "email_rejets.log";

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
.DistinctBy(p => p.Email, StringComparer.OrdinalIgnoreCase)
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


SmtpEmailSender facteur = new SmtpEmailSender(ConfigEthereal.configEthereal);
MailboxAddress messageRecipient = new MailboxAddress(Encoding.UTF8, "Pauline", "pauline@gmail.com");
MailboxAddress messageSender = new MailboxAddress(Encoding.UTF8, "Melody", "melody97@ethereal.email");
BodyBuilder bodybuilder = new BodyBuilder();
bodybuilder.TextBody = "Bonjour, je m'appelle Jean-Luc et je vous envoie un email.";
string subject = "urgent";
MimeEntity body = bodybuilder.ToMessageBody();
SendResult response = await facteur.SendEmail(messageRecipient, messageSender, subject, body);
Console.WriteLine($"Réponse du serveur : {response.ResponseText}");

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
