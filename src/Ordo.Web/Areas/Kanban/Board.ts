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
        assignedUserNickName: string | null;
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
                onDragStart(task: TaskCard) {
                    this.draggedTask = task;
                },
                async onDrop(nuovoStato: number) {
                    if (!this.draggedTask) return;
                    const task = this.draggedTask;
                    this.draggedTask = null;

                    if (task.stato === nuovoStato) return;

                    const statoPrecedente = task.stato;
                    task.stato = nuovoStato; // aggiornamento ottimistico

                    try {
                        const response = await fetch(moveTaskUrl, {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
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