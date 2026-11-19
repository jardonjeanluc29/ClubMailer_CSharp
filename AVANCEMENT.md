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
- **Module 8, étapes 1 et 2 faites (17/11/2026)** : (1) compte rendu en fin de programme (envoyés, refusés par le serveur, échecs, sans e-mail, non traités = total moins la somme des autres). (2) Une seule connexion SMTP : `SmtpEmailSender` implémente `IDisposable`, `_client` est un champ d'instance `readonly`, méthodes `ConnexionAsync` (connexion + authentification privée), `SendEmail`, `CloseConnectionAsync` (gardée par `IsConnected`), `Dispose`. `Program.cs` : `using var facteur`, un `try` autour de la connexion ET de la boucle, `catch` `AuthenticationException` / `SocketException` / `Exception` pour la connexion, `catch (Exception)` par mail dans la boucle, `finally` qui ferme la connexion, compte rendu après. `SendingErrorLog.WriteSendingErrorsLog(ex, Participant? p = null)` : participant facultatif, « (connexion) » dans le journal sinon. Testé : faux `Host` (SocketException) et mauvais mot de passe (AuthenticationException 535 avec `SmtpCommandException` en `InnerException`) → message clair, entrée dans `sending_errors.log`, tous les participants en « non traités ». Notions vues : un seul `catch` s'exécute (le premier qui correspond, comme `if / else if`), encapsulation (le `SmtpClient` reste caché dans `SmtpEmailSender`), `IDisposable` / `using var` / `Dispose`, `DisconnectAsync` vs `Dispose` vs événement `Disconnected`, paramètre facultatif `= null`, lecture d'un `StackTrace`.
- **Module 8, état constaté dans le code (reprise du 19/11/2026)** : (3) retry **écrit** dans `Program.cs` (boucle `for` de 3 essais dans le `if (!facteur.IsConnected())`, `Task.Delay(3000)` puis `ConnexionAsync()`, `catch` séparés `AuthenticationException` / `InvalidOperationException` / `Exception`, puis `InvalidOperationException` si toujours déconnecté) ; le `break` problématique du `catch (Exception)` a disparu. Reste : `delay` déclaré dans la boucle, `3` en dur dans le message final au lieu de `nbreEssais`, jamais testé par une vraie coupure réseau. (4) logging structuré **en cours** : `ILogger logger` est maintenant utilisé (`LogInformation` / `LogError`) à la place de la plupart des `Console.WriteLine` de la boucle d'envoi et du `compteRendu`, MAIS (a) la ligne 20 de `Program.cs` appelle `logger.WriteLine(...)` qui n'existe pas sur `ILogger` → **erreur de compilation** (à remplacer par `logger.LogInformation`) ; (b) les messages utilisent l'interpolation `$"..."` au lieu de modèles à `{}` (`logger.LogError(ex, "... {Participant}", ...)`), ce qui annule l'intérêt du logging structuré ; (c) `SendingErrorLog.WriteSendingErrorsLog` n'est plus appelé que dans le dernier `catch (Exception)` : le `catch (Exception)` par mail dans la boucle et les `catch` `AuthenticationException` / `SocketException` n'écrivent plus dans `sending_errors.log` (régression à confirmer : voulu ou oubli ?) ; (d) beaucoup de `Console.WriteLine` restent dans la partie CSV/LINQ. Ménage déjà fait : classe et fichier renommés `SendingErrorLog` ; expéditeur déplacé dans `appsettings.json` (`Smtp:Sender`, propriété `SmtpSettings.Sender`, validé au démarrage) ; sujet changé en « Les Balades Bigoudènes 2027 » mais toujours en dur.
- **Prochaine étape** : fin du Module 8 : d'abord faire recompiler `Program.cs` (ligne 20 `logger.WriteLine`), puis (4) passer au vrai logging structuré (modèles `{Nom}` + exception en premier argument, décider du sort de `SendingErrorLog`), puis finir (3) le retry (`nbreEssais`, `delay`). Puis question de vérification de fin de module (différence entre un échec qui arrête tout et un échec qui touche un seul participant). Pense à vérifier que les variables d'environnement de test (`Smtp__Password`, `Smtp__Host`) sont supprimées.
- **À faire quand l'occasion se présente (petit ménage Module 7)** : sujet (`"Les Balades Bigoudènes 2027"`) encore en dur dans `Program.cs` (idée : sujet avec la course ; l'expéditeur est désormais dans `appsettings.json`) ; `using` inutiles en tête de `Program.cs` et de `config/SmtpSettings.cs` ; préfixes `MimeKit.` redondants dans `SmtpEmailSender.cs` ; `Encoding.UTF8` en dur ; encodage `HtmlEncode` éventuellement factorisé dans des variables locales de `BuildHtml`.
- **Petit ménage Module 8** : supprimer l'ancien `sending_errors.json` / `sending_errors.log` à la racine du projet ; `email_rejets.log` suit encore `GetCurrentDirectory` (à aligner sur `AppContext.BaseDirectory`, Module 18) ; ajouter un `README.md` qui liste les commandes `dotnet user-secrets set` (équivalent d'un `.env.example`) ; (renommage `AppendLogSendingError` → `SendingErrorLog` : fait).
- **Tâche future (test réseau réel) — RÈGLE POUR CLAUDE : prévenir Jean-Luc À L'AVANCE, dès l'annonce du module qui utilisera le vrai CSV (Module 9 — Lire le vrai export CSV), qu'on traitera `IOException` à ce moment-là** : quand le vrai fichier CSV (plusieurs centaines de participants) sera utilisé, tester le retry en coupant vraiment le Wi-Fi en pleine boucle d'envoi. Vérifier (a) si MailKit lance une `IOException` en plus de `SocketException` (sinon le retry ne se déclenche pas), (b) la valeur de `IsConnected()` après la coupure, (c) le sort du participant dont l'envoi était en cours au moment de la coupure.
- **À prévoir plus tard** : envoi manuel (une personne + une course) depuis l'interface du Module 15, d'où l'intérêt que la logique d'envoi ne reste pas collée dans `Program.cs`.
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
- [ ] Module 9 — Lire le vrai export CSV *(ajouté le 18/11/2026)*
- [ ] Module 10 — Le nouveau modèle métier : Participant, Course, Inscription *(ajouté le 18/11/2026)*
- [ ] Module 11 — La base de données SQLite avec Entity Framework Core *(ajouté le 18/11/2026)*
- [ ] Module 12 — Historique et archivage des mails par course *(ajouté le 18/11/2026)*
- [ ] Module 13 — Gestion des courses et maintenance de la base *(ajouté le 18/11/2026)*
- [ ] Module 14 — Le module Agent intégré
- [ ] Module 15 — Interface utilisateur
- [ ] Module 16 — Tests
- [ ] Module 17 — Packaging en .exe
- [ ] Module 18 — Finitions
- [ ] Module 19 — Chatbot local (1) : installer un mini modèle et aider à utiliser l'application *(ajouté le 18/11/2026)*
- [ ] Module 20 — Chatbot local (2) : répondre aux questions sur la base *(ajouté le 18/11/2026)*
- [ ] Module 21 — Chatbot local (3) : exécuter des commandes de l'application *(ajouté le 18/11/2026)*

*(Cette coche initiale est une estimation faite à distance — si elle ne correspond pas exactement à où tu en es réellement, corrige-la toi-même ou demande à Claude Code de l'ajuster à la prochaine session.)*

## Décisions techniques prises (à ne pas redécider)

- `CsvHelper` épinglé en version `33.0.1` (voir le skill `tuteur-csharp-clubmailer` pour le détail du bug qui a motivé ce choix).
- Niveau de code visé : professionnel/idiomatique dès le départ, proportionné à la taille du projet (pas de sur-architecture).
- Un mail par inscription (une personne inscrite à deux courses reçoit deux mails).
- Mode de chiffrement SMTP : `StartTls` par défaut, modes non chiffrés refusés à la validation.
