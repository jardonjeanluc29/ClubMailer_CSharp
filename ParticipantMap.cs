using CsvHelper.Configuration;

sealed class ParticipantMap : ClassMap<Participant>
{
    public ParticipantMap() {
        Map(m => m.Date).Name("Date");
        Map(m => m.Epreuve).Name("Epreuve", "Épreuve", "Course");
        Map(m => m.Bonnet).Name("N° Bonnet", "N°_Bonnet", "N°Bonnet", "Bonnet");
        Map(m => m.Nom).Name("Nom", "Nom de Famille");
        Map(m => m.Prenom).Name("Prenom", "Prénom");
        Map(m => m.Email).Name("Email", "Courriel", "Adresse Email", "E-Mail", "Adresse E-Mail");
        Map(m => m.Sexe).Name("Sexe", "Genre");
        Map(m => m.DateDeNaissance).Name("Date de Naissance", "Date Naissance");
        Map(m => m.Mobile).Name("Mobile", "Téléphone Mobile", "Téléphone Portable", "Telephone Mobile", "Telephone Portable");
        Map(m => m.Licence).Name("N° de licence", "N°_de_licence", "Numéro De Licence", "Numero De Licence");
        Map(m => m.Club).Name("Club");
    }
}