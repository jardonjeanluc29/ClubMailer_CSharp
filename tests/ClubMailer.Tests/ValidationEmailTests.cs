namespace ClubMailer.Tests;


public class ValidationEmailTests
{
    [Theory]
    [InlineData("jea n34@", ResultatEmail.FormatInvalide)]
    [InlineData("adresse-sans-arobase.fr", ResultatEmail.FormatInvalide)]
    [InlineData("test@", ResultatEmail.FormatInvalide)]
    [InlineData("@domaine.com", ResultatEmail.FormatInvalide)]
    public void IsEmailvalid_EmailInvalide_Renvoi_Invalide(string pEmail, ResultatEmail pResultatAttendu)
    {
        // Arrange: 
        // On instancie rien car la class ValidationEmail est STATIC
        // Act: 
        ResultatEmail resultatActuel = ValidationEmail.IsEmailValid(pEmail);
        // Assert: 
        Assert.Equal(pResultatAttendu, resultatActuel);
    }

    [Theory]
    [InlineData("", ResultatEmail.Vide)]
    [InlineData("   ", ResultatEmail.Vide)]
    [InlineData(null, ResultatEmail.Vide)]
    public void IsEmailvalid_EmailVide_Renvoi_Vide(string? pEmail, ResultatEmail pResultatAttendu)
    {
        // Arrange: 
        // On instancie rien car la class ValidationEmail est STATIC
        // Act: 
        ResultatEmail resultatActuel = ValidationEmail.IsEmailValid(pEmail);
        // Assert: 
        Assert.Equal(pResultatAttendu, resultatActuel);
    }

    [Theory]
    [InlineData("jean.dupont@example.com")]
    [InlineData("contact@example.com")]
    // Variante possible pour l'apprentissage (Pas de deuxième paramètre)
    public void IsEmailvalid_EmailValide_Renvoi_Valide(string pEmail)
    {
        // Arrange: 
        // On instancie rien car la class ValidationEmail est STATIC
        // Act: 
        ResultatEmail resultatActuel = ValidationEmail.IsEmailValid(pEmail);
        ResultatEmail resultatAttendu = ResultatEmail.Valide;
        // Assert: 
        Assert.Equal(resultatAttendu, resultatActuel);
    }

    // Alternative à [Theory] avec [Fact] si j'ai aucun paramètre
    [Fact]
    public void IsEmailvalid_EmailValide_Renvoi_Valide_Fact()
    {
        // Arrange: 
        // On instancie rien car la class ValidationEmail est STATIC
        // Act: 
        string test1 = "jean.dupont@example.com";
        ResultatEmail resultatActuel = ValidationEmail.IsEmailValid(test1);
        ResultatEmail resultatAttendu = ResultatEmail.Valide;
        // Assert: 
        Assert.Equal(resultatAttendu, resultatActuel);
    }
}
