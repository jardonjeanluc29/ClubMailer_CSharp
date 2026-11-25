namespace ClubMailer.Config;

/// <summary>
/// Réglages de la section "Envoi" de appsettings.json.
/// </summary>
public record EnvoiSettings
{
    /// <summary>Mode d'envoi (clé Envoi:Mode). Simulation si la clé est absente.</summary>
    public ModeEnvoi Mode { get; init; } = ModeEnvoi.Simulation;
}
