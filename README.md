# Proposta di Progetto: Ordo

## Piattaforma Collaborativa per la Gestione di Progetti e Task

---

## 1. Componenti del gruppo

- **Naman Bagga** — Email: naman.bagga@studio.unibo.it
- **Salvatore Persico** — Email: salvatore.persico4@studio.unibo.it

---

## 2. Descrizione generale

Ordo è una piattaforma web per la gestione collaborativa di progetti e attività basata sulla metodologia Kanban. Il sistema permette a gruppi di utenti di organizzare il lavoro in progetti, suddividere le attività in task, monitorarne lo stato di avanzamento e collaborare in tempo reale.

L'applicazione è pensata specificamente per contesti universitari e di team working, con un focus mirato su organizzazione del lavoro, collaborazione tra utenti, aggiornamenti in tempo reale e visualizzazione chiara dello stato dei progetti.

---

## 3. Obiettivi del progetto

- Realizzare una piattaforma full-stack moderna, scalabile e manutenibile seguendo le linee guida didattiche.
- Implementare un sistema di gestione task collaborativo basato su lavagna Kanban.
- Sfruttare comunicazioni bidirezionali in tempo reale guidate dagli eventi di dominio tramite SignalR.
- Integrare un frontend dinamico e reattivo mediante l'utilizzo di Vue.js e TypeScript.
- Applicare l'architettura Clean/Vertical Slice del template Unibo con netta separazione dei livelli.
- Progettare un'interfaccia UI/UX moderna e responsive tramite Figma.

---

## 4. Tecnologie utilizzate

| Tecnologia | Utilizzo nel progetto |
|---|---|
| **ASP.NET Core MVC** | Infrastruttura web, routing, gestione delle aree protette e rendering delle viste Razor di base. |
| **Entity Framework Core** | ORM (Object-Relational Mapping) per l'accesso e la persistenza dei dati sul database. |
| **SQL Server** | Database relazionale per la memorizzazione di utenti, progetti, board, task e commenti. |
| **Vue.js** | Framework frontend progressivo per la gestione dinamica della Kanban board e dei componenti reattivi. |
| **SignalR** | Libreria per la comunicazione real-time e la sincronizzazione immediata delle board tra i client connessi. |
| **Bootstrap 5 & SCSS** | Framework CSS e fogli di stile personalizzati per la realizzazione di un layout responsive e moderno. |
| **TypeScript** | Linguaggio tipizzato per la logica frontend, integrato nativamente nel processo di build del template. |
| **Figma** | Strumento di design per la prototipazione dell'interfaccia utente (UI/UX). |
| **GitHub** | Sistema di controllo versione del codice e collaborazione remota. |

---

## 5. Architettura del sistema

Il progetto adotta rigorosamente l'architettura a livelli fornita dal template Unibo, integrando logiche di CQRS (Commands/Queries) direttamente nello strato dei servizi di dominio, strutturati per feature:

```
┌────────────────────────────────────────────────────────┐
│                      PRESENTATION                       │
│                    (Ordo.Web - MVC)                     │
│  - Features/ (Pagine pubbliche: Home, Login)            │
│  - Areas/ (Pagine protette: Progetti, Task con Vue.js)   │
│  - SignalR Hub (OrdoHub e messagistica real-time)        │
└───────────────────────────┬───────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                      APPLICATION                        │
│                         (Ordo)                          │
│  - Services/ (Logica organizzata per macro-entità)       │
│  - Commands & Queries (CQRS integrato nel dominio)       │
│  - Infrastructure (DataGenerator, Paging, ecc.)          │
└───────────────────────────┬───────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                       DATA LAYER                         │
│  - Entity Framework Core (_OrdoDbContext)                │
│  - SQL Server Database                                   │
└────────────────────────────────────────────────────────┘
```

---

## 6. Flusso dei dati e dell'applicazione

L'interazione dell'utente segue un flusso lineare e strutturato, dove il frontend (Vue.js) comunica con i controller dedicati, i quali espongono le operazioni ai servizi CQRS sottostanti:

