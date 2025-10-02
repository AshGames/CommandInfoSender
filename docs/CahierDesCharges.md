# Cahier des charges – Expéditeur d'accusé de commande

## 1. Contexte et objectifs

- **Produit actuel** : application desktop WPF (.NET 9) orchestrant la génération et l'envoi par e-mail d'accusés de commande à partir d'une base SQL Server.
- **Exploitation** : service automatisé opéré par l'équipe informatique, en charge du paramétrage, de la supervision et des déclenchements exceptionnels.
- **Objectif de la phase de production** : industrialiser la solution pour un déploiement sécurisé, supervisé et maintenable dans un environnement client (multi-postes, haute disponibilité des services SQL/SMTP).

## 2. Parties prenantes

| Rôle                                    | Responsabilités principales                                                    | Interlocuteur              |
| --------------------------------------- | ------------------------------------------------------------------------------ | -------------------------- |
| Service Administration Des Ventes (ADV) | Destinataires des accusés, validation métier sans accès direct à l'application | Product owner métier       |
| Équipe IT / Infrastructure              | Gestion des environnements, sécurité, sauvegardes, supervision                 | Responsable infrastructure |
| Équipe développement                    | Implémentation, tests, packaging, documentation                                | Tech lead + développeurs   |
| Support niveau 1                        | Assistance utilisateurs, escalade vers IT/développement                        | Centre de services         |

## 3. Périmètre fonctionnel

- Lecture des commandes à accuser via la procédure stockée `dbo.ObtenirAccusesCommande`.
- Génération des accusés au format PDF (PdfSharp/MigraDoc) avec gabarit corporatif.
- Envoi des e-mails sortants via SMTP (Outlook 365 par défaut).
- Historisation des traitements (table `Expediteur.JobHistory`).
- Pilotage des intervalles de planification via l'UI et un service d'arrière-plan.
- Déclenchement via quatre canaux :
  - planification automatique à intervalle horaire configurable
  - action manuelle de l'équipe informatique depuis l'application desktop
  - insertion d'une commande éligible dans la base (détection et traitement automatiques) à l'INSERT des commandes.
  - sollicitation de l'application web **Lucie Info Livraison** qui transmet la demande pour une commande spécifique.
- Consultation détaillée de l'historique et des erreurs.

## 4. Parcours utilisateurs cibles

1. **Processus automatisé**

- Le service planifié exécute l'envoi d'accusés à la fréquence définie (1 à 24 heures).
- Les nouvelles commandes insérées dans la base sont détectées et intégrées au prochain lot d'envoi sans intervention humaine.

2. **Opérateur IT**

- Configure la chaîne de connexion et les paramètres SMTP (`appsettings.json`).
- Supervise l'état du service de planification et des jobs Hangfire.
- Utilise l'interface desktop pour relancer un envoi à la demande ou ajuster la périodicité.
- Exécute la procédure `Expediteur.TriggerManualExecution` ou déclenche une commande isolée depuis l'application Lucie Info Livraison.

3. **Application web Lucie Info Livraison**

- L'utilisateur habilité déclenche un bouton dédié pour envoyer l'accusé d'une commande ciblée.
- L'appel est relu par l'Expéditeur qui traite uniquement la commande demandée et trace l'opération.

## 5. Exigences fonctionnelles détaillées

- **EF1 – Gestion des commandes** : la procédure `dbo.ObtenirAccusesCommande` doit retourner l'ensemble des colonnes métier requises et filtrer les commandes déjà traitées.
- **EF2 – Génération PDF** : le gabarit doit respecter la charte graphique, supporter les caractères spéciaux (UTF-8) et inclure les totaux.
- **EF3 – Envoi e-mail** : prise en charge de serveurs SMTP authentifiés (NTLM/OAuth2 si nécessaire) avec journalisation des échecs.
- **EF4 – Planification** : plage configurable (1 à 24 heures), activation/désactivation persistante en base (`Expediteur.ScheduleConfiguration`).
- **EF5 – Déclenchements** :
  - Planification automatique à fréquence horaire paramétrable (1 à 24 heures).
  - Action manuelle de l'équipe informatique depuis l'interface desktop.
  - Insertion d'une commande dans la base métier déclenchant automatiquement son accusé au prochain cycle.
  - Intégration **Lucie Info Livraison** : bouton contextuel pour déclencher l'envoi d'une commande unique, relayé à l'Expéditeur via l'API interne ou la procédure `Expediteur.TriggerManualExecution`.
- **EF6 – Historique** : affichage des derniers N traitements avec statut, durée, nombre d'e-mails envoyés.
- **EF7 – Notifications** : en option, envoi d'un e-mail de synthèse en cas d'échec.

## 6. Exigences non fonctionnelles

- **Performance** : traiter jusqu'à 5 000 commandes en moins de 10 minutes (hors délais SMTP).
- **Fiabilité** : reprise automatique après incident (retry Hangfire, filtre des doublons via clés métiers).
- **Sécurité** : chiffrement TLS des connexions SQL et SMTP, obfuscation des secrets (Secret Manager, Azure Key Vault, ou coffre-fort interne).
- **Disponibilité** : fonctionnement nominal sur Windows 10/11 64 bits, compatibilité avec RDS éventuel.
- **Observabilité** : logs structurés (Serilog) et remontée vers un SIEM interne.

## 7. Architecture cible

