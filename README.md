# Expéditeur d'accusé de commande

Application ASP.NET Core (hébergée sous IIS) qui orchestre la génération d'accusés de commande au format PDF et leur envoi par e-mail à partir d'une base SQL Server. Le système s'appuie sur Hangfire pour la planification récurrente et propose une interface web moderne (en français) pour piloter les déclenchements, consulter l'historique et ajuster les horaires.

## 🧱 Architecture

| Projet                      | Rôle                                                                                                  |
| --------------------------- | ----------------------------------------------------------------------------------------------------- |
| `Expediteur.Domain`         | Modèles métier, contrats et interfaces (e-mail, PDF, dépôts, horloge, orchestrateur).                 |
| `Expediteur.Infrastructure` | Implémentations concrètes (Dapper + SQL Server, QuestPDF, SMTP, orchestrateur `CommandAcknowledger`). |
| `Expediteur.Web`            | Application ASP.NET Core MVC/Razor Pages, Hangfire Dashboard, API REST, UI Tailwind-like.             |
| `Expediteur.Tests`          | Tests unitaires (xUnit, FluentAssertions, NSubstitute) couvrant l'orchestration principale.           |

## ✨ Fonctionnalités principales

- Lecture des commandes à accuser via procédure stockée SQL (`dbo.ObtenirAccusesCommande`).
- Génération PDF élégante avec QuestPDF.
- Envoi SMTP (hôte par défaut `saintelucie1885-fr.mail.protection.outlook.com`).
- Historique des envois et suivi des erreurs dans la table `Expediteur.JobHistory`.
- Planification Hangfire toutes les _X_ heures (1 ≤ X ≤ 24) avec possibilité de pause.
- Déclenchement manuel immédiat via UI ou API REST.
- Interface web responsive (français) : tableau de bord, historique complet, configuration.
- Tableau de bord Hangfire exposé sous `/tableau-hangfire`.

## ⚙️ Prérequis

- .NET SDK 8.0+
- SQL Server 2019+ (ou Azure SQL) avec accès au serveur d'envoi (port 25 ou selon configuration).
- IIS si hébergement on-premise (avec module ASP.NET Core Hosting Bundle).

## 🗄️ Initialisation de la base

1. Créez une base (ex.: `Commandes`).
2. Exécutez le script [`database/CreateSchema.sql`](database/CreateSchema.sql) pour créer le schéma `Expediteur` et les tables de suivi.
3. Implémentez la procédure `dbo.ObtenirAccusesCommande` qui doit retourner les colonnes :
   - `NumeroCommande`, `Client`, `EmailDestinataire`, `DateCommande`, `ReferenceProduit`, `Description`, `Quantite`, `PrixUnitaire`.

## 🔧 Configuration applicative

Fichier `src/Expediteur.Web/appsettings.json` :

- `ConnectionStrings:Commandes` : chaîne vers la base.
- `Email:Expediteur` : adresse envoyeur.
- `Email:Sujet` : sujet de l'e-mail (`{0}` remplacé par le numéro de commande).
- `Email:Smtp` : hôte, port, SSL (désactivé par défaut pour l'hôte Outlook). Ajustez si authentification nécessaire.

## ▶️ Lancement en développement

```powershell
# Restaurer et tester
cd c:\Users\DevWeb\CommandInfoSender
# Assurez-vous que le SDK .NET 8 est installé et accessible dans le PATH
```

> ⚠️ L'environnement fourni n'expose pas `dotnet` ; exécutez ces commandes sur votre machine.

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `dotnet run --project src/Expediteur.Web`

Accédez ensuite à `https://localhost:5001` (ou port indiqué) :

- Tableau de bord : `/`
- Historique : `/Historique`
- Configuration : `/Configuration`
- Dashboard Hangfire : `/tableau-hangfire`

## 🌐 API REST

| Méthode | Route                                 | Description                                              |
| ------- | ------------------------------------- | -------------------------------------------------------- |
| `GET`   | `/api/commandes/historique?limite=20` | Retourne les `limite` dernières exécutions.              |
| `GET`   | `/api/commandes/configuration`        | Détaille l'intervalle et l'état de la planification.     |
| `POST`  | `/api/commandes/declencher`           | Déclenche immédiatement l'envoi.                         |
| `PUT`   | `/api/commandes/configuration`        | Met à jour l'intervalle (1-24 h) et l'état (`EstActif`). |

Corps attendu pour `PUT` :

```json
{
  "intervalleHeures": 4,
  "estActif": true
}
```

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

## 🚀 Déploiement IIS

1. Publiez : `dotnet publish src/Expediteur.Web -c Release -o publish`.
2. Déployez le dossier `publish` derrière un site IIS configuré avec le **Hosting Bundle ASP.NET Core**.
3. Définissez les variables d'environnement ou transformez `appsettings.Production.json` pour la connexion SQL et SMTP.
4. Assurez-vous que le pool d'applications dispose des droits d'accès au dossier `Logs` (Serilog).
5. Ouvrez le port du Dashboard Hangfire si nécessaire (accès protégé recommandé via IIS Authorization).

## 📌 Points d'extension

- Ajouter une authentification (Azure AD, SSO) pour sécuriser l'UI et l'API.
- Brancher un bus d'événements (Service Bus) pour tracer chaque envoi.
- Générer plusieurs langues (FR/EN) en enrichissant les ressources.
- Alimenter un module d'analytics PowerBI via une vue SQL.

Bon envoi d'accusés !
