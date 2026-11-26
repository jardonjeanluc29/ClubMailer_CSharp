using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubMailer.Tests;

public class LecteurParticipantsTests
{
    [Fact]
    public void Lire_FormatValideCsv_RenvoiTroisParticipants()
    {
        // Arrange:
        string fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "format_a_valide.csv");
        // Arrange:
        NullLogger<LecteurParticipants> nullLogger = NullLogger<LecteurParticipants>.Instance;
        // Act:
        ResultatLecture _sut = new LecteurParticipants(nullLogger).Lire(fixturesPath);
        string delimiteur = _sut.Delimiteur.ToString().ToLowerInvariant();
        string encodage = _sut.Encodage;
        List<string> listeHeadersOptionalDuFichierManquant = _sut.ListeHeadersOptionalDuFichierManquant;
        string[] listeHeadersCompleteDuFichier = _sut.ListeHeadersCompleteDuFichier;
        List<Participant> listeParticipantSansDoublonHeaders = _sut.ListeParticipantSansDoublonHeaders;
        int lignesTotalFichierBrut = _sut.LignesTotalFichierBrut;
        int nombreLignesRetirees = _sut.NombreLignesRetirees;
        // Assert:
        string delimiteurAttendu = ";";
        string encodageAttendu = "utf-8";
        Assert.Equal(delimiteurAttendu, delimiteur);
        Assert.Equal(encodageAttendu, encodage);
        Assert.Empty(listeHeadersOptionalDuFichierManquant);
        string[] listeHeadersAttendue = { "Date", "Epreuve", "N° Bonnet", "Nom", "Prenom", "Email", "Sexe", "Date de Naissance", "Mobile", "N° de licence", "Club" };
        Assert.Equal(listeHeadersAttendue, listeHeadersCompleteDuFichier);
        List<Participant> listeParticipantsAttendue = [
            new Participant{
                Date = "01/01/2025 10:00",
                Epreuve = "Course Mock 5 Km",
                Bonnet = "101",
                Nom = "MOCKNOM1",
                Prenom = "Alice",
                Email = "alice.mock1@example.com",
                Sexe = "F",
                DateDeNaissance = "01/01/2000",
                Mobile = "0600000001",
                Licence = "0000001",
                Club = "CLUB MOCK"
            },
            new Participant{
                Date = "01/01/2025 10:05",
                Epreuve = "Course Mock 5 Km",
                Bonnet = "102",
                Nom = "MOCKNOM2",
                Prenom = "Bruno",
                Email = "bruno.mock2@example.com",
                Sexe = "M",
                DateDeNaissance = "02/02/2001",
                Mobile ="",
                Licence = "0000002",
                Club ="CLUB MOCK"
                },
            new Participant{
                Date = "01/01/2025 10:10",
                Epreuve = "Course Mock 10 Km",
                Bonnet = "103",
                Nom = "MOCKNOM3",
                Prenom = "Chloe",
                Email = "chloe.mock3@example.com",
                Sexe = "F",
                DateDeNaissance = "03/03/2002",
                Mobile = "0600000003",
                Licence = "0000003",
                Club ="CLUB MOCK"
                }
        ];
        Assert.Equivalent(listeParticipantsAttendue, listeParticipantSansDoublonHeaders, true);
        Assert.Equal(3, lignesTotalFichierBrut);
        Assert.Equal(0, nombreLignesRetirees);
    }

    [Fact]
    public void Lire_FormatBValide_RenvoiTabulationEtWindows1252()
    {
        // Arrange:
        string fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "format_b_valide.csv");
        // Arrange:
        NullLogger<LecteurParticipants> _sut = NullLogger<LecteurParticipants>.Instance;
        // Act:
        ResultatLecture resultat = new LecteurParticipants(_sut).Lire(fixturesPath);
        string delimiteur = resultat.Delimiteur.ToString().ToLowerInvariant();
        string encodage = resultat.Encodage.ToLowerInvariant();
        List<string> listeHeadersOptionalDuFichierManquant = resultat.ListeHeadersOptionalDuFichierManquant;
        string[] listeHeadersCompleteDuFichier = resultat.ListeHeadersCompleteDuFichier;
        List<Participant> listeParticipantSansDoublonHeaders = resultat.ListeParticipantSansDoublonHeaders;
        int lignesTotalFichierBrut = resultat.LignesTotalFichierBrut;
        int nombreLignesRetirees = resultat.NombreLignesRetirees;
        // Assert:
        string delimiteurAttendu = "\t";
        string encodageAttendu = "windows-1252";
        Assert.Equal(delimiteurAttendu, delimiteur);
        Assert.Equal(encodageAttendu, encodage);
        Assert.Empty(listeHeadersOptionalDuFichierManquant);
        string[] listeHeadersAttendue = { "Date", "Epreuve", "N° Bonnet", "Nom", "Prenom", "Email", "Sexe", "Date de Naissance", "Mobile", "N° de licence", "Club" };
        Assert.Equal(listeHeadersAttendue, listeHeadersCompleteDuFichier);
        List<Participant> listeParticipantsAttendue = [
            new Participant{
                Date = "01/01/2025 10:00",
                Epreuve = "Course Mock 5 Km",
                Bonnet = "101",
                Nom = "MOCKNOM1",
                Prenom = "Mélina",
                Email = "alice.mock1@example.com",
                Sexe = "F",
                DateDeNaissance = "01/01/2000",
                Mobile = "0600000001",
                Licence = "0000001",
                Club = "CLUB MOCK"
            },
            new Participant{
                Date = "01/01/2025 10:05",
                Epreuve = "Course Mock 5 Km",
                Bonnet = "102",
                Nom = "MOCKNOM2",
                Prenom = "Rémy",
                Email = "bruno.mock2@example.com",
                Sexe = "M",
                DateDeNaissance = "02/02/2001",
                Mobile ="",
                Licence = "0000002",
                Club ="CLUB MOCK"
                },
            new Participant{
                Date = "01/01/2025 10:10",
                Epreuve = "Course Mock 10 Km",
                Bonnet = "103",
                Nom = "MOCKNOM3",
                Prenom = "Chlœé",
                Email = "chloe.mock3@example.com",
                Sexe = "F",
                DateDeNaissance = "03/03/2002",
                Mobile = "0600000003",
                Licence = "0000003",
                Club ="CLUB MOCK"
                }
        ];
        Assert.Equivalent(listeParticipantsAttendue, listeParticipantSansDoublonHeaders, true);
        Assert.Equal(3, lignesTotalFichierBrut);
        Assert.Equal(0, nombreLignesRetirees);
    }

    [Fact]
    public void Lire_FichierIntrouvable_LanceFileNotFoundException()
    {
        // Arrange:
        string fixturesPath = Path.Combine(AppContext.BaseDirectory, "fichierFantome.csv");
        NullLogger<LecteurParticipants> logger = NullLogger<LecteurParticipants>.Instance;

        // Act & Assert:
        FileNotFoundException _sut = Assert.Throws<FileNotFoundException>(() => new LecteurParticipants(logger).Lire(fixturesPath));
        string cheminAttendu = fixturesPath;
        Assert.Equal(cheminAttendu, _sut.FileName);
    }

    [Fact]
    public void Lire_ColonneObligatoireManquante_LanceInvalidDataException()
    {
        // Arrange: 
        string fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "format_a_sans_licence.csv");
        NullLogger<LecteurParticipants> logger = NullLogger<LecteurParticipants>.Instance;
        // Act & Assert

        //INUTILE !! Il faut garder ça comme une étape pour débutant à remetre ensuite dans l Assert, sert à conceptualiser par découpage 
        // ResultatLecture _sut = new LecteurParticipants(logger).Lire(fixturesPath);

        InvalidDataException resultat = Assert.Throws<InvalidDataException>(() => new LecteurParticipants(logger).Lire(fixturesPath));
        // Version Simple sans tester la collection des possibles : 
        // string headerManquantAttendu = "N° de licence";
        //Assert.Contains(headerManquantAttendu, resultat.Message, StringComparison.OrdinalIgnoreCase);
        
        Assert.All(ParticipantMap.NomsColonneLicence, n =>  Assert.Contains(n, resultat.Message, StringComparison.OrdinalIgnoreCase));
    }
}
