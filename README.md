# Expéditeur d'accusé de commande (édition desktop)

Application Windows (WPF/.NET 9) qui orchestre la génération d'accusés de commande au format PDF et leur envoi par e-mail à partir d'une base SQL Server. Le système s'appuie sur un service de planification intégré et propose une interface de bureau moderne (en français) pour piloter les déclenchements, consulter l'historique et ajuster les horaires.

## 🧱 Architecture

| Projet                      | Rôle                                                                                                             |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `Expediteur.Domain`         | Modèles métier, contrats et interfaces (e-mail, PDF, dépôts, horloge, orchestrateur).                            |
| `Expediteur.Infrastructure` | Implémentations concrètes (Dapper + SQL Server, PdfSharp/MigraDoc, SMTP, orchestrateur `CommandAcknowledger`).   |
| `Expediteur.Desktop`        | Application WPF (.NET 9, WinUI-like) embarquant l'interface opérateur et le service de planification récurrente. |
| `Expediteur.Tests`          | Tests unitaires (xUnit, FluentAssertions, NSubstitute) couvrant l'orchestration principale.                      |

## ✨ Fonctionnalités principales

- Lecture des commandes à accuser via procédure stockée SQL (`dbo.ObtenirAccusesCommande`).
- Génération PDF élégante avec PdfSharp/MigraDoc (accusé soigné, compatible entreprise).
- Envoi SMTP (hôte par défaut `saintelucie1885-fr.mail.protection.outlook.com`).
- Historique des envois et suivi des erreurs dans la table `Expediteur.JobHistory`.
- Planification automatique toutes les _X_ heures (1 ≤ X ≤ 24) via un service d'arrière-plan Windows, activable/désactivable depuis l'UI.
- Déclenchement manuel immédiat via la fenêtre de contrôle.
- Interface desktop (français) : tableau de bord, historique complet, configuration.

## ⚙️ Prérequis

- .NET SDK 9.0+
- Windows 10/11 (x64) avec prise en charge WPF.
- SQL Server 2019+ (ou Azure SQL) avec accès au serveur d'envoi (port 25 ou selon configuration).

## 🗄️ Initialisation de la base

1. Créez une base (ex.: `Commandes`).
2. Exécutez le script [`database/CreateSchema.sql`](database/CreateSchema.sql) pour créer le schéma `Expediteur` et les tables de suivi.
3. Implémentez la procédure `dbo.ObtenirAccusesCommande` qui doit retourner les colonnes :
   - `NumeroCommande`, `Client`, `EmailDestinataire`, `DateCommande`, `ReferenceProduit`, `Description`, `Quantite`, `PrixUnitaire`.

## 🔧 Configuration applicative

Fichier `src/Expediteur.Desktop/appsettings.json` (et variantes `appsettings.{Environnement}.json` si nécessaire) :

- `ConnectionStrings:Commandes` : chaîne vers la base.
- `Email:Expediteur` : adresse envoyeur.
- `Email:Sujet` : sujet de l'e-mail (`{0}` remplacé par le numéro de commande).
- `Email:Smtp` : hôte, port, SSL (désactivé par défaut pour l'hôte Outlook). Ajoutez `User`/`Password` si authentification requise.

## ▶️ Lancement en développement

```powershell
# Restaurer et tester
cd c:\Users\DevWeb\CommandInfoSender
# Assurez-vous que le SDK .NET 9 est installé et accessible dans le PATH
```

> ⚠️ L'environnement fourni n'expose pas `dotnet` ; exécutez ces commandes sur votre machine.

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `dotnet run --project src/Expediteur.Desktop`

L'interface Windows se lance et permet :

- de consulter l'historique récent des envois ;
- d'activer/désactiver la planification et de modifier l'intervalle ;
- de déclencher immédiatement un envoi.

## 📨 Intégration SMTP

Aucune authentification n'est configurée par défaut. Si votre relais SMTP exige des identifiants, ajoutez :

```json
"Email": {
  "Smtp": {
    "Host": "...",
    "Port": 587,
    "EnableSsl": true,
    "User": "compte",
    "Password": "secret"
  }
}
```

Puis ajustez `SmtpEmailSender` pour utiliser `NetworkCredential` (extension simple).

## 🧪 Tests

Les tests importants se trouvent dans `tests/Expediteur.Tests/CommandAcknowledgerTests.cs` et couvrent :

- Envoi réussi : PDF généré, e-mail envoyé, historique et prochaine exécution mis à jour.
- Gestion d'erreur SMTP : historique marqué en échec sans interrompre la planification.

## 🚀 Distribution (poste client)

1. Publiez : `dotnet publish src/Expediteur.Desktop -c Release -r win-x64 --self-contained false -o publish`.
2. Copiez le dossier `publish` sur le poste client (nécessite .NET Desktop Runtime 9.0 si publication framework-dependent).
3. Ajustez `appsettings.json` (ou `appsettings.Production.json`) avec la chaîne SQL et les paramètres SMTP.
4. Facultatif : créez un raccourci vers `Expediteur.Desktop.exe` et configurez l'exécution au démarrage Windows si besoin.

## 📌 Points d'extension

- Ajouter une authentification (Azure AD, SSO) pour sécuriser l'accès à l'UI.
- Brancher un bus d'événements (Service Bus) pour tracer chaque envoi.
- Générer plusieurs langues (FR/EN) en enrichissant les ressources.
- Alimenter un module d'analytics PowerBI via une vue SQL.

Bon envoi d'accusés !
