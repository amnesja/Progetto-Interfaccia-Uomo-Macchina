/// <reference path="../../node_modules/vue/dist/vue.d.ts" />

declare const Vue: any;
namespace Ordo.Kanban {

    interface TaskCard {
        id: string;
        titolo: string;
        priorita: number;
        stato: number;
        scadenza: string | null;
        assignedUserId: string | null;
        assignedUserName: string | null;
    }

    interface BoardSeed {
        boardId: string;
        boardName: string;
        projectId: string;
        tasks: TaskCard[];
    }

    interface Column {
        stato: number;
        titolo: string;
    }

    const COLUMNS: Column[] = [
        { stato: 0, titolo: "Da fare" },
        { stato: 1, titolo: "In corso" },
        { stato: 2, titolo: "Review" },
        { stato: 3, titolo: "Done" }
    ];

    interface BoardData {
        tasks: TaskCard[];
        columns: Column[];
        draggedTask: TaskCard | null;
    }

    export function createBoardApp(seed: BoardSeed, moveTaskUrl: string) {
        const component = Vue.defineComponent({
            data(): BoardData {
                return {
                    tasks: seed.tasks,
                    columns: COLUMNS,
                    draggedTask: null
                };
            },
            methods: {
                tasksByStato(stato: number): TaskCard[] {
                    return this.tasks.filter(t => t.stato === stato);
                },
                formatDate(dateStr: string): string {
                    const d = new Date(dateStr);
                    return d.toLocaleDateString("it-IT", { day: "2-digit", month: "2-digit" });
                },
                addTask(taskEvent: {
                    taskId: string;
                    titolo: string;
                    priorita: number;
                    stato: number;
                    scadenza: string | null;
                    assignedUserId: string | null;
                    assignedUserName: string | null;
                }) {
                    if (this.tasks.some(task => task.id === taskEvent.taskId)) return;

                    this.tasks.push({
                        id: taskEvent.taskId,
                        titolo: taskEvent.titolo,
                        priorita: taskEvent.priorita,
                        stato: taskEvent.stato,
                        scadenza: taskEvent.scadenza,
                        assignedUserId: taskEvent.assignedUserId,
                        assignedUserName: taskEvent.assignedUserName
                    });
                },
                updateTask(taskEvent: {
                    taskId: string;
                    titolo: string;
                    priorita: number;
                    stato: number;
                    scadenza: string | null;
                    assignedUserId: string | null;
                    assignedUserName: string | null;
                }) {
                    const task = this.tasks.find(task => task.id === taskEvent.taskId);
                    if (!task) {
                        this.addTask(taskEvent);
                        return;
                    }

                    task.titolo = taskEvent.titolo;
                    task.priorita = taskEvent.priorita;
                    task.stato = taskEvent.stato;
                    task.scadenza = taskEvent.scadenza;
                    task.assignedUserId = taskEvent.assignedUserId;
                    task.assignedUserName = taskEvent.assignedUserName;
                },
                removeTask(taskId: string) {
                    const index = this.tasks.findIndex(task => task.id === taskId);
                    if (index >= 0) this.tasks.splice(index, 1);
                },
                updateTaskAssignee(taskId: string, assignedUserId: string | null, assignedUserName: string | null) {
                    const task = this.tasks.find(task => task.id === taskId);
                    if (!task) return;

                    task.assignedUserId = assignedUserId;
                    task.assignedUserName = assignedUserName;
                },
                onDragStart(task: TaskCard) {
                    this.draggedTask = task;
                },
                async onDrop(nuovoStato: number) {
                    if (!this.draggedTask) return;
                    const task = this.draggedTask;
                    this.draggedTask = null;

                    await this.moveTask(task, nuovoStato);
                },
                async moveTaskFromSelect(task: TaskCard, event: Event) {
                    const select = event.target as HTMLSelectElement;
                    await this.moveTask(task, Number(select.value));
                },
                async moveTask(task: TaskCard, nuovoStato: number) {

                    if (task.stato === nuovoStato) return;

                    const statoPrecedente = task.stato;
                    task.stato = nuovoStato; // aggiornamento ottimistico

                    try {
                        const antiforgeryToken = document.querySelector<HTMLInputElement>("input[name='__RequestVerificationToken']")?.value;
                        const response = await fetch(moveTaskUrl, {
                            method: "POST",
                            headers: {
                                "Content-Type": "application/json",
                                "RequestVerificationToken": antiforgeryToken ?? ""
                            },
                            body: JSON.stringify({ taskId: task.id, nuovoStato: nuovoStato })
                        });

                        if (!response.ok) {
                            throw new Error("Richiesta fallita: " + response.status);
                        }
                    } catch (err) {
                        task.stato = statoPrecedente; // rollback se il salvataggio fallisce
                        console.error("Impossibile spostare il task", err);
                        alert("Non è stato possibile salvare lo spostamento. Riprova.");
                    }
                },
                openTaskDetail(taskId: string) {
                    window.location.href = "/Tasks/Dettaglio/" + taskId;
                }
            }
        });

        return Vue.createApp(component);
    }

    // Chiamato quando arriva un evento "TaskMoved" via SignalR da un ALTRO utente collegato
    // alla stessa board: aggiorna lo stato reattivo senza bisogno di ricaricare la pagina.
    export function applyRemoteMove(kanbanVm: { tasks: TaskCard[] }, taskId: string, nuovoStato: number) {
        const task = kanbanVm.tasks.find(t => t.id === taskId);
        if (task) {
            task.stato = nuovoStato;
        }
    }
}
