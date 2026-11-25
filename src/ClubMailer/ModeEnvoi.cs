// Fichier : ModeEnvoi.cs
/// <summary>
/// Mode d'envoi des emails, choisi dans appsettings.json (clé Envoi:Mode).
/// Si la clé est absente, le mode est Simulation.
/// </summary>
public enum ModeEnvoi
{
    /// <summary>Aucune connexion au serveur SMTP : on journalise seulement chaque email qui serait envoyé.</summary>
    Simulation,
    /// <summary>Envoi vers Ethereal (smtp.ethereal.email), qui capture les emails sans les remettre à personne. Refusé si le serveur n'est pas Ethereal.</summary>
    Test,
    /// <summary>Envoi de vrais emails aux participants, après une confirmation tapée au clavier.</summary>
    Reel
}