using System.Text;
using System.Globalization;
using CsvHelper.Configuration;
using System.Text.Json;
using ClubMailer.Config;
using MimeKit;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using CsvHelper;
using ClubMailer;

public class LectureCSV
{

    //Version Longue UTF-8 private static readonly string _cheminCsv = "participant_pointVirgule.csv";
    // Version Allegée UTF-8 private static readonly string _cheminCsv = "participantsLight.csv";
    // Version Tabulation 1252 private static readonly string _cheminCsv = "participant_tabulation.csv";
    // Version Allegée UTF-8 Test Colonne Manquante private static readonly string _cheminCsv = "testColonneManquante.csv";
    // Version Allegée UTF-8 Test
    private static readonly string _cheminCsv = "format_a_valide.csv";

    // TODO: A Changer Plus tard et à mettre dans le constructeur je pense.
    private readonly string nomFichier = _cheminCsv; // 
    private readonly EnvoiSettings envoiSetting;
    private static bool envoiPossible = false;
    private static bool envoiReelPossible = false;
    // Partie Connexion et Reconnexion 
    const int nbreEssais = 3;
    const int delay = 6000;
    const int nbreEnvoiMax = 2;
    const int delayEnvoiDeco = 6000;


    // Partie Constructeur
    private readonly SmtpSettings smtpSettings;
    private readonly SmtpEmailSender facteur;

