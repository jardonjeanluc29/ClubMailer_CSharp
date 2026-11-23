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
using System.Net;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Delegates;
using MimeKit.Encodings;
using System.Security.Cryptography;

public class LectureCSV
{

    //Version Longue UTF-8 private static readonly string _cheminCsv = "participant_pointVirgule.csv";
    // Version Allegée UTF-8 
    private static readonly string _cheminCsv = "participantsLight.csv";
    // Version Tabulation 1252 private static readonly string _cheminCsv = "participant_tabulation.csv";
    // Version Allegée UTF-8 Test Colonne Manquante private static readonly string _cheminCsv = "testColonneManquante.csv";

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
    // Constructeur d'instance 
    public LectureCSV(ILogger<LectureCSV> pIlogger)
    {
        this._logger = pIlogger;
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
        // Le fichier existe-t-il et n'est-il pas vide ? (lance une exception sinon)
        DetecterFichierNullEmpty(_cheminCsv);

        // Quel séparateur ? (renvoie ";" ou "\t", ou lance une exception)
        string delimiteur = DetectDelimiterCSV(_cheminCsv);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            Delimiter = delimiteur,
            // ShouldSkipRecord attend obligatoirement un bool (true = on saute la ligne, false = on la garde).
            // 1. Record? : la ligne peut être null (au début ou en fin de fichier), le "?." évite le plantage et passe null.
            // 2. .All(...) : vérifie si TOUTES les cellules de cette ligne sont vides ou ne contiennent que des espaces.
            // 3. ?? true : si Record était null, on transforme ce null en "true" pour sauter la ligne.
            ShouldSkipRecord = arg => arg.Row.Parser.Record?.All(string.IsNullOrWhiteSpace) ?? true,
            // Formatage majuscules/minuscules, ni des espaces avant ou après le mot.
            PrepareHeaderForMatch = arg => arg.Header.ToLowerInvariant().Trim()
        };

        // On test l'encodage pour etre sur du bon format dans la création
        using var lecteurCSV = LecteurAvecBonEncodage(_cheminCsv);
        string encodage = lecteurCSV.CurrentEncoding.WebName;


        // On passe le Lecteur finalisé au Package CsvHelper.CsvReader
        // Note IA : le false est leaveOpen (« laisser le flux ouvert »). 
        // Avec false, fermer le CsvReader ferme aussi le StreamReader. 
        // Les deux using ferment donc le même fichier. C'est sans danger : appeler Dispose() deux fois est permis.
        // using var csvFile = new CsvReader(lecteurCSV, config, leaveOpen: false);
        // Donc pas besoin du premier Using en THEORIE : En C#, il existe une règle simple et universelle : « Celui qui crée un objet IDisposable (avec new) est responsable de le mettre dans un using. »
        using CsvReader csvFile = new CsvHelper.CsvReader(lecteurCSV, config, false);


        // Ici on indique quel ClassMap choisir quand on fait ToList Participant
        //  ( il sait que c est notre ClassMap qu il faut prendre avec nos optional etc et pas celle qui genere lui meme à partir de participant)
        csvFile.Context.RegisterClassMap<ParticipantMap>();
        DetecterHeaderManquant(csvFile);

        List<string> listeHeadersOptionalDuFichierManquant = DetecterHeadersOptionnelsManquants(csvFile);
        if (listeHeadersOptionalDuFichierManquant.Any())
        {
            _logger.LogWarning("[Fichier]Il y a {Count} header(s) optionnel(s) manquant(s) dans le CSV : {Manquants}.", listeHeadersOptionalDuFichierManquant.Count, string.Join(", ", listeHeadersOptionalDuFichierManquant));
        }


        List<Participant> listeParticipants = csvFile.GetRecords<Participant>()
        //.Where(ligne => !ligne.ChampsTextCsvMvp.All(string.IsNullOrWhiteSpace))
        .ToList();
        // [IMPORTANT] Ici : on attrape un Header Manquant (qui n'est pas Optional et donc nécessaire au programme)  mais on a verifié avant donc aucun risque      



        // On va nettoyer de la liste, les lignes d'entête qui se répêtent
        // La condition : soit Faux 
        // => TOUS les champs de la liste qui ont le nom correspondant à ChampsTextCsvMvp 
        // => Doivent etre Contenu dans la liste des Headers
        // Si c est le cas : c'est une répétition du header donc on n'en veut pas comme une ligne d'inscription.
        // C'est moins compréhenssible au premier abord mais on utilise listeHeaders... pour comparer la présence et pas l'inverse car :
        // ChampsTextCsvMvp est une liste réduite des champs, listeHeaders est complete elle normalement.
        string[] listeHeadersCompleteDuFichier = ListerHeaders(csvFile);
        List<Participant> listeParticipantSansDoublonHeaders = listeParticipants
        .Where(p => !p.ChampsTextCsvMvp.All(champ => listeHeadersCompleteDuFichier.Contains(champ, StringComparer.OrdinalIgnoreCase)))
        .ToList();

