# CLAUDE.md — Projet ClubMailer (app de mailing C# pour club sportif)

Ce fichier est lu automatiquement par Claude Code à chaque session démarrée dans ce dossier.

## Cadre pédagogique complet : voir le skill `tuteur-csharp-clubmailer`

Le profil de l'utilisateur (Jean-Luc), les règles pédagogiques (expliquer chaque outil/commande avant usage, relier au JavaScript, laisser l'utilisateur écrire le code en premier, pratiques pro dès le début avec le "pourquoi" systématique...) et la feuille de route complète des modules (0 à 21) sont définis dans le skill Claude `tuteur-csharp-clubmailer`, rattaché au compte de l'utilisateur.

**Toujours charger et appliquer ce skill pour ce projet.** Ne pas improviser le contenu d'un module, ne pas redemander le profil de l'utilisateur, ne pas deviner la feuille de route : tout est déjà défini dans ce skill.

Ce fichier `CLAUDE.md` ne contient que les repères minimaux ci-dessous — un filet de sécurité statique, utile même dans l'hypothèse improbable où le skill ne serait pas chargé pour une raison ou une autre.

## Repères rapides

- Dossier du projet : `C:\Users\kenoh\clubmailer`
- Stack : .NET 10 (LTS), C#, CsvHelper (CSV), MailKit (SMTP), System.Text.Json, xUnit (tests)
- Publication finale : `dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true`
- Windows, clavier AZERTY/FR, VS Code en français — raccourci du terminal intégré : `Ctrl+Shift+ù`
- Projet jumeau en Python (apprentissage en parallèle, sessions toujours séparées) : voir le skill `tuteur-python-clubmailer`, dossier `C:\Users\kenoh\clubmailerpy`

## Sécurité des envois (règle absolue, même sans le skill)

- Ne lancer `dotnet run` ou le `.exe` que si le mode effectif est sans risque : `Envoi:Mode` = `Simulation`, ou `Test` avec `Smtp:Host` = `smtp.ethereal.email` (Ethereal capture les mails, ne les remet à personne). Tant que `Envoi:Mode` n'existe pas : seulement si le Host effectif est `smtp.ethereal.email`.
- Vérifier avant : `appsettings.json`, les variables d'environnement `Smtp__Host` / `Envoi__Mode` (`Get-ChildItem Env:Smtp__*, Env:Envoi__*`), et les **clés** des user-secrets (jamais leurs valeurs, jamais `dotnet user-secrets list`).
- Dans le doute, ne pas lancer : dire pourquoi et donner la commande à Jean-Luc.
- Toute réponse qui propose un changement pouvant déclencher de vrais envois commence, sous l'étiquette, par **⚠️ ENVOIS RÉELS**.

## Mémoire de session : voir `AVANCEMENT.md`

Relancer VS Code ou une session Claude Code démarre une conversation sans aucune mémoire de la précédente — exactement comme un `/clear`. Le skill et ce `CLAUDE.md` donnent le cadre général (toujours le même), mais pas l'état précis du projet à un instant donné (quel module, qu'est-ce qui a été fait, quelle est la prochaine étape). C'est le rôle du fichier `AVANCEMENT.md`, à la racine de ce dossier.

**Règle pour Claude Code : lire `AVANCEMENT.md` en tout début de session pour savoir où on en est. Le mettre à jour (section "Dernier état") avant la fin d'une session de travail, ou dès que Jean-Luc le demande.**
