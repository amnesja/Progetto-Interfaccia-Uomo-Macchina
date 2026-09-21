# Ordo

## Piattaforma collaborativa per la gestione di progetti e attività

Ordo è una web application per la gestione collaborativa di progetti e attività, progettata attorno a una **board Kanban** e arricchita da funzionalità di collaborazione in tempo reale.

L'applicazione permette agli utenti di creare progetti, organizzare il lavoro in board, creare e assegnare attività, monitorarne lo stato, commentare i task, gestire i collaboratori e comunicare attraverso una chat interna al progetto.

La caratteristica distintiva dell'applicazione è l'integrazione tra una normale architettura ASP.NET Core MVC e componenti frontend dinamici, con **Vue 3**, **TypeScript/JavaScript** e **SignalR** per gli aggiornamenti real-time.

---

## Indice

- [Obiettivi](#obiettivi)
- [Funzionalità](#funzionalità)
- [Tecnologie](#tecnologie)
- [Architettura](#architettura)
- [Modello dei dati](#modello-dei-dati)
- [Autenticazione e autorizzazioni](#autenticazione-e-autorizzazioni)
- [Pagine dell'applicazione](#pagine-dellapplicazione)
- [Kanban](#kanban)
- [Collaborazione real-time](#collaborazione-real-time)
- [Database](#database)
- [Struttura del repository](#struttura-del-repository)
- [Installazione](#installazione)
- [Avvio](#avvio)
- [Configurazione](#configurazione)
- [Dati iniziali](#dati-iniziali)
- [Sicurezza](#sicurezza)
- [Possibili sviluppi](#possibili-sviluppi)
- [Note tecniche](#note-tecniche)

---

# Obiettivi

Gli obiettivi principali di Ordo sono:

- fornire uno spazio unico per organizzare il lavoro di un gruppo;
- rendere la Kanban il punto centrale della gestione delle attività;
- permettere di assegnare e monitorare i task;
- ridurre la necessità di ricaricare manualmente le pagine grazie agli aggiornamenti real-time;
- fornire strumenti di collaborazione integrati nel progetto;
- mantenere separate presentazione, logica applicativa e accesso ai dati;
- utilizzare un'interfaccia responsive e orientata alla semplicità d'uso.

---

# Funzionalità

## Gestione utenti

- registrazione;
- login e logout;
- autenticazione tramite cookie;
- opzione "Ricordami";
- gestione del profilo personale;
- modifica dei dati del profilo.

## Gestione progetti

- elenco dei progetti accessibili all'utente;
- ricerca e paginazione;
- creazione di un progetto;
- modifica del progetto;
- eliminazione del progetto;
- gestione dei collaboratori;
- workspace dedicato al singolo progetto.

## Board e attività

- creazione e gestione delle board;
- visualizzazione Kanban;
- quattro stati delle attività;
- creazione e modifica dei task;
- eliminazione dei task;
- assegnazione dei task ai membri del progetto;
- priorità;
- scadenze;
- drag & drop tra le colonne;
- dettaglio del task;
- commenti sui task.

## Collaborazione

- aggiunta e rimozione dei membri di progetto;
- chat interna al progetto;
- notifiche e aggiornamenti real-time;
- sincronizzazione delle modifiche tra più client tramite SignalR.

## Dashboard e attività personali

La Dashboard fornisce una panoramica del lavoro dell'utente, mentre la sezione **Le mie attività** raccoglie i task assegnati all'utente e permette di filtrarli in base allo stato.

---

# Tecnologie

## Backend

| Tecnologia | Utilizzo |
|---|---|
| **C#** | Linguaggio principale |
| **.NET 8** | Runtime e framework applicativo |
| **ASP.NET Core MVC** | Controller, routing, autenticazione e Razor Views |
| **Entity Framework Core 8** | ORM e accesso ai dati |
| **EF Core InMemory** | Database utilizzato nella configurazione attuale |
| **SignalR 8** | Comunicazione real-time |
| **Newtonsoft.Json** | Serializzazione JSON |
| **SG4MVC** | Supporto alla generazione tipizzata delle route |

## Frontend

| Tecnologia | Utilizzo |
|---|---|
| **Razor** | Rendering delle pagine server-side |
| **Vue 3** | Componenti e interazioni dinamiche |
| **TypeScript** | Logica frontend tipizzata, in particolare Kanban |
| **JavaScript** | Logica client e workspace |
| **Bootstrap 5** | Layout e componenti UI |
| **SCSS/CSS** | Personalizzazione grafica |
| **Font Awesome** | Icone |
| **Toastify** | Messaggi e notifiche visuali |
| **Vue Multiselect** | Selezione dinamica di utenti e filtri |

## Strumenti

- Git e GitHub;
- Figma per la progettazione UI/UX;
- .NET SDK;
- Node.js e npm;
- IDE compatibili con .NET, ad esempio Rider, Visual Studio o Visual Studio Code.

---

# Architettura

Ordo utilizza un'architettura web basata su **ASP.NET Core MVC**, organizzata secondo la struttura Feature/Area del template Unibo utilizzato per il progetto.

A livello applicativo, le operazioni di lettura e modifica sono separate in **Query e Command**, gestite attraverso `SharedService`.

È quindi più corretto parlare di una **separazione Command/Query (CQRS leggero)** piuttosto che di una implementazione CQRS completa con bus separati.

```text
┌────────────────────────────────────────────────────────┐
│                    PRESENTATION                        │
│                     Ordo.Web                           │
│                                                        │
│  Razor Views                                           │
│  MVC Controllers                                       │
│  ViewModel                                             │
│  Vue / TypeScript / JavaScript                         │
│  SignalR Hub                                           │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│                 APPLICATION / DOMAIN                   │
│                       Ordo                             │
│                                                        │
│  Commands                                              │
│  Queries                                               │
│  SharedService                                         │
│  Domain entities                                       │
│  Infrastructure                                        │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│                     DATA ACCESS                        │
│                                                        │
│                 OrdoDbContext                          │
│                        │                               │
│                        ▼                               │
│             EF Core InMemory Database                  │
└────────────────────────────────────────────────────────┘
```

---

# Flusso applicativo

Una normale operazione segue questo schema:

```text
Utente
  │
  ▼
Razor / Vue / JavaScript
  │
  ▼
MVC Controller
  │
  ▼
Command / Query
  │
  ▼
SharedService
  │
  ▼
OrdoDbContext
  │
  ▼
InMemory Database
```

Per le operazioni che richiedono aggiornamenti real-time viene aggiunto il sistema di Domain Events/SignalR:

```text
Controller
    │
    ▼
SharedService
    │
    ▼
Database
    │
    ▼
Domain Event
    │
    ▼
IPublishDomainEvents
    │
    ▼
SignalR
    │
    ▼
Client interessati
```

---

# Modello dei dati

Le principali entità del dominio sono:

```text
User
 │
 ├───────────────┐
 │               │
 ▼               ▼
Project       TaskItem
 │               │
 ├── Board       ├── Comment
 │     │         │
 │     └── Task  └── AssignedUser
 │
 ├── ProjectMember
 │
 └── ProjectChatMessage
```

## User

Rappresenta un utente registrato.

Campi principali:

- `Id`;
- `Email`;
- `Password`;
- `FirstName`;
- `LastName`;
- `NickName`.

## Project

Rappresenta uno spazio di lavoro.

Campi principali:

- `Id`;
- `Nome`;
- `Descrizione`;
- `OwnerId`.

Un progetto può contenere board, membri e messaggi della chat.

## Board

Rappresenta una bacheca appartenente a un progetto.

Campi principali:

- `Id`;
- `Nome`;
- `ProjectId`.

## TaskItem

Rappresenta un'attività di lavoro.

Campi principali:

- `Id`;
- `Titolo`;
- `Descrizione`;
- `Priorita`;
- `Stato`;
- `Scadenza`;
- `BoardId`;
- `AssignedUserId`.

## Comment

Rappresenta un commento associato a un task.

Campi principali:

- `Id`;
- `Testo`;
- `DataCreazione`;
- `TaskId`;
- `UserId`.

## ProjectMember

Rappresenta la partecipazione di un utente a un progetto.

La combinazione `ProjectId + UserId` è resa univoca dal modello dati per evitare duplicazioni dello stesso collaboratore.

## ProjectChatMessage

Rappresenta un messaggio della chat associata a un progetto.

Contiene il riferimento al progetto, all'autore, il testo e la data di creazione.

---

# Autenticazione e autorizzazioni

L'autenticazione viene gestita tramite **cookie authentication** di ASP.NET Core.

Il cookie contiene le informazioni necessarie a identificare l'utente autenticato, in particolare l'identificativo e l'email.

Le pagine protette utilizzano `AuthenticatedBaseController`.

Per le operazioni sui progetti vengono effettuati controlli sul proprietario e sui membri del progetto.

In particolare, il proprietario può gestire il progetto e i suoi collaboratori, mentre gli utenti autorizzati possono operare sulle attività del progetto.

Anche il SignalR Hub richiede autenticazione e verifica l'accesso dell'utente prima di consentirgli di entrare nei gruppi di comunicazione.

---

# Pagine dell'applicazione

## Home

È la pagina pubblica dell'applicazione e rappresenta il punto di ingresso per gli utenti non autenticati.

---

## Login

Permette all'utente di:

- inserire email e password;
- effettuare il login;
- scegliere l'opzione "Ricordami";
- essere reindirizzato alla pagina richiesta dopo l'autenticazione.

---

## Registrazione

Permette di creare un nuovo account fornendo i dati richiesti dall'applicazione.

La password viene trasformata tramite `PasswordHasher` prima di essere salvata nell'entità `User`.

---

## Dashboard

La Dashboard rappresenta la panoramica personale dell'utente autenticato.

Visualizza informazioni relative alle attività, tra cui:

- task da fare;
- task in corso;
- task in revisione;
- task completati/scaduti in base ai dati disponibili;
- riepiloghi dei progetti;
- distribuzione delle scadenze nella settimana;
- attività assegnate all'utente.

---

## Progetti

La pagina Progetti mostra i progetti accessibili all'utente.

Sono disponibili:

- ricerca;
- paginazione;
- creazione;
- modifica;
- eliminazione;
- accesso al dettaglio/workspace.

---

## Workspace del progetto

Il dettaglio del progetto è stato progettato come un vero **workspace collaborativo**.

Le informazioni e le funzioni principali sono organizzate in sezioni dedicate:

```text
Progetto
├── Panoramica
├── Board
├── Persone
└── Chat
```

### Panoramica

Mostra un riepilogo del progetto e informazioni come il numero di board, task, persone e messaggi.

Il proprietario può modificare nome e descrizione.

### Board

Permette di accedere direttamente alla Kanban senza dover attraversare una serie di pagine separate.

Questa scelta è importante dal punto di vista UX perché la Kanban rappresenta il principale strumento operativo dell'applicazione.

### Persone

Mostra il proprietario e i collaboratori del progetto.

Il proprietario può aggiungere un utente tramite email e rimuovere i membri esistenti.

### Chat

Permette ai membri del progetto di comunicare attraverso una chat interna, con aggiornamenti real-time.

---

## Kanban

La Kanban è il cuore dell'applicazione.

Le attività sono organizzate in quattro colonne:

```text
┌──────────┬────────────┬────────┬────────┐
│  To Do   │ In Progress│ Review │  Done  │
└──────────┴────────────┴────────┴────────┘
```

Gli stati sono rappresentati dall'enum `TaskState`:

```csharp
public enum TaskState
{
    ToDo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3
}
```

Le card mostrano le informazioni principali del task e possono essere spostate tramite **drag & drop**.

Durante lo spostamento, il frontend aggiorna l'interfaccia e invia la richiesta al controller `KanbanController`, che utilizza `MoveTaskCommand` per aggiornare lo stato del task.

---

## Task

Un task può essere:

- creato;
- modificato;
- eliminato;
- assegnato a un membro;
- spostato tra gli stati;
- commentato.

I dati principali sono titolo, descrizione, priorità, stato, scadenza e assegnatario.

---

## Le mie attività

La sezione `Attivita` mostra i task assegnati all'utente autenticato.

È possibile filtrare le attività in base allo stato e visualizzare una lista ordinata in funzione dei dati del task e delle scadenze.

---

## Dettaglio task

Il dettaglio del task raccoglie:

- informazioni dell'attività;
- stato;
- priorità;
- descrizione;
- scadenza;
- assegnatario;
- commenti.

Gli utenti autorizzati possono aggiungere commenti e, secondo le regole implementate, eliminare i propri commenti.

---

## Profilo

La sezione Profilo consente di visualizzare e modificare le informazioni dell'utente autenticato.

---

# Kanban

La Kanban è implementata principalmente attraverso:

```text
Areas/Kanban/
├── Board.cshtml
├── BoardViewModel.cs
├── Board.ts
├── Board.js
└── KanbanController.cs
```

`Board.ts` contiene la logica frontend per la gestione dinamica della board.

Il codice utilizza Vue 3 per mantenere sincronizzato lo stato dell'interfaccia con i dati mostrati all'utente.

Il controller espone anche l'endpoint per lo spostamento delle attività.

Il flusso dello spostamento è:

```text
Drag & Drop
     │
     ▼
Vue / TypeScript
     │
     ▼
POST MoveTask
     │
     ▼
KanbanController
     │
     ▼
MoveTaskCommand
     │
     ▼
SharedService
     │
     ▼
OrdoDbContext
```

Successivamente il cambiamento può essere propagato agli altri client attraverso SignalR.

---

# Priorità e stati

Le attività utilizzano due enumerazioni principali.

## Stato

```text
ToDo
InProgress
Review
Done
```

## Priorità

La priorità è rappresentata dall'enum `Priorita` presente nell'entità `TaskItem`.

Il frontend utilizza questi valori per visualizzare e filtrare le attività.

---

# Collaborazione real-time

La comunicazione real-time è realizzata tramite **ASP.NET Core SignalR**.

L'hub principale è:

```text
/OrdoHub
```

ed è implementato dalla classe `OrdoHub`.

Il client utilizza `@microsoft/signalr`.

---

## Gruppi SignalR

Il sistema utilizza gruppi per limitare la distribuzione degli eventi ai client interessati.

Un gruppo può rappresentare:

- un utente;
- un progetto;
- una board.

Prima di entrare in un gruppo, `OrdoHub` verifica che l'utente disponga dell'accesso necessario.

---

## Eventi

Tra gli eventi gestiti dal sistema sono presenti:

```text
TaskMoved
TaskCreated
TaskUpdated
TaskDeleted

CommentAdded
CommentDeleted

UserAssigned

ProjectMemberAdded
MemberRemoved

ProjectDeleted
ProjectUpdated

BoardCreated
BoardUpdated
BoardDeleted

ProjectChatMessageAdded
TaskChangedForUser
```

Gli eventi vengono inoltrati attraverso `IPublishDomainEvents` e `SignalRPublishDomainEvents` invece di accoppiare direttamente la logica applicativa all'Hub.

---

## Riconnessione

Il frontend include `signalRConnectionManager.ts`, che gestisce la connessione e la riconnessione al server.

In caso di perdita della connessione vengono effettuati tentativi progressivi di riconnessione e, una volta ristabilito il collegamento, il client può ripristinare la propria partecipazione ai gruppi necessari.

---

# Database

## Database utilizzato

La configurazione di sviluppo utilizzata dal progetto è **Entity Framework Core InMemory**.

Quando non viene fornita una connection string, `Startup.ConfigureServices` configura:

```csharp
options.UseInMemoryDatabase(databaseName: "Ordo");
```

Di conseguenza l'applicazione può essere avviata senza installare o configurare un database esterno.

### Importante

Il database InMemory **non è persistente**.

I dati vengono mantenuti durante l'esecuzione dell'applicazione, ma vengono persi quando il processo viene terminato e il database viene ricreato al successivo avvio.

Questo rende la configurazione particolarmente adatta a:

- sviluppo;
- test;
- dimostrazione;
- valutazione del progetto.

Non deve invece essere considerata una configurazione di produzione.

---

## Provider disponibili nel codice

Il progetto contiene anche il package `Microsoft.EntityFrameworkCore.Sqlite` e il codice di `Startup` prevede un percorso alternativo per una connection string.

Questo **non cambia il fatto che la configurazione attuale senza connection string utilizzi InMemory**.

Il README considera quindi InMemory il database effettivamente utilizzato nell'esecuzione standard del progetto.

---

# Entity Framework Core

Il contesto principale è `OrdoDbContext`.

Espone i seguenti `DbSet`:

```csharp
DbSet<User> Users
DbSet<Project> Projects
DbSet<Board> Boards
DbSet<TaskItem> Tasks
DbSet<Comment> Comments
DbSet<ProjectMember> ProjectMembers
DbSet<ProjectChatMessage> ProjectChatMessages
```

Le relazioni principali sono configurate nel `OnModelCreating`.

Sono presenti relazioni con comportamenti `Cascade` e `Restrict` in base alla responsabilità dell'entità collegata.

In particolare, gli oggetti appartenenti gerarchicamente a un progetto possono essere eliminati insieme al progetto, mentre i riferimenti verso gli utenti utilizzano restrizioni per evitare cancellazioni a cascata indesiderate.

---

# Migration

Nel progetto sono presenti le migration di Entity Framework Core.

`Program.cs` verifica il provider utilizzato:

```csharp
if (context.Database.IsRelational())
{
    context.Database.Migrate();
}
else
{
    context.Database.EnsureCreated();
}
```

Quindi:

- con un provider relazionale vengono applicate le migration;
- con InMemory viene utilizzato `EnsureCreated()`.

Le migration sono quindi presenti nel repository, ma **non vengono utilizzate nella normale esecuzione InMemory**.

---

# Struttura del repository

```text
Progetto-Interfaccia-Uomo-Macchina/
│
├── README.md
│
└── src/
    ├── Ordo.sln
    ├── NuGet.Config
    │
    ├── Ordo/
    │   ├── Design/
    │   ├── Infrastructure/
    │   │   ├── DataGenerator.cs
    │   │   ├── EmailAlreadyExistException.cs
    │   │   ├── LoginException.cs
    │   │   ├── Paging.cs
    │   │   └── PasswordHasher.cs
    │   │
    │   ├── Migrations/
    │   │
    │   └── Services/
    │       ├── _OrdoDbContext.cs
    │       └── Shared/
    │           ├── User.cs
    │           ├── User.Commands.cs
    │           ├── User.Queries.cs
    │           ├── Project.cs
    │           ├── Project.Commands.cs
    │           ├── Project.Queries.cs
    │           ├── Board.cs
    │           ├── Board.Commands.cs
    │           ├── Board.Queries.cs
    │           ├── TaskItem.cs
    │           ├── TaskItem.Commands.cs
    │           ├── TaskItem.Queries.cs
    │           ├── Comment.cs
    │           ├── Comment.Commands.cs
    │           ├── Comment.Queries.cs
    │           ├── ProjectMember.cs
    │           ├── ProjectMember.Commands.cs
    │           ├── ProjectMember.Queries.cs
    │           ├── ProjectChatMessage.cs
    │           ├── ProjectChat.Commands.cs
    │           ├── ProjectChat.Queries.cs
    │           └── _SharedService.cs
    │
    └── Ordo.Web/
        ├── Areas/
        │   ├── AuthenticatedBaseController.cs
        │   ├── IdentitaViewModel.cs
        │   ├── Kanban/
        │   ├── Progetti/
        │   ├── Tasks/
        │   └── _ViewImports.cshtml
        │
        ├── Features/
        │   ├── Home/
        │   ├── Login/
        │   ├── Registrazione/
        │   ├── Dashboard/
        │   ├── Attivita/
        │   └── Profile/
        │
        ├── SignalR/
        │   ├── IPublishDomainEvents.cs
        │   ├── SignalRPublishDomainEvents.cs
        │   └── Hubs/
        │       ├── OrdoHub.cs
        │       └── Events/
        │
        ├── Views/
        │   └── Shared/
        │
        ├── wwwroot/
        │   ├── css/
        │   └── js/
        │
        ├── Program.cs
        ├── Startup.cs
        ├── Container.cs
        ├── AppSettings.cs
        ├── package.json
        └── tsconfig.json
```

---

# Commands e Queries

Le operazioni sul dominio sono organizzate in file separati per entità.

Esempi di Command:

```text
AddOrUpdateProjectCommand
DeleteProjectCommand

AddOrUpdateBoardCommand
DeleteBoardCommand

AddOrUpdateTaskCommand
MoveTaskCommand
DeleteTaskCommand

AddCommentCommand
DeleteCommentCommand

AddProjectMemberCommand
RemoveProjectMemberCommand

AddProjectChatMessageCommand

RegisterUserCommand
AddOrUpdateUserCommand
```

Esempi di Query:

```text
ProjectsIndexQuery
ProjectDetailQuery

BoardsByProjectQuery
BoardDetailQuery

TasksByBoardQuery
TaskDetailQuery

CommentsByTaskQuery

ProjectMembersQuery
ProjectChatMessagesQuery

UserDetailQuery
UsersSelectQuery
CheckLoginCredentialsQuery
```

Le operazioni vengono centralizzate in `SharedService`, mantenendo le entità e le operazioni sul dominio organizzate per feature.

---

# Frontend

Il progetto utilizza un approccio ibrido.

Le pagine vengono inizialmente renderizzate tramite **Razor**, mentre le parti che richiedono elevata interattività vengono gestite lato client.

```text
ASP.NET Core MVC
       │
       ├── Razor
       │
       └── Vue 3
             │
             ├── TypeScript
             └── JavaScript
```

Questa scelta evita di trasformare l'intera applicazione in una SPA e consente di utilizzare Vue dove porta un vantaggio concreto, soprattutto nella Kanban e nel workspace del progetto.

---

# Configurazione frontend

Le dipendenze JavaScript sono definite in `Ordo.Web/package.json`.

Le principali sono:

```text
vue
@ microsoft/signalr
bootstrap
@fortawesome/fontawesome-free
toastify-js
vue-multiselect
```

Il progetto utilizza TypeScript tramite `tsconfig.json`.

Il target configurato per TypeScript è `ES2019`.

---

# Installazione

## Requisiti

Sono necessari:

- **.NET 8 SDK**;
- **Node.js**;
- **npm**;
- Git, se si desidera clonare il repository.

Non è necessario installare un server SQL per la configurazione standard del progetto, poiché viene utilizzato il database InMemory.

---

## Clonazione

```bash
git clone <repository-url>
cd Progetto-Interfaccia-Uomo-Macchina/src
```

---

## Ripristino dei pacchetti .NET

```bash
dotnet restore
```

---

## Installazione delle dipendenze frontend

```bash
cd Ordo.Web
npm install
```

---

# Avvio

Dalla directory `src` è possibile avviare il progetto con:

```bash
dotnet run --project Ordo.Web
```

In alternativa è possibile avviarlo dall'IDE tramite il progetto `Ordo.Web`.

All'avvio ASP.NET Core indica nella console l'indirizzo HTTP/HTTPS sul quale è disponibile l'applicazione.

---

# Dati iniziali

All'avvio `Program.Main` esegue:

```csharp
DataGenerator.InitializeUsers(context);
```

Il `DataGenerator` inserisce gli utenti iniziali se il database non contiene già utenti.

Poiché il database standard è InMemory, il seeding viene rieseguito dopo una nuova inizializzazione del database.

---

# Sicurezza

Il progetto include diversi meccanismi di protezione:

- autenticazione tramite cookie;
- controller autenticati tramite `AuthenticatedBaseController`;
- `[Authorize]` sull'Hub SignalR;
- verifica dell'accesso ai gruppi SignalR;
- controllo della proprietà dei progetti;
- controllo dell'appartenenza ai progetti;
- antiforgery configurato in ASP.NET Core;
- hashing delle password tramite `PasswordHasher`.

La password viene trasformata prima della memorizzazione e verificata tramite `PasswordHasher.Verify` durante il login.

Per un'applicazione di produzione sarebbe comunque preferibile utilizzare un password hashing adattivo dedicato, come ASP.NET Core Identity/PBKDF2, Argon2 o bcrypt.

---

# Possibili sviluppi

Il progetto può essere esteso in diverse direzioni.

## Persistenza reale

Sostituire il database InMemory con un database persistente, ad esempio SQLite o SQL Server, mantenendo Entity Framework Core come livello di accesso ai dati.

## Kanban

Possibili miglioramenti:

- ordine persistente delle card;
- colonne personalizzabili;
- etichette;
- sotto-task;
- allegati;
- storico delle modifiche;
- archiviazione dei task.

## Chat

Possibili estensioni:

- modifica dei messaggi;
- eliminazione;
- ricerca;
- paginazione;
- indicatori di lettura;
- stato online degli utenti.

## Notifiche

Le notifiche potrebbero essere persistite nel database, così da renderle disponibili anche dopo il logout o il cambio dispositivo.

## Autenticazione

L'autenticazione potrebbe essere portata a un sistema completo basato su ASP.NET Core Identity, con gestione strutturata di password, ruoli, reset password e account.

---

# Note tecniche

## Database InMemory

La configurazione standard è volutamente semplice: non è richiesto alcun servizio esterno per il database.

Questo comporta però la perdita dei dati al riavvio dell'applicazione.

## Migration

Le migration presenti nel progetto sono utili qualora si utilizzi un provider relazionale, ma non rappresentano il meccanismo di persistenza utilizzato durante l'esecuzione standard con InMemory.

## Password seed

Il repository contiene dati seed per gli utenti di sviluppo. Gli hash presenti nel `DataGenerator` devono essere mantenuti coerenti con l'algoritmo utilizzato da `PasswordHasher`.

## Antiforgery

Il progetto configura l'anti-forgery con l'header `RequestVerificationToken`. Le operazioni POST dovrebbero mantenere una politica coerente di validazione del token, soprattutto se l'applicazione dovesse essere portata in produzione.

---

# Conclusione

Ordo è una piattaforma di project management collaborativo che combina una struttura MVC tradizionale con strumenti frontend dinamici e comunicazione real-time.

Il nucleo dell'applicazione è la **Kanban**, attorno alla quale vengono organizzate le attività del progetto. Il workspace del progetto permette di accedere rapidamente alla board, ai collaboratori e alla chat, riducendo la frammentazione della navigazione.

Dal punto di vista tecnico il progetto combina:

```text
ASP.NET Core MVC
        +
Entity Framework Core
        +
InMemory Database
        +
Vue 3
        +
TypeScript / JavaScript
        +
SignalR
        +
Bootstrap
```

L'architettura separa la presentazione dalla logica applicativa e dall'accesso ai dati, utilizzando Command e Query attraverso `SharedService` e un sistema di Domain Events per propagare le modifiche real-time.

La configurazione InMemory rende il progetto immediatamente eseguibile e adatto a sviluppo e dimostrazione, mentre la struttura del codice permette in futuro di introdurre una persistenza relazionale senza dover riscrivere l'intera applicazione.
