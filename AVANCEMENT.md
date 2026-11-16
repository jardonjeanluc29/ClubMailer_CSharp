# Avancement — ClubMailer (C#)

Ce fichier n'est **pas** le cadre pédagogique (ça, c'est le skill `tuteur-csharp-clubmailer` + `CLAUDE.md`). Ce fichier est la **mémoire courte du projet** : où on en est concrètement, pour reprendre sans perdre de temps après avoir relancé VS Code ou une session Claude Code.

**Règle pour Claude Code : lire ce fichier en tout début de session pour savoir où on en est. Le mettre à jour (section "Dernier état") avant la fin d'une session de travail, ou dès que Jean-Luc le demande ("mets à jour l'avancement").**

## Dernier état (à mettre à jour à chaque session)

- **Module en cours** : Module 7 — Envoi d'emails (premier envoi réussi, config à externaliser).
- **Dernière chose faite** : premier mail envoyé et reçu dans la boîte Ethereal (faux serveur SMTP de test). MailKit `5.0.0` installé. Classe `SmtpEmailSender` (`services/SmtpEmailSender.cs`) : constructeur `public` qui reçoit un `SmtpSettings`, champ `readonly`, méthode `SendEmail` qui renvoie un `Task<SendResult>` (réponse du serveur + destinataires acceptés/refusés), `DisconnectAsync` avant le `return`, aucun affichage console dans la classe. `Program.cs` (fin du fichier) : crée le `SmtpEmailSender` avec `ConfigEthereal.configEthereal`, deux `MailboxAddress`, un `BodyBuilder` (`TextBody` + `ToMessageBody()`), puis affiche `ResponseText` et les acceptés/refusés. Notions vues : classes abstraites (`InternetAddress`, `MimeEntity`) et leurs filles concrètes, méthodes d'extension (`using MailKit;` nécessaire à `SendAsync`, donc à garder), surcharges de constructeur, F12 / survol / point magique pour découvrir les types.
- **Prochaine étape** : externaliser la config SMTP. (1) `appsettings.json` pour `Host` et `Port` (penser à le copier dans le dossier de sortie via le `.csproj`), (2) `dotnet user-secrets` pour `User` et `Password`, (3) lecture avec `ConfigurationBuilder` (paquets `Microsoft.Extensions.Configuration.Json`, `.UserSecrets`, `.EnvironmentVariables`, `.Binder`), (4) suppression de `config/ConfigEthereal.cs` (mot de passe en clair dans le code), (5) validation de `SmtpSettings` au démarrage, (6) se demander si `SecureSocketOptions` se déduit du port ou devient un réglage. Ensuite : templating du message personnalisé (`HtmlBody` du `BodyBuilder`), puis envoi à la liste de diffusion.
- **Blocages connus** : aucun. Pas de gestion d'erreur réseau pour l'instant : un serveur injoignable ou un mauvais mot de passe fait planter `Program.cs` (c'est le sujet du Module 8, avec `try/finally` autour de la connexion). Le CSV n'a pas encore d'email mal formé (cas `FormatInvalide` jamais testé en vrai) ; 2 participants sans email volontaires comme cas de test. À faire plus tard : ménage des `using` inutiles en tête de `Program.cs` et de `config/SmtpSettings.cs`, préfixes `MimeKit.` redondants dans `SmtpEmailSender.cs`, adresse expéditeur et `Encoding.UTF8` en dur dans `Program.cs`. (L'ancienne note sur le `foreach` de `logging` était périmée : il est actif dans `Program.cs` et alimente bien `email_rejets.log`.)

## Historique des modules

- [x] Module 0 — Installation & premier programme
- [x] Module 1 — Syntaxe de base vs JS/TS/Java
- [x] Module 2 — Modéliser un participant (classe `Participant`)
- [x] Module 3 — Lire le CSV
- [x] Module 4 — Collections et LINQ
- [x] Module 5 — Validation et nettoyage des données
- [x] Module 6 — Construire la liste de diffusion
- [ ] Module 7 — Envoi d'emails *(en cours)*
- [ ] Module 8 — Asynchrone et gestion d'erreurs réseau
- [ ] Module 9 — Le module Agent intégré
- [ ] Module 10 — Interface utilisateur
- [ ] Module 11 — Tests
- [ ] Module 12 — Packaging en .exe
- [ ] Module 13 — Finitions

*(Cette coche initiale est une estimation faite à distance — si elle ne correspond pas exactement à où tu en es réellement, corrige-la toi-même ou demande à Claude Code de l'ajuster à la prochaine session.)*

## Décisions techniques prises (à ne pas redécider)

- `CsvHelper` épinglé en version `33.0.1` (voir le skill `tuteur-csharp-clubmailer` pour le détail du bug qui a motivé ce choix).
- Niveau de code visé : professionnel/idiomatique dès le départ, proportionné à la taille du projet (pas de sur-architecture).