```
Utente ──► View (Razor + Vue.js / TypeScript) ──► Controller (MVC Area)
       ──► Service (CQRS Command/Query) ──► EF Core DbContext ──► SQL Server
```

---

## 7. Modello concettuale dei dati (ER Diagram Semplificato)

```
[User]
  │
  ├──< crea/possiede >── [Project]
                            │
                            └──< contiene >── [Board]
                                                │
                                                └──< contiene >── [Task]
                                                                    │
                                                                    ├──< ha >── [Comment]
                                                                    └──< assegnato a >── [User]
```

---

## 8. Struttura delle entità principali

- **User**: Id, Nome, Email, PasswordHash
- **Project**: Id, Nome, Descrizione, OwnerId
- **Board**: Id, ProjectId, Nome
- **Task**: Id, Titolo, Descrizione, Priorità, Stato (To Do / In Progress / Review / Done), Scadenza, AssignedUserId
- **Comment**: Id, Testo, TaskId, UserId, DataCreazione

---

## 9. Funzionalità principali

### 9.1 Gestione utenti

- Registrazione nuovo profilo, Login sicuro e Logout (gestiti all'interno della cartella `Features/Login`).
- Profilo personale contenente il riepilogo delle attività assegnate.

### 9.2 Gestione progetti

- Creazione e configurazione di un nuovo spazio di lavoro (Project).
- Invito e gestione dei membri abilitati ad accedere alla board (Accesso multi-utente).

### 9.3 Kanban Board (Vue.js + TypeScript)

- Visualizzazione dinamica dei task suddivisi per colonne di stato (To Do, In Progress, Review, Done).
- Interazione fluida tramite Drag & Drop delle card da una colonna all'altra.
- Aggiornamento istantaneo dell'interfaccia utente senza ricaricamento della pagina.

### 9.4 Task Management

- Creazione di un task con definizione di titolo, descrizione, livello di priorità e data di scadenza.
- Assegnazione del task a uno specifico membro del progetto.
- Sezione commenti interna al dettaglio del task per favorire la comunicazione asincrona.

### 9.5 Collaborazione real-time (SignalR)

- Sincronizzazione istantanea della Kanban Board: se l'Utente A sposta un task, la card si muove in tempo reale anche sulla schermata dell'Utente B.
- Invio e ricezione immediata di notifiche live in-app per le azioni chiave sul progetto.

---

## 10. Architettura Real-Time e Domain Events

In perfetta aderenza con l'infrastruttura del template dei docenti, il sistema non invocherà direttamente l'Hub di SignalR dai controller. Verrà invece utilizzato il pattern dei Domain Events tramite l'interfaccia `IPublishDomainEvents`. Quando un servizio esegue un comando di modifica, viene pubblicato un evento che `SignalRPublishDomainEvents` intercetta, traducendolo in un messaggio WebSocket per i client interessati:

```
Utente A (Azione) ──► Comando (Service) ──► Domain Event ──►
IPublishDomainEvents ──► OrdoHub ──► Broadcast a Utenti del Progetto
```

**Eventi gestiti:** `TaskCreated`, `TaskUpdated`, `TaskMoved`, `CommentAdded`, `UserAssigned`.

---

## 11. Struttura del Repository (Allineata al Template Unibo)

La disposizione dei file rispecchia la struttura Feature-Driven del template accademico, dove le componenti backend CQRS sono unite per entità nei servizi e le componenti web (Controller, ViewModel, Viste e script TypeScript) sono co-locate all'interno delle rispettive cartelle di Area:

```
Ordo/
│
└── src/
    ├── Ordo.sln
    ├── NuGet.Config
    │
    ├── Ordo/  <-- (Progetto Core / Logica e Dati)
    │   ├── Infrastructure/
    │   │   ├── DataGenerator.cs
    │   │   ├── LoginException.cs
    │   │   └── Paging.cs
    │   └── Services/
    │       ├── _OrdoDbContext.cs
    │       └── Shared/
    │           ├── _SharedService.cs
    │           ├── User.cs (Entità + User.Commands.cs + User.Queries.cs)
    │           ├── Project.cs (Entità + Proj.Commands.cs + Proj.Queries.cs)
    │           ├── Board.cs (Entità + Board.Commands.cs + Board.Queries.cs)
    │           └── TaskItem.cs (Entità + Task.Commands.cs + Task.Queries.cs)
    │
    └── Ordo.Web/  <-- (Progetto Presentazione / Web MVC)
        ├── Program.cs / Startup.cs / Container.cs / AppSettings.cs
        │
        ├── Areas/  <-- (Pagine e pannelli sotto autenticazione)
        │   ├── AuthenticatedBaseController.cs
        │   ├── IdentitaViewModel.cs
        │   │
        │   ├── Progetti/  <-- (Gestione Spazi di Lavoro)
        │   │   ├── ProgettiController.cs
        │   │   ├── Index.cshtml / IndexViewModel.cs
        │   │   └── Dettaglio.cshtml
        │   │
        │   └── Kanban/  <-- (Bacheca collaborativa di progetto)
        │       ├── KanbanController.cs
        │       ├── Board.cshtml / BoardViewModel.cs
        │       ├── Board.ts  <-- (Logica Vue.js e interazione in TypeScript)
        │       ├── Board.js  <-- (Compilato automaticamente)
        │       └── Board.js.map
        │
        ├── Features/  <-- (Pagine pubbliche o di sistema)
        │   ├── Home/ (HomeController.cs, Index.cshtml)
        │   └── Login/ (LoginController.cs, LoginViewModel.cs, Login.cshtml)
        │
        ├── SignalR/  <-- (Infrastruttura Real-time e gestione Eventi)
        │   ├── IPublishDomainEvents.cs
        │   ├── SignalRPublishDomainEvents.cs
        │   └── Hubs/
        │       ├── OrdoHub.cs
        │       └── Events/OrdoEvents.cs
        │
        └── wwwroot/  <-- (Asset statici e bundle JavaScript/CSS globali)
            ├── css/ (site.scss, site.css, site.min.css)
            └── js/ (site.ts, signalRConnectionManager.ts, bundle-vue.js, bundle-signalr.js)
```

---

## 12. Mockup Figma (Interfacce da progettare)

1. **Login / Registrazione**: Schermata di accesso sicuro e form di iscrizione alla piattaforma.
2. **Dashboard Progetti**: Schermata principale post-login contenente la griglia dei progetti attivi, l'elenco dei membri e le statistiche rapide di avanzamento complessivo.
3. **Kanban Board**: Schermata cardine del sistema, con la visualizzazione a quattro colonne, le card dei task interattive e i filtri di ricerca rapidi.
4. **Dettaglio Task (Modal)**: Finestra popup attivabile cliccando su una card per visualizzare e modificare la descrizione completa, cambiare la priorità, impostare le scadenze e inserire i commenti.
5. **Profilo Utente**: Spazio dedicato alla modifica delle informazioni personali e alla visualizzazione focalizzata di tutti i task assegnati nei vari progetti.
6. **Dashboard Amministratore**: Pannello di controllo globale per il monitoraggio degli utenti registrati, moderazione dei contenuti e statistiche di utilizzo del sistema.

---

## 13. Risultati attesi e Conclusione

Il progetto mira a dimostrare piene competenze nello sviluppo full-stack moderno applicando le metodologie consolidate del software aziendale, ovvero:

- Un'architettura solida basata sulla separazione tra dominio e presentazione (Vertical Slice).
- Gestione efficace e ottimizzata della persistenza tramite Entity Framework Core.
- Un frontend reattivo guidato da componenti Vue.js e irrobustito dal controllo statico di TypeScript, integrato in armonia con le viste Razor.
- Sincronizzazione in tempo reale trasparente per l'utente finale mediante SignalR e l'infrastruttura a eventi.

Ordo si propone come una piattaforma di project management completa, scalabile ed efficiente, nata e strutturata per sfruttare al 100% le potenzialità fornite dall'ecosistema del template didattico Unibo.