using CsvHelper.Configuration;

sealed class ParticipantMap : ClassMap<Participant>
{
    public static readonly string[] NomsColonneLicence =
    [
        "N° de licence",
        "N°_de_licence",
        "Numéro De Licence",
        "Numero De Licence"
    ];

    public static readonly string[] NomsColonneNom =
    [
        "Nom", 
        "Nom de Famille"
    ];
    // Optional() pour tous les champs qui ne relèvent pas du MVP
    public ParticipantMap() {
        Map(m => m.Date).Name("Date").Optional();
        Map(m => m.Epreuve).Name("Epreuve", "Épreuve", "Course");
        Map(m => m.Bonnet).Name("N° Bonnet", "N°_Bonnet", "N°Bonnet", "Bonnet").Optional();
        Map(m => m.Nom).Name(NomsColonneNom);
        Map(m => m.Prenom).Name("Prenom", "Prénom");
        Map(m => m.Email).Name("Email", "Courriel", "Adresse Email", "E-Mail", "Adresse E-Mail");
        Map(m => m.Sexe).Name("Sexe", "Genre").Optional();
        Map(m => m.DateDeNaissance).Name("Date de Naissance", "Date Naissance").Optional();
        Map(m => m.Mobile).Name("Mobile", "Téléphone Mobile", "Téléphone Portable", "Telephone Mobile", "Telephone Portable").Optional();
        Map(m => m.Licence).Name(NomsColonneLicence);
        Map(m => m.Club).Name("Club").Optional();
    }
}