- Application WPF hébergée sur les postes et serveurs de l'équipe informatique, packagée via `dotnet publish`.
- Couche Domain + Infrastructure partagée avec tests automatisés (xUnit).
- Base SQL Server (on-premise ou Azure SQL) disposant des schémas `dbo` (commandes) et `Expediteur` (pilotage).
- Service Hangfire embarqué dans l'app pour la planification ; option d'externalisation via Hangfire Server Windows Service.
- Procédures stockées et tables dédiées (`Expediteur.JobHistory`, `Expediteur.ScheduleConfiguration`, `Expediteur.ManualTrigger`).
- Envoi SMTP vers le relais d'entreprise (Office 365) avec fallback configurables.

## 8. Gestion des données

- Tables applicatives principales :
  - `dbo.Commandes` (source métier).
  - `Expediteur.JobHistory` (journal d'exécution).
  - `Expediteur.ScheduleConfiguration` (paramétrage planification).
  - `Expediteur.ManualTrigger` (file d'attente pour déclenchements manuels hors UI).
- Scripts SQL fournis (`database/CreateSchema.sql`, `database/CreateDemoDatabase.sql`) à adapter pour l'environnement client.
- Politique de rétention : historique conservé 24 mois, purge automatique à prévoir (job SQL Server Agent).

## 9. Sécurité et conformité

- Gestion des accès SQL via comptes techniques dédiés (principes du moindre privilège).
- Stockage des secrets (SMTP, OAuth) hors du code source (variables d'environnement, coffre-fort).
- Authentification Windows de l'utilisateur final (contrôle d'accès au poste + AD).
- Conformité RGPD : seules les données nécessaires à l'accusé sont manipulées, logs pseudonymisés.
- Audit : traçabilité des déclenchements manuels (UI et procédure stockée) avec l'identité de l'initiateur.

## 10. Exploitation et supervision

- Mise en place de logs applicatifs centralisés (fichier + export vers ELK/Azure Monitor).
- Supervision SQL Server Agent pour les jobs de purge/maintenance.
- Dashboard Hangfire pour consulter les jobs planifiés et les erreurs.
- Guide d'exploitation détaillant procédures de redémarrage, diagnostic, escalade.

## 11. Déploiement et environnements

| Environnement  | Objectif                               | Particularités                                       |
| -------------- | -------------------------------------- | ---------------------------------------------------- |
| Développement  | Tests locaux dev, intégrations rapides | Base de test locale, mocks SMTP                      |
| Recette / QA   | Validation fonctionnelle par ADV       | Copie anonymisée des données réelles, SMTP de test   |
| Pré-production | Répétition générale, tests de charge   | Infrastructure proche de la prod, supervision active |
| Production     | Service aux utilisateurs finaux        | Haute disponibilité SQL/SMTP, supervision 24/7       |

- Pipeline CI/CD : GitHub Actions ou Azure DevOps pour build, tests, packaging MSIX/ZIP.
- Publication via `dotnet publish` (framework-dependent) + script PowerShell de déploiement.

## 12. Tests et validation

- **Unitaires** : couverture du domaine (`CommandAcknowledger`, générations PDF) – existants, à compléter.
- **Intégration** : tests Dapper/SQL sur base de test, tests SMTP via sandbox.
- **End-to-end** : scénario utilisateur sur environnement QA, incluant déclenchement manuel et planifié.
- **Performance** : campagne de charge (5 000 commandes) sur pré-prod avec monitoring CPU/RAM.
- **Recette métier** : validation par ADV selon scénarios fournis.

## 13. Risques principaux et mitigations

| Risque                                                    | Impact                                   | Mitigation                                                      |
| --------------------------------------------------------- | ---------------------------------------- | --------------------------------------------------------------- |
| Dépendance au poste utilisateur (app desktop)             | Rupture de service si poste indisponible | Étudier un service Windows Hangfire centralisé + monitoring     |
| Volume de commandes élevé                                 | Temps d'envoi excessif                   | Optimiser la procédure SQL, paralléliser par lots, scaling SMTP |
| Configuration SMTP changeante                             | Échecs d'envoi                           | Gestion centralisée des secrets, support OAuth2                 |
| Retour en erreur de la procédure `ObtenirAccusesCommande` | Blocage complet                          | Ajout de garde-fous, alerting, gestion d'incident               |

## 14. Planning indicatif

| Phase                       | Durée estimée | Livrables                                              |
| --------------------------- | ------------- | ------------------------------------------------------ |
| Clarification des exigences | 1 semaines    | Cahier des charges validé, backlog priorisé            |
| Durcissement applicatif     | 2 semaines    | Support secrets, logs, mécanisme retry, tests          |
| Industrialisation SQL/SMTP  | 2 semaines    | Procédures stockées finalisées, scripts de déploiement |
| Recette et performance      | 1 semaines    | PV de recette, rapport de charge                       |
| Déploiement pilote          | 1 semaine     | Installation pilote, retour utilisateurs               |
| Passage en production       | 1 semaine     | Documentation d'exploitation, formation                |

## 15. Annexes

- Scripts SQL : `database/CreateSchema.sql`, `database/CreateDemoDatabase.sql`.
- Procédure de déclenchement manuel : `Expediteur.TriggerManualExecution` (insertion dans `Expediteur.ManualTrigger`).
- Documentation technique (README) et tests automatisés (`tests/Expediteur.Tests`).