    private readonly ILogger<LectureCSV> _logger;
    private readonly LecteurParticipants _lecteur;
    // Constructeur d'instance 
    internal LectureCSV(ILogger<LectureCSV> pIlogger, LecteurParticipants pLecteur)
    {
        this._logger = pIlogger;
        this._lecteur = pLecteur;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)// optional: false Rend le fichier obligatoire.
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();
        this.envoiSetting = configuration
            .GetSection("Envoi")
            .Get<EnvoiSettings>() ?? throw new InvalidOperationException("[Configuration]La section ModeEnvoi est manquante dans appsettings.json !");
        this.smtpSettings = configuration
            .GetSection("Smtp")
            .Get<SmtpSettings>() ?? throw new InvalidOperationException("[Configuration]La section Smtp est manquante dans appsettings.json !");
        this.facteur = new SmtpEmailSender(smtpSettings);
    }

    public async Task ReadCsv()
    {
        ResultatLecture resultat = _lecteur.Lire(_cheminCsv);
        // Variables pour le Rapport de Journalisation
        int nbreHeaders = CompterHeaders(resultat.ListeHeadersCompleteDuFichier);
        int nbreHeadersManquants = resultat.ListeHeadersOptionalDuFichierManquant.Count;
        int lignesInscriptions = resultat.ListeParticipantSansDoublonHeaders.Count;
        // Rapport de Journalisation sur le Fichier
        _logger.LogInformation(
    """
    -------------------------------------------------------------------------------------------
    [Rapport Fichier CSV] Résumé de l'import du fichier : {NomFichier}
    [Encodage]Encodage détecté : {Encodage}
    [Séparateur]Séparateur détecté : {Separateur}
    [Taille]Nombre de lignes du fichier : {NbreLignes}
    [Taille]Nombre de lignes retirées du fichier (Répétition Headers) : {NbreLignesRetirées}
    [Taille]Nombre d'inscriptions extraite du fichier : {NombreParticipants}
    [Colonnes]Nombre de Colonnes dans le Csv : {Headers}
    [Colonnes]Nombre de Headers Optionnels Manquant : {NbreHeadersManquants}
    -------------------------------------------------------------------------------------------
    """,
    nomFichier,
    resultat.Encodage,
    resultat.Delimiteur == "\t" ? "tabulation" : resultat.Delimiteur,
    resultat.LignesTotalFichierBrut,
    resultat.NombreLignesRetirees,
    resultat.ListeParticipantSansDoublonHeaders.Count,
    nbreHeaders,
    nbreHeadersManquants
    );

        // On va utiliser la méthode native de .net pour valider la presence d'email (valide) ou non 

        List<Participant> listeAvecEmailValide = resultat.ListeParticipantSansDoublonHeaders
        // est devenu facultatif, la methode le fait déjà ensuite 
        // .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Email))
        .Where<Participant>(p => ValidationEmail.IsEmailValid(p.Email) == ResultatEmail.Valide)
        .ToList();

        List<Participant> participantsRejetes = resultat.ListeParticipantSansDoublonHeaders
        .Where<Participant>(p => ValidationEmail.IsEmailValid(p.Email) != ResultatEmail.Valide)
        .ToList();

        List<string> logging = [];

        // A décommenter plus tard  string chemingLogRejet = Path.Combine(AppContext.BaseDirectory, "email_invalide.log");
        string chemingLogRejet = "email_invalide.log";

        foreach (var ligne in participantsRejetes)
        {
            ResultatEmail enumResult = ValidationEmail.IsEmailValid(ligne.Email);
            string result = enumResult switch
            {
                ResultatEmail.FormatInvalide => "Format d'email invalide !",
                ResultatEmail.Vide => "Email Absent !",
                _ => "Cas Inconnu"
            };
            _logger.LogInformation("{NomComplet} Motif rejet Email : {Resultat}", ligne.NomComplet, result);
            logging.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {enumResult} | {ligne.NomComplet} | {result}");
        }
        File.AppendAllLines(chemingLogRejet, logging);


        List<List<Participant>> listeParCourse = resultat.ListeParticipantSansDoublonHeaders
        .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Epreuve))
        .GroupBy(p => p.Epreuve, StringComparer.OrdinalIgnoreCase)
        .Select(gr => gr.ToList())
        .ToList();


        List<Participant> listeParCourseSimple = resultat.ListeParticipantSansDoublonHeaders
        .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Epreuve))
        .OrderBy(p => p.Epreuve, StringComparer.OrdinalIgnoreCase)
        .ThenBy(p => p.Nom)
        .ToList();


        // GroupBy permet d'avoir .Key (étiquette des groupes) et renvoi des groupes et non des participants
        List<IGrouping<string, Participant>> listeGroupBy = listeAvecEmailValide
        .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Epreuve))
        .GroupBy(p => p.Epreuve, StringComparer.OrdinalIgnoreCase)
        .ToList();


        // Si je reveux tous les participant de ma liste de groupe : 
        IEnumerable<Participant> tousLesParticipants =
            listeGroupBy.SelectMany<IGrouping<string, Participant>, Participant>(groupe => groupe);


        List<Participant> listeEmails = listeGroupBy
        .SelectMany(groupe => groupe)
        .Where(p => !string.IsNullOrWhiteSpace(p.Email))
        .DistinctBy(p => (p.Email!.ToLowerInvariant(), p.Epreuve.ToLowerInvariant()))
        .ToList();


        string cheminListeDiffusion = "liste_diffusion.json";
        var optionSerialisation = new JsonSerializerOptions
        {
            WriteIndented = true   // propriété de type bool : true = JSON sur plusieurs lignes, indenté
        };
        string jsonSerialisationListe = JsonSerializer.Serialize(listeEmails, optionSerialisation);
        File.WriteAllText(cheminListeDiffusion, jsonSerialisationListe);


        // Début de la partie Configuration des paramètres Smtp et Mode Envoi (appsettings.json et variables d'environnement)
        //
        // SMTP SETTING
        if (smtpSettings is null
            || string.IsNullOrWhiteSpace(smtpSettings.Host)
            || smtpSettings.Port <= 0
            || string.IsNullOrWhiteSpace(smtpSettings.Sender)
            || smtpSettings.SecureSocketOptions == SecureSocketOptions.Auto
            || smtpSettings.SecureSocketOptions == SecureSocketOptions.None
            || smtpSettings.SecureSocketOptions == SecureSocketOptions.StartTlsWhenAvailable)
        { throw new InvalidOperationException("Configuration SMTP invalide : la section \"Smtp\" doit contenir un Host / Port / Sender valide(s) (appsettings.json) et un mode d'envoi sécurisé."); }
        if (string.IsNullOrWhiteSpace(smtpSettings.User)) { throw new Exception("La section Smtp.User est manquante dans appsettings.json !"); }
        if (string.IsNullOrWhiteSpace(smtpSettings.Password)) { throw new Exception("La section Smtp.Password est manquante dans appsettings.json !"); }
        // FIN SMTP SETTING

        // MODE ENVOI SETTING
        ModeEnvoi enumResultEnvoiSetting = envoiSetting.Mode;
        string resultSwitch = enumResultEnvoiSetting switch
        {
            ModeEnvoi.Reel => TraiterModeEnvoiReel(envoiSetting, smtpSettings),
            ModeEnvoi.Simulation => TraiterModeEnvoiSimulation(envoiSetting, smtpSettings),
            ModeEnvoi.Test => TraiterModeEnvoiTest(envoiSetting, smtpSettings),
            _ => throw new Exception("Mode Envoi Inconnu.")
        };
        _logger.LogInformation("Serveur {Host} sur le PORT : {PORT} et avec la méthode : {MethodeChiffrement} et le mode d'envoi : {ModeEnvoi}.", smtpSettings.Host, smtpSettings.Port, smtpSettings.SecureSocketOptions?.ToString() ?? "Non Défini", enumResultEnvoiSetting.ToString());
        // FIN MODE ENVOI SETTING
        // 
        // Fin de la partie Configuration des paramètres Smtp et Mode Envoi (appsettings.json et variables d'environnement)


        // DEBUT PARTIE MAILING
        //
        // LES COMPTEURS POUR MAILING 
        int compteurAccepted = 0;
        int compteurRejeted = 0;
        int compteurSansEmail = 0;
        int compteurErreur = 0;
        int compteurSimulation = 0;
        // CONFIGURATION DE L'EXPEDITEUR
        MailboxAddress messageSender = new MailboxAddress(Encoding.UTF8, smtpSettings.Sender, smtpSettings.User);
        try
        {
            // CONNEXION
            if (envoiReelPossible)
            {
                Console.WriteLine($"Veuillez confirmer par 'oui' ou 'non' l'envoi de : {listeEmails.Count} Emails.");
                string? reponseUser = Console.ReadLine();
                if (string.Equals(reponseUser, "oui", StringComparison.OrdinalIgnoreCase))
                {
                    envoiReelPossible = true;
                }
                else
                {
                    envoiPossible = false;
                    envoiReelPossible = false;
                }
                _logger.LogWarning("Vous avez répondu : {ReponseUser}.", reponseUser);
            }
            if (envoiPossible) { await facteur.ConnexionAsync(); }
            foreach (var p in listeEmails)
            {
                // CONSTRUCTION DU MAIL
                if (p.Email is null) { _logger.LogError("Le mail du participant {NomComplet} est absent.", p.NomComplet); compteurSansEmail++; continue; }
                MailboxAddress messageRecipient = new MailboxAddress(Encoding.UTF8, p.NomComplet, p.Email);
                BodyBuilder bodybuilder = new BodyBuilder
                {
                    TextBody = MailBodyBuilder.BuildText(p),
                    HtmlBody = MailBodyBuilder.BuildHtml(p)
                };
                string subject = "Les Balades Bigoudènes 2027";
                MimeEntity body = bodybuilder.ToMessageBody();

                // BOUCLE DE DEUX TENTATIVES D'ENVOI (DECONNEXION INTEMPESTIVE AVEC RISQUE ACCEPTE IDEMPOTENT : DOUBLON MAIL) VIA : nbreEnvoiMax
                for (int i = 1; i <= nbreEnvoiMax; i++)
                {
                    // Une exception ici remonte jusqu'au catch externe et arrête la campagne.
                    if (i > 1 && envoiPossible)
                    {
                        // Seul un échec réseau mène à un 2e essai : le mail précédent a peut-être déjà été remis.
                        _logger.LogWarning("[Connexion]Nouvelle tentative {Tentative}/{NbreEnvoiMax} pour {NomComplet} : le mail précédent a peut-être déjà été remis.", i, nbreEnvoiMax, p.NomComplet);
                        await Task.Delay(delayEnvoiDeco);
                    } else if (envoiPossible) { _logger.LogWarning("[Connexion]Tentative de connexion {i} pour {NomComplet}.", i, p.NomComplet); }
                    // GESTION PROBLEME CONNEXION
                    if (!facteur.IsConnected() && envoiPossible)
                    {
                        // TENTATIVE DE RECONNEXION VIA nbreEssais
                        await EnsureConnectedAsync(p);
                        if (!facteur.IsConnected())
                        {
                            throw new InvalidOperationException($"Connexion SMTP perdue : reconnexion impossible après {nbreEssais} essais.");
                        }
                    }
                    try
                    {
                        // ENVOI DU MAIL
                        if (envoiPossible)
                        {
                            // ENVOI REEL ET SIMULE
                            SendResult response = await facteur.SendEmail(messageRecipient, messageSender, subject, body);
                            _logger.LogInformation("Réponse du serveur : {Reponse}", response.ResponseText);
                            //Mode Test : Partie pour ETHEREAL SEULEMENT : 
                            if (response.ResponseText.Split(" ").Contains("Accepted"))
                            {
                                string[] responseParts = response.ResponseText.Split("MSGID=");
                                if (responseParts.Count() > 1)
                                {
                                    _logger.LogInformation("{Lien}{Reponse}", "https://ethereal.email/message/", responseParts[1].TrimEnd(']'));
                                    /** Variante :  string resultat = texte.Length > 0 ? texte[..^1] : texte; **/
                                }
                            }
                            // FIN PARTIE ETHEREAL
                            if (response.RejectedRecipients.Count == 0)
                            {
                                compteurAccepted++;
                                string acceptes = string.Join(", ", response.AcceptedRecipients.Select(destinataire => destinataire.Address));
                                _logger.LogInformation("[MAIL ENVOYE]Envoi effectué pour : {ParticipantAccepte}", acceptes);
                                break;
                            }
                            else
                            {
                                compteurRejeted++;
                                string refuses = string.Join(", ", response.RejectedRecipients.Select(destinataire => destinataire.Address));
                                _logger.LogInformation("Destinataire(s) refusé(s) : {ParticipantRefuse}", refuses);
                                break;
                            }
                        }
                        else // Mode SIMULATION : Pas D'ENVOI
                        {
                            compteurSimulation++;
                            string simuleModeSimulation = string.Join(", ", messageRecipient.Address);
                            _logger.LogInformation("[Mail]Destinataire(s) Simulé(s) : {ParticipantSimule}", simuleModeSimulation);
                            break;
                        }
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogError(ioEx, "[Serveur]Déconnecté du serveur Serveur. \nParticipant : {NomComplet}.", p.NomComplet);
                        SendingErrorLog.WriteSendingErrorsLog(ioEx, p);
                        if (i == nbreEnvoiMax) { compteurErreur++; }
                    }
                    catch (Exception ex)
                    {
                        compteurErreur++;
                        _logger.LogError(ex, "[Mail]La procédure d'envoi d'email n'a pas pu aboutir.\nParticipant : {NomComplet}.", p.NomComplet);
                        SendingErrorLog.WriteSendingErrorsLog(ex, p);
                        break;
                    }
                    finally
                    {
                        //_logger.LogInformation("Tentative numéro {i} Fin de la procédure de reconnexion pour {ParticipantNomComplet}.", i, p.NomComplet);
                    }
                }
            }
        }
        catch (AuthenticationException authEx)
        {
            _logger.LogError(authEx, "Identifiants SMTP refusés (User ou Password incorrect).");
            SendingErrorLog.WriteSendingErrorsLog(authEx);
        }
        catch (SocketException sockEx)
        {
            _logger.LogError(sockEx, "Serveur non accessible ({Host}).", smtpSettings.Host);
            SendingErrorLog.WriteSendingErrorsLog(sockEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connexion impossible.");
            SendingErrorLog.WriteSendingErrorsLog(ex);
        }
        finally
        {
            _logger.LogInformation("Fin de la procédure d'envoi d'email.");
            await facteur.CloseConnectionAsync();
        }

        string compteRendu;
        if (envoiPossible)
        {
            int compteurNoneTreated = listeEmails.Count - (compteurAccepted + compteurRejeted + compteurErreur + compteurSansEmail);
            compteRendu = $"\nMails à envoyer : {listeEmails.Count}.\nMails envoyés : {compteurAccepted}.\nMails Rejetés : {compteurRejeted}.\nMails non traités : {compteurNoneTreated}.\nÉchecs d'envoi : {compteurErreur}.\nEmail absent : {compteurSansEmail}.";
        }
        else
        {
            int compteurNoneTreated = listeEmails.Count - (compteurSimulation + compteurErreur + compteurSansEmail);
            compteRendu = $"\nMails à envoyer : {listeEmails.Count}.\nMails simulés : {compteurSimulation}.\nMails non traités : {compteurNoneTreated}.\nÉchecs d'envoi : {compteurErreur}.\nEmail absent : {compteurSansEmail}.";
        }
        _logger.LogInformation(compteRendu);
    }



    private async Task EnsureConnectedAsync(Participant pParticipant)
    {
        for (int i = 1; i <= nbreEssais; i++)
        {
            try
            {
                await facteur.ConnexionAsync();
                if (facteur.IsConnected()) { _logger.LogInformation("[Connexion]Reconnexion efféctuée."); break; }
            }
            catch (AuthenticationException authEx)
            {
                _logger.LogError(authEx, "[Connexion]problème d'authentification.");
                throw;
            }
            catch (InvalidOperationException ivEx)
            {
                _logger.LogError(ivEx, "[Connexion]problème d'acces Serveur. \nParticipant : {NomComplet}.", pParticipant.NomComplet);
                if (i == nbreEssais) { throw; }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Connexion]Connexion SMTP perdue : tentative {I}/{NombreEssais} essais.\nParticipant : {NomComplet}.", i, nbreEssais, pParticipant.NomComplet);
            }
            if (i != nbreEssais) { await Task.Delay(delay); }
        }
    }



    private string TraiterModeEnvoiSimulation(EnvoiSettings pSettingModeEnvoi, SmtpSettings pSettingSmtp)
    {
        if (envoiPossible || envoiReelPossible) { throw new Exception("[Auth]Autorisation Non Accordée pour l'envoi de Mails, vérifiez vos drapeaux."); }
        if (pSettingModeEnvoi.Mode != ModeEnvoi.Simulation) { throw new Exception("[Configuration]La section Envoi.Mode n'est pas en mode Simulation dans appsettings.json !"); }
        envoiPossible = false;
        return "Envoi Impossible : Mode Simulation";
    }
    private string TraiterModeEnvoiTest(EnvoiSettings pSettingModeEnvoi, SmtpSettings pSettingSmtp)
    {
        if (envoiPossible || envoiReelPossible) { throw new Exception("Autorisation Non Accordée pour l'envoi de Mails, vérifiez vos drapeaux."); }
        if (pSettingModeEnvoi.Mode != ModeEnvoi.Test) { throw new Exception("La section Envoi.Mode n'est pas en mode Test dans appsettings.json !"); }
        var hostAdress = "smtp.ethereal.email";
        if (!string.Equals(pSettingSmtp.Host, hostAdress, StringComparison.OrdinalIgnoreCase)) { throw new Exception("La section Smtp.Host n'est pas configuré avec Ethereal dans appsettings.json !"); }
        envoiPossible = true;
        return "Envoi Simulé Possible : Mode Test";
    }
    private string TraiterModeEnvoiReel(EnvoiSettings pSettingModeEnvoi, SmtpSettings pSettingSmtp)
    {
        if (envoiPossible || envoiReelPossible) { throw new Exception("Autorisation Non Accordée pour l'envoi de Mails, vérifiez vos drapeaux."); }
        if (pSettingModeEnvoi.Mode != ModeEnvoi.Reel) { throw new Exception("La section Envoi.Mode n'est pas en mode Reel dans appsettings.json !"); }
        if (string.Equals(pSettingSmtp.Host, "smtp.ethereal.email", StringComparison.OrdinalIgnoreCase)) { throw new Exception($"La section Smtp.Host est configuré avec ETHEREAL : '{pSettingSmtp.Host}' dans appsettings.json !"); }
        //if (pSettingSmtp.Host != "Non Disponible") { throw new Exception("La section Smtp.Host n'est pas configuré avec Non Disponible dans appsettings.json !"); }
        envoiPossible = true;
        envoiReelPossible = true;
        return "[PRODUCTION]Envoi Réel Possible : Mode Reel";
    }

    private int CompterHeaders(string[] pListeHeaders)
    {
        return pListeHeaders?.Length ?? throw new InvalidDataException("[Fichier]Impossible de compter le nombre de Headers du Fichier.");
    }

}