using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Text;
using CsvHelper.Configuration;
using System.Globalization;

namespace ClubMailer;

internal record ResultatLecture(
    string Delimiteur,
    string Encodage,
    List<string> ListeHeadersOptionalDuFichierManquant,
    string[] ListeHeadersCompleteDuFichier,
    List<Participant> ListeParticipantSansDoublonHeaders,
    int LignesTotalFichierBrut,
    int NombreLignesRetirees
);

internal class LecteurParticipants
{
   internal const string FormatMessageExceptionSeparateurInconnu = "[Fichier]Format CSV inconnu dans '{0}' : séparateur ';' ou tabulation introuvable ou ambigu dans l'en-tête.";
    private readonly ILogger<LecteurParticipants> _logger;

    // Constructeur : 
    public LecteurParticipants(ILogger<LecteurParticipants> pLogger)
    {
        _logger = pLogger;
    }

    // Méthodes Nécessaire à la Lecture du Fichier : 
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
    private static string DetectDelimiterCSV(string pCheminCsv)
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
        if (nbrePointsVirgules == nbreTabulations){ throw new InvalidDataException(string.Format(FormatMessageExceptionSeparateurInconnu, pCheminCsv));}

        string delimiteur = nbrePointsVirgules > nbreTabulations ? ";" : "\t";
        return delimiteur;
    }

    private static StreamReader LecteurAvecBonEncodage(string pCheminCsv)
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

    private static string[] ListerHeaders(CsvReader pCsv)
    {
        if (pCsv.HeaderRecord is null || pCsv.HeaderRecord.Length == 0)
        {
            throw new InvalidDataException("[Fichier]Aucun Headers dans le Csv");
        }
        return pCsv.HeaderRecord;
    }

    private static int CompterHeaders(CsvReader pCsv)
    {
        return pCsv.HeaderRecord?.Length ?? throw new InvalidDataException("[Fichier]Impossible de compter le nombre de Headers du Fichier.");
    }

    private static List<string> DetecterHeadersOptionnelsManquants(CsvReader pCsv)
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

    public ResultatLecture Lire(string pCheminCsv)
    {
        // Le fichier existe-t-il et n'est-il pas vide ? (lance une exception sinon)
        DetecterFichierNullEmpty(pCheminCsv);
        // Quel séparateur ? (renvoie ";" ou "\t", ou lance une exception)
        var delimiteur = DetectDelimiterCSV(pCheminCsv);

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
        using var lecteurCSV = LecteurAvecBonEncodage(pCheminCsv);
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
        var listeHeadersOptionnelsManquants = DetecterHeadersOptionnelsManquants(csvFile);
        if (listeHeadersOptionnelsManquants.Any())
        {
            _logger.LogWarning("[Fichier]Il y a {Count} header(s) optionnel(s) manquant(s) dans le CSV : {Manquants}.", listeHeadersOptionnelsManquants.Count, string.Join(", ", listeHeadersOptionnelsManquants));
        }

        string[] listeHeadersComplete = ListerHeaders(csvFile);
        List<Participant> listeParticipants = csvFile.GetRecords<Participant>().ToList();
        // [IMPORTANT] Ici : on attrape un Header Manquant (qui n'est pas Optional et donc nécessaire au programme)  mais on a verifié avant donc aucun risque      
        int lignesTotalFichierBrut = listeParticipants.Count;
        // On va nettoyer de la liste, les lignes d'entête qui se répêtent
        // La condition : soit Faux 
        // => TOUS les champs de la liste qui ont le nom correspondant à ChampsTextCsvMvp 
        // => Doivent etre Contenu dans la liste des Headers
        // Si c est le cas : c'est une répétition du header donc on n'en veut pas comme une ligne d'inscription.
        // C'est moins compréhenssible au premier abord mais on utilise listeHeaders... pour comparer la présence et pas l'inverse car :
        // ChampsTextCsvMvp est une liste réduite des champs, listeHeaders est complete elle normalement.
        List<Participant> listeParticipantSansDoublonHeaders = listeParticipants
        .Where(p => !p.ChampsTextCsvMvp.All(champ => listeHeadersComplete.Contains(champ, StringComparer.OrdinalIgnoreCase)))
        .ToList();
        int lignesretirees = listeParticipants.Count - listeParticipantSansDoublonHeaders.Count;
        int nbreInscriptions = listeParticipantSansDoublonHeaders.Count;
        if (lignesretirees != 0 && (nbreInscriptions + lignesretirees != lignesTotalFichierBrut))
        {
            _logger.LogWarning("[Fichier]Le fichier n'est pas Valide");
            throw new InvalidDataException("Le fichier n'est pas Valide, impossible de traiter les lignes correctement.");
        }

        ResultatLecture resultat = new ResultatLecture(
            delimiteur,
            encodage,
            listeHeadersOptionnelsManquants,
            listeHeadersComplete,
            listeParticipantSansDoublonHeaders,
            lignesTotalFichierBrut,
            lignesretirees
            );
        return resultat;
    }
}
