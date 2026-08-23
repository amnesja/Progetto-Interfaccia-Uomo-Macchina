using System;
using Ordo.Services.Shared;

namespace Ordo.Web.SignalR.Hubs.Events
{
    // IdGroup = BoardId: ogni evento viene notificato solo a chi sta guardando quella board

    public class TaskMovedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public TaskState NuovoStato { get; set; }
        public string Titolo { get; set; }
    }

    public class TaskCreatedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public string Titolo { get; set; }
        public int Priorita { get; set; }
        public int Stato { get; set; }
        public DateTime? Scadenza { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
    }

    public class TaskDeletedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public string Titolo { get; set; }
    }

    public class TaskUpdatedEvent
    {
        public TaskCreatedEvent Task { get; set; }
        public bool IsAssignmentChanged { get; set; }
    }

    public class CommentAddedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public Guid CommentId { get; set; }
        public string Titolo { get; set; }
    }

    public class UserAssignedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid TaskId { get; set; }
        public Guid? UserId { get; set; }
        public string AssignedUserName { get; set; }
        public string Titolo { get; set; }
    }

    // IdGroup = UserId del destinatario: notifica quell'utente ovunque si trovi
    // (in particolare se ha aperta la pagina "I miei progetti")
    public class MemberAddedEvent
    {
        public Guid IdGroup { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectNome { get; set; }
        public string ProjectDescrizione { get; set; }
    }

    // Notifica sia chi guarda il Dettaglio del progetto (gruppo = ProjectId)
    // sia ciascun utente coinvolto (owner + collaboratori), per aggiornare
    // la loro pagina "I miei progetti" anche se non erano dentro quel progetto
    public class ProjectDeletedEvent
    {
        public Guid ProjectId { get; set; }
        public Guid[] UtentiCoinvolti { get; set; }
    }

    // IdGroup implicito = ProjectId: notifica chi guarda il Dettaglio del progetto
    public class BoardCreatedEvent
    {
        public Guid ProjectId { get; set; }
        public Guid BoardId { get; set; }
        public string BoardNome { get; set; }
    }

    // Notifica sia chi guarda il Dettaglio del progetto (gruppo = ProjectId)
    // sia chi è dentro quella specifica board sulla Kanban (gruppo = BoardId)
    public class BoardDeletedEvent
    {
        public Guid ProjectId { get; set; }
        public Guid BoardId { get; set; }
        public string BoardNome { get; set; }
    }

    // IdGroup implicito = ProjectId: notifica chi guarda il Dettaglio del progetto.
    // Inviato anche ai singoli utenti coinvolti, per aggiornare la loro pagina "I miei progetti"
    public class ProjectUpdatedEvent
    {
        public Guid ProjectId { get; set; }
        public string Nome { get; set; }
        public string Descrizione { get; set; }
        public Guid[] UtentiCoinvolti { get; set; }
    }

    // IdGroup implicito = ProjectId: notifica chi guarda il Dettaglio del progetto
    public class BoardUpdatedEvent
    {
        public Guid ProjectId { get; set; }
        public Guid BoardId { get; set; }
        public string BoardNome { get; set; }
    }

    // IdGroup = UserId destinatario. Notifica Dashboard e "Le mie attività" di quell'utente
    // quando gli viene assegnato/tolto un task, o quando un suo task viene modificato/eliminato
    public class TaskChangedForUserEvent
    {
        public Guid IdGroup { get; set; }
        public string Tipo { get; set; }   // "Assigned" | "Unassigned" | "Updated" | "Deleted"
        public string Titolo { get; set; }
        public string ProjectNome { get; set; }
        public Guid ProjectId { get; set; }
        public Guid BoardId { get; set; }
    }

    // Notifica sia la pagina "I miei progetti" dell'utente rimosso (gruppo = UserId)
    // sia chi guarda il Dettaglio del progetto, incluso l'utente stesso se ci si trova dentro (gruppo = ProjectId)
    public class MemberRemovedEvent
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
    }
}