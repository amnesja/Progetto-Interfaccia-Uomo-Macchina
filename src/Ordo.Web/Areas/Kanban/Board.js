"use strict";
/// <reference path="../../node_modules/vue/dist/vue.d.ts" />
var Ordo;
(function (Ordo) {
    var Kanban;
    (function (Kanban) {
        const COLUMNS = [
            { stato: 0, titolo: "Da fare" },
            { stato: 1, titolo: "In corso" },
            { stato: 2, titolo: "Review" },
            { stato: 3, titolo: "Done" }
        ];
        function createBoardApp(seed, moveTaskUrl) {
            const component = Vue.defineComponent({
                data() {
                    return {
                        tasks: seed.tasks,
                        columns: COLUMNS,
                        draggedTask: null
                    };
                },
                methods: {
                    tasksByStato(stato) {
                        return this.tasks.filter(t => t.stato === stato);
                    },
                    formatDate(dateStr) {
                        const d = new Date(dateStr);
                        return d.toLocaleDateString("it-IT", { day: "2-digit", month: "2-digit" });
                    },
                    addTask(taskEvent) {
                        if (this.tasks.some(task => task.id === taskEvent.taskId))
                            return;
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
                    updateTask(taskEvent) {
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
                    removeTask(taskId) {
                        const index = this.tasks.findIndex(task => task.id === taskId);
                        if (index >= 0)
                            this.tasks.splice(index, 1);
                    },
                    updateTaskAssignee(taskId, assignedUserId, assignedUserName) {
                        const task = this.tasks.find(task => task.id === taskId);
                        if (!task)
                            return;
                        task.assignedUserId = assignedUserId;
                        task.assignedUserName = assignedUserName;
                    },
                    onDragStart(task) {
                        this.draggedTask = task;
                    },
                    async onDrop(nuovoStato) {
                        if (!this.draggedTask)
                            return;
                        const task = this.draggedTask;
                        this.draggedTask = null;
                        await this.moveTask(task, nuovoStato);
                    },
                    async moveTaskFromSelect(task, event) {
                        const select = event.target;
                        await this.moveTask(task, Number(select.value));
                    },
                    async moveTask(task, nuovoStato) {
                        var _a;
                        if (task.stato === nuovoStato)
                            return;
                        const statoPrecedente = task.stato;
                        task.stato = nuovoStato; // aggiornamento ottimistico
                        try {
                            const antiforgeryToken = (_a = document.querySelector("input[name='__RequestVerificationToken']")) === null || _a === void 0 ? void 0 : _a.value;
                            const response = await fetch(moveTaskUrl, {
                                method: "POST",
                                headers: {
                                    "Content-Type": "application/json",
                                    "RequestVerificationToken": antiforgeryToken !== null && antiforgeryToken !== void 0 ? antiforgeryToken : ""
                                },
                                body: JSON.stringify({ taskId: task.id, nuovoStato: nuovoStato })
                            });
                            if (!response.ok) {
                                throw new Error("Richiesta fallita: " + response.status);
                            }
                        }
                        catch (err) {
                            task.stato = statoPrecedente; // rollback se il salvataggio fallisce
                            console.error("Impossibile spostare il task", err);
                            alert("Non è stato possibile salvare lo spostamento. Riprova.");
                        }
                    },
                    openTaskDetail(taskId) {
                        window.location.href = "/Tasks/Dettaglio/" + taskId;
                    }
                }
            });
            return Vue.createApp(component);
        }
        Kanban.createBoardApp = createBoardApp;
        // Chiamato quando arriva un evento "TaskMoved" via SignalR da un ALTRO utente collegato
        // alla stessa board: aggiorna lo stato reattivo senza bisogno di ricaricare la pagina.
        function applyRemoteMove(kanbanVm, taskId, nuovoStato) {
            const task = kanbanVm.tasks.find(t => t.id === taskId);
            if (task) {
                task.stato = nuovoStato;
            }
        }
        Kanban.applyRemoteMove = applyRemoteMove;
    })(Kanban = Ordo.Kanban || (Ordo.Kanban = {}));
})(Ordo || (Ordo = {}));
//# sourceMappingURL=Board.js.map