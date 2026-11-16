using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;   // donne accès à [JsonIgnore]

record Participant
{
    public required string Nom { get; init;}
    public required string Prenom { get; init;}
    
    [JsonIgnore]   // Serialize ignorera cette propriété
    public string NomComplet => $"{Nom} {Prenom}";
    public string ?Email { get; init;}
    /*[Ignore]
    public string ?ResultMail { get; init;}*/
    public string ?Telephone { get; init;}
    public required string Course { get; init;}
    public required string Licence { get; init;}
    public TimeSpan ?Temps { get; init;}

// Exemple pour la surcharge polymorphique (polylmorphisme)
    public override string ToString() => $"{NomComplet} - {Email}";
}