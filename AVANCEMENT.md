# Avancement — ClubMailer (C#)

Ce fichier n'est **pas** le cadre pédagogique (ça, c'est le skill `tuteur-csharp-clubmailer` + `CLAUDE.md`). Ce fichier est la **mémoire courte du projet** : où on en est concrètement, pour reprendre sans perdre de temps après avoir relancé VS Code ou une session Claude Code.

**Règle pour Claude Code : lire ce fichier en tout début de session pour savoir où on en est. Le mettre à jour (section "Dernier état") avant la fin d'une session de travail, ou dès que Jean-Luc le demande ("mets à jour l'avancement").**

## Dernier état (à mettre à jour à chaque session)

- **Module en cours** : **Module 8 — Asynchrone et gestion d'erreurs réseau** (bien entamé : `try/catch` et journal d'erreurs faits ; restent le compte rendu d'envoi, la connexion unique, le retry et le logging structuré). Le Module 7 (envoi d'emails) est terminé.
- **Module 8, ce qui est fait (17/11/2026)** : boucle d'envoi de `Program.cs` protégée par `try/catch/finally`. `AuthenticationException` (identifiants refusés, le serveur ne dit pas si c'est User ou Password) et `SocketException` (Host introuvable) → message court avec `.Message` + journal + `break` (l'échec condamne tous les envois). `Exception` générale → message + journal, la boucle continue. Journal via `services/AppendLogSendingError.cs` (classe `static`, écrit `sending_errors.log` dans `AppContext.BaseDirectory`, donc `bin/Debug/net10.0/` en développement). Constat réel : trop d'échecs d'authentification d'affilée → Ethereal répond « 403 Authentication was rate limited » (blocage temporaire, ce n'est pas un code HTTP). L'exception `AuthenticationException` enveloppe une `SmtpCommandException` en `InnerException`, donc un `catch (SmtpCommandException)` ne l'attrape pas. Notions vues : `throw` vs `throws` (Java seulement), exceptions checked/unchecked, `Error` vs `Exception`, objet exception (`Message`, `StackTrace`, `InnerException`, `Source`, `TargetSite`), `throw;` vs `throw ex;` vs `throw new ...` vs enveloppe avec `InnerException`, `{ex}` appelle `ToString()`, ordre des `catch` (précis puis général), quand lancer une exception et quand `continue`/journal, `static` pour une classe sans instance, `.gitignore` avec `!.env.example`.
- **Module 7, ce qui est en place** :
  - Config SMTP externalisée : `appsettings.json` (`Host`, `Port`, `SecureSocketOptions`), `dotnet user-secrets` (`User`, `Password`), `ConfigurationBuilder` dans `Program.cs` (ordre JSON < secrets < variables d'environnement, la dernière source gagne ; variables d'environnement au format `Smtp__Host`). `config/ConfigEthereal.cs` supprimé. Validation au démarrage (section absente, Host vide, Port <= 0, modes non chiffrés `Auto`/`None`/`StartTlsWhenAvailable` refusés, User/Password obligatoires). `SmtpSettings.SecureSocketOptions` nullable, défaut `StartTls` via `??` dans `SmtpEmailSender`.
  - `services/SmtpEmailSender.cs` : une connexion ouverte/fermée par mail.
  - `services/MailBodyBuilder.cs` : classe `static`, `BuildHtml(Participant)` (avec `WebUtility.HtmlEncode` sur le nom et la course) et `BuildText(Participant)` (texte brut, sans encodage volontairement).
  - `Program.cs` : `DistinctBy` sur (email, course) en minuscules ; `foreach` sur `listeEmails` qui envoie **un mail par inscription** (décision de Jean-Luc) ; `continue` si email null ; lien d'aperçu Ethereal affiché à partir du `MSGID` de `ResponseText`.
  - Question de vérification de fin de module (ordre de priorité des sources de config) : réponse correcte de Jean-Luc.
  - Notions vues : binding de configuration, `init` vs `set`, `GetSection` ne renvoie jamais null mais `Get<T>()` oui, `GetValueOrDefault()` sur un enum nullable renvoie `None` (piège), STARTTLS vs SSL direct (465/587), `FirstOrDefault`, `StringComparison` (enum) vs `StringComparer` (objet), chaînes brutes `$"""..."""`, injection HTML et `HtmlEncode`, `continue` vs `return`.
- **Prochaine étape** : suite du Module 8 : (1) compte rendu des envois en fin de boucle (envoyés / refusés / en erreur / non traités après un `break`), (2) `async`/`await` en profondeur (`Task`, pourquoi `await` dans la boucle) et une seule connexion SMTP pour toute la boucle (aujourd'hui une par mail, ce qui aggrave le blocage « rate limited »), (3) retry sur les erreurs temporaires, (4) logging structuré (`Microsoft.Extensions.Logging`) à la place des `Console.WriteLine` / `AppendAllText`. Puis question de vérification de fin de module.
- **À faire quand l'occasion se présente (petit ménage Module 7)** : adresse expéditeur (`"Melody"`, `smtpSettings.User`) et sujet (`"urgent"`) encore en dur dans `Program.cs` (idée : sujet avec la course) ; `using` inutiles en tête de `Program.cs` et de `config/SmtpSettings.cs` ; préfixes `MimeKit.` redondants dans `SmtpEmailSender.cs` ; `Encoding.UTF8` en dur ; encodage `HtmlEncode` éventuellement factorisé dans des variables locales de `BuildHtml`.
- **Petit ménage Module 8** : supprimer l'ancien `sending_errors.json` / `sending_errors.log` à la racine du projet ; `email_rejets.log` suit encore `GetCurrentDirectory` (à aligner sur `AppContext.BaseDirectory`, Module 13) ; ajouter un `README.md` qui liste les commandes `dotnet user-secrets set` (équivalent d'un `.env.example`) ; nom de la classe `AppendLogSendingError` (verbe) éventuellement à renommer (`SendingErrorLog`).
- **À prévoir plus tard** : envoi manuel (une personne + une course) depuis l'interface du Module 10, d'où l'intérêt que la logique d'envoi ne reste pas collée dans `Program.cs`.
- **Blocages connus** : aucun. Pas de gestion d'erreur réseau : un serveur injoignable ou un mauvais mot de passe fait planter `Program.cs` (sujet du Module 8). Le cas `FormatInvalide` d'email n'a pas été testé en vrai dans le CSV ; 2 participants sans email volontaires comme cas de test. Le compte Ethereal n'a pas besoin d'être renouvelé (compte de test, décision de Jean-Luc).

## Historique des modules

- [x] Module 0 — Installation & premier programme
- [x] Module 1 — Syntaxe de base vs JS/TS/Java
- [x] Module 2 — Modéliser un participant (classe `Participant`)
- [x] Module 3 — Lire le CSV
- [x] Module 4 — Collections et LINQ
- [x] Module 5 — Validation et nettoyage des données
- [x] Module 6 — Construire la liste de diffusion
- [x] Module 7 — Envoi d'emails
- [ ] Module 8 — Asynchrone et gestion d'erreurs réseau *(prochain)*
- [ ] Module 9 — Le module Agent intégré
- [ ] Module 10 — Interface utilisateur
- [ ] Module 11 — Tests
- [ ] Module 12 — Packaging en .exe
- [ ] Module 13 — Finitions

*(Cette coche initiale est une estimation faite à distance — si elle ne correspond pas exactement à où tu en es réellement, corrige-la toi-même ou demande à Claude Code de l'ajuster à la prochaine session.)*

## Décisions techniques prises (à ne pas redécider)

- `CsvHelper` épinglé en version `33.0.1` (voir le skill `tuteur-csharp-clubmailer` pour le détail du bug qui a motivé ce choix).
- Niveau de code visé : professionnel/idiomatique dès le départ, proportionné à la taille du projet (pas de sur-architecture).
- Un mail par inscription (une personne inscrite à deux courses reçoit deux mails).
- Mode de chiffrement SMTP : `StartTls` par défaut, modes non chiffrés refusés à la validation.