        // Variables pour le Rapport de Journalisation
        int nbreHeaders = CompterHeaders(csvFile); ;
        int nbreHeadersManquants = listeHeadersOptionalDuFichierManquant.Count;
        int lignesTotalFichierBrut = listeParticipants.Count;
        int lignesRetirees = listeParticipants.Count - listeParticipantSansDoublonHeaders.Count;
        int lignesInscriptions = listeParticipantSansDoublonHeaders.Count;
        if (lignesTotalFichierBrut - (lignesInscriptions + lignesRetirees) != 0)
        {
            _logger.LogWarning("[Fichier]Le fichier n'est pas Valide");
            throw new InvalidDataException("Le fichier n'est pas Valide, impossible de traiter les lignes correctement.");
        }
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
    encodage,
    delimiteur == "\t" ? "tabulation" : delimiteur,
    lignesTotalFichierBrut,
    lignesRetirees,
    lignesInscriptions,
    nbreHeaders,
    nbreHeadersManquants
    );

        // On va utiliser la méthode native de .net pour valider la presence d'email (valide) ou non 

        List<Participant> listeAvecEmailValide = listeParticipantSansDoublonHeaders
        // est devenu facultatif, la methode le fait déjà ensuite 
        // .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Email))
        .Where<Participant>(p => ValidationEmail.IsEmailValid(p.Email) == ResultatEmail.Valide)
        .ToList();

        List<Participant> participantsRejetes = listeParticipantSansDoublonHeaders
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


        List<List<Participant>> listeParCourse = listeParticipants
        .Where<Participant>(p => !string.IsNullOrWhiteSpace(p.Epreuve))
        .GroupBy(p => p.Epreuve, StringComparer.OrdinalIgnoreCase)
        .Select(gr => gr.ToList())
        .ToList();


        List<Participant> listeParCourseSimple = listeParticipants
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
                        _logger.LogWarning("Nouvelle tentative {Tentative}/{NbreEnvoiMax} pour {NomComplet} : le mail précédent a peut-être déjà été remis.", i, nbreEnvoiMax, p.NomComplet);
                        await Task.Delay(delayEnvoiDeco);
                    }
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
                            _logger.LogInformation("Destinataire(s) Simulé(s) : {ParticipantSimule}", simuleModeSimulation);
                            break;
                        }
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogError(ioEx, "Déconnecté du serveur Serveur. \nParticipant : {NomComplet}.", p.NomComplet);
                        SendingErrorLog.WriteSendingErrorsLog(ioEx, p);
                        if (i == nbreEnvoiMax) { compteurErreur++; }
                    }
                    catch (Exception ex)
                    {
                        compteurErreur++;
                        _logger.LogError(ex, "La procédure d'envoi d'email n'a pas pu aboutir.\nParticipant : {NomComplet}.", p.NomComplet);
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

    private bool DetecterFichierNullEmpty(string pCheminCsv)
    {
        if (!File.Exists(pCheminCsv))
        {
            _logger.LogWarning("[Fichier]Fichier {Fichier} Manquant", pCheminCsv);
            throw new FileNotFoundException($"Le fichier {pCheminCsv} est introuvable.", pCheminCsv);
        }
        using var lecteurCSV = new StreamReader(pCheminCsv);
        if (lecteurCSV.Peek() == -1)
        {
            _logger.LogWarning("[Fichier]Fichier {Fichier} Vide", pCheminCsv);
            throw new InvalidDataException($"Le fichier CSV '{pCheminCsv}' est totalement vide.");
        }
        return true;
    }

    private string DetectDelimiterCSV(string pCheminCsv)
    {
        using StreamReader lecteur = new StreamReader(pCheminCsv);
        string? ligneEnTete = lecteur.ReadLine();

        // Fichier sans aucune ligne : rien à analyser.
        if (string.IsNullOrWhiteSpace(ligneEnTete))
        {
            throw new InvalidDataException($"[Fichier]Le fichier CSV '{pCheminCsv}' n'a pas de ligne d'en-tête.");
        }

        int nbrePointsVirgules = ligneEnTete.Count(c => c == ';');
        int nbreTabulations = ligneEnTete.Count(c => c == '\t');

        // Aucun des deux, ou autant de l'un que de l'autre : on refuse plutôt que de deviner.
        if (nbrePointsVirgules == nbreTabulations)
        {
            throw new InvalidDataException(
                $"[Fichier]Format CSV inconnu dans '{pCheminCsv}' : séparateur ';' ou tabulation introuvable ou ambigu dans l'en-tête.");
        }

        string delimiteur = nbrePointsVirgules > nbreTabulations ? ";" : "\t";
        return delimiteur;
    }
    private StreamReader LecteurAvecBonEncodage(string pCheminCsv)
    {
        // [IMPORTANT] Ici on renvoi sans USING car sinon il serait fermé avec Dispose() et donc innaccessible ensuite.
        // On crée un décodeur UTF-8 strict (throwOnInvalidBytes: true)
        var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            //Windows-1252 donne un caractère à presque tous les octets possibles.
            //Il ne peut donc pas échouer, alors qu'UTF-8 strict échoue dès qu'un octet est invalide. 
            //Seul le test qui peut échouer sait distinguer les deux formats.
            using (var lecteurCSV = new StreamReader(pCheminCsv, utf8Strict, detectEncodingFromByteOrderMarks: true))
            {
                lecteurCSV.ReadToEnd();
            }
            // Si aucune exception n'a été levée, le fichier est bien 100 % UTF-8 !
            // Mis dans la méthode parente _logger.LogInformation("[Fichier]Le {Fichier} est encodé en UTF-8.", pCheminCsv);
            return new StreamReader(pCheminCsv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        }
        catch (DecoderFallbackException)
        {
            // Pas du UTF-8 valide : c'est un fichier Excel français (Windows-1252).
            // CodePagesEncodingProvider.Instance fournit l'encodage sans rien enregistrer
            // globalement (pas d'état partagé, donc testable sans Program.cs).
            // GetEncoding peut renvoyer null : on échoue avec un message clair.
            Encoding windows1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252)
                ?? throw new InvalidOperationException("[Fichier]L'encodage Windows-1252 n'est pas disponible sur cette machine.");
            return new StreamReader(pCheminCsv, windows1252);
        }
    }
    private bool DetecterHeaderManquant(CsvReader pCsv)
    {
        try
        {
            if (!pCsv.Read() || !pCsv.ReadHeader())
            {
                throw new InvalidDataException("[Fichier]Le fichier CSV est vide ou ne contient aucun en-tête.");
            }

            // IMPORTANT : Passer <Participant> (le modèle), pas <ParticipantMap> !
            pCsv.ValidateHeader<Participant>();

            return true;
        }
        catch (HeaderValidationException headEx)
        {
            var manquants = string.Join(", ", headEx.InvalidHeaders.SelectMany(h => h.Names));

            _logger.LogWarning(headEx, "[Fichier] Un ou plusieurs Header(s) nécessaire(s) manquant(s) : {HeadersManquants}", manquants);
            throw new InvalidDataException($"[Fichier]Un ou plusieurs Header(s) nécessaire(s) sont manquants dans le fichier : {manquants}", headEx);
        }
    }


    private string[] ListerHeaders(CsvReader pCsv)
    {
        if (pCsv.HeaderRecord is null || pCsv.HeaderRecord.Length == 0)
        {
            throw new InvalidDataException("[Fichier]Aucun Headers dans le Csv");
        }
        return pCsv.HeaderRecord;
    }

    private int CompterHeaders(CsvReader pCsv)
    {
        return pCsv.HeaderRecord?.Length ?? throw new InvalidDataException("[Fichier]Impossible de compter le nombre de Headers du Fichier.");
    }

    private List<string> DetecterHeadersOptionnelsManquants(CsvReader pCsv)
    {
        // Le "?? throw" : si HeaderRecord vaut null, on lance l'exception à la place de continuer.
        string[] headersCsv = pCsv.HeaderRecord ?? throw new InvalidOperationException("[Fichier]Les en-têtes n'ont pas été lus : appeler DetecterHeaderManquant d'abord.");

        // Je récupère le mapping pour les headers du Records , si il ne trouve pas le mapping (ParticipantMap, il renverra null => on crée une exception pour le signaler)  
        ClassMap mapParticipant = pCsv.Context.Maps.Find<Participant>() ?? throw new InvalidOperationException("[ClassMap]La ClassMap pour Participant est absente.");
        // Je lui dis lesquels prendre, on veut les Optional().

        List<string> headerMappingOptionalManquant = mapParticipant.MemberMaps
        .Where(m => m.Data.IsOptional)
        // Ici le Any est important pour comparer la présence d au moins UN dans une liste. Il renvoi tru si il y en AUNCUN pour tous les noms possibles
        .Where(nomColonneOptional => !nomColonneOptional.Data.Names.Any(nomColonnePossible => headersCsv.Contains(nomColonnePossible, StringComparer.OrdinalIgnoreCase)))
        .SelectMany(headersPossibleManquant => headersPossibleManquant.Data.Names)
        .ToList();
        // Ensuite on renvoi la liste des Headers Manquant
        return headerMappingOptionalManquant;
    }
}