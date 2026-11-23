# ClubMailer

[![CI ClubMailer (.NET)](https://github.com/jardonjeanluc29/ClubMailer_CSharp/actions/workflows/ci.yml/badge.svg?branch=main&event=push)](https://github.com/jardonjeanluc29/ClubMailer_CSharp/actions/workflows/ci.yml)
[![CI ClubMailer GitLab](https://gitlab.com/jardon.jeanluc29/clubmail_csharp/badges/main/pipeline.svg)](https://gitlab.com/jardon.jeanluc29/clubmail_csharp/-/commits/main)

## Description
Application de mailing pour association sportive, destinée aux événements sportifs : confirmation d'inscription, rappel d'inscription, règlement et déroulement des compétitions.

## Prérequis
SDK .NET 10.0.403 ou une version 10.0.x plus récente (voir `global.json`).

## Configuration
L'adresse du serveur (`Host`), le port et l'expéditeur sont dans `appsettings.json`.
Les identifiants SMTP sont des secrets : ils se rangent hors du dépôt, avec les *user-secrets* de .NET.

```powershell
dotnet user-secrets set "Smtp:User" "..."
dotnet user-secrets set "Smtp:Password" "..."
```

## Modes d'envoi
La clé `Envoi:Mode` de `appsettings.json` choisit le comportement. Sans valeur, le mode est `Simulation`.

- `Simulation` : aucune connexion SMTP, le programme journalise seulement ce qu'il enverrait.
- `Test` : envoi autorisé uniquement vers le faux serveur `smtp.ethereal.email`, qui ne remet aucun mail.
- `Reel` : vrais envois, avec avertissement et confirmation tapée au clavier.

## Intégration continue
À chaque push, deux pipelines (GitHub Actions et GitLab CI/CD) installent le SDK .NET 10 et compilent le projet en mode Release.