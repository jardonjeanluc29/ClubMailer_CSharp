using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;   // donne accès à [JsonIgnore]

record Participant
{

    public required string? Date { get; init;}
    public required string Epreuve { get; init;}
    public required string? Bonnet { get; init;}
    public required string Nom { get; init;}
    public required string Prenom { get; init;}
    public required string? Email { get; init;}
    public required string? Sexe { get; init;}
    public required string? DateDeNaissance { get; init;}
    public required string? Mobile { get; init;}
    public required string Licence { get; init;}
    public required string? Club { get; init;}
    public bool NageursBigouden => Club?.Contains("bigouden", StringComparison.OrdinalIgnoreCase) == true;

    public string?[] ChampsTextCsvMvp => [Epreuve, Nom, Prenom, Licence];

    //LES NAGEURS BIGOUDENS
    [JsonIgnore][CsvHelper.Configuration.Attributes.Ignore]   // Serialize ignorera cette propriété
    public string NomComplet => $"{Nom} {Prenom}";
    /*[Ignore]
    public string ?ResultMail { get; init;}*/
    
    
    //public required string Inscription { get; init;}


// Exemple pour la surcharge polymorphique (polylmorphisme)
    public override string ToString() => $"{NomComplet} - {Email}";
}