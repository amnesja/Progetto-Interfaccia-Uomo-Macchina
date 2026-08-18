/// <reference path="../../../node_modules/vue/dist/vue.d.ts" />

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

    const COLUMNS = [
        { stato: 0, titolo: "Da fare" },
        { stato: 1, titolo: "In corso" },
        { stato: 2, titolo: "Review" },
        { stato: 3, titolo: "Done" }
    ];

    export function createBoardApp(seed: BoardSeed, moveTaskUrl: string) {
        return Vue.createApp({
            data() {
                return {
                    tasks: seed.tasks as TaskCard[],
                    columns: COLUMNS,
                    draggedTask: null as TaskCard | null
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
                    task.stato = nuovoStato;

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
                        task.stato = statoPrecedente;
                        console.error("Impossibile spostare il task", err);
                        alert("Non è stato possibile salvare lo spostamento. Riprova.");
                    }
                }
            }
        });
    }
}