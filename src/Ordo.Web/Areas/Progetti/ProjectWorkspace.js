"use strict";
var Ordo;
(function (Ordo) {
    var ProjectWorkspace;
    (function (ProjectWorkspace) {
        const COLUMNS = [
            { stato: 0, titolo: "Da fare" },
            { stato: 1, titolo: "In corso" },
            { stato: 2, titolo: "Review" },
            { stato: 3, titolo: "Completato" }
        ];

        function create(seed, initialTab, currentUserId, moveTaskUrl, projectId) {
            const app = Vue.createApp({
                data() {
                    const boards = (seed.boards || []).map(board => ({
                        ...board,
                        tasks: (board.tasks || []).map(task => ({ ...task }))
                    }));
                    const savedBoardId = window.localStorage.getItem("ordo:last-board:" + projectId);
                    const selectedBoardId = boards.some(board => board.id === savedBoardId)
                        ? savedBoardId
                        : (boards.length ? boards[0].id : null);
                    return {
                        activeTab: initialTab || "overview",
                        boards,
                        members: seed.membri || [],
                        messages: seed.messages || [],
                        selectedBoardId,
                        draggedTask: null,
                        suppressCardClick: false,
                        currentUserId,
                        columns: COLUMNS,
                        filters: { search: "", state: "", priority: "", assignee: "" },
                        taskModal: {
                            open: false, id: "", boardId: null, titolo: "", descrizione: "",
                            priorita: "1", scadenza: "", assignedUserId: ""
                        }
                    };
                },
                computed: {
                    selectedBoard() {
                        return this.boards.find(board => board.id === this.selectedBoardId) || this.boards[0] || { id: null, nome: "", tasks: [] };
                    },
                    totalTasks() {
                        return this.boards.reduce((total, board) => total + board.tasks.length, 0);
                    },
                    filteredTaskCount() {
                        return this.selectedBoard.tasks.filter(task => this.matchesFilters(task)).length;
                    }
                },
                methods: {
                    selectTab(tab) {
                        this.activeTab = tab;
                        window.history.replaceState(null, "", "?tab=" + encodeURIComponent(tab));
                    },
                    tasksByState(state) {
                        return this.selectedBoard.tasks
                            .filter(task => task.stato === state)
                            .sort((left, right) => right.priorita - left.priorita);
                    },
                    matchesFilters(task) {
                        const search = this.filters.search.toLowerCase();
                        return (!search || task.titolo.toLowerCase().includes(search)) &&
                            (!this.filters.state || String(task.stato) === this.filters.state) &&
                            (!this.filters.priority || String(task.priorita) === this.filters.priority) &&
                            (!this.filters.assignee || String(task.assignedUserId || "") === this.filters.assignee);
                    },
                    filteredTasksByState(state) {
                        return this.tasksByState(state).filter(task => this.matchesFilters(task));
                    },
                    async dropTaskOnTask(targetTask) {
                        const draggedTask = this.draggedTask;
                        this.draggedTask = null;
                        if (!draggedTask || draggedTask.id === targetTask.id || draggedTask.stato !== targetTask.stato)
                            return;

                        const direction = this.tasksByState(targetTask.stato).findIndex(task => task.id === draggedTask.id)
                            < this.tasksByState(targetTask.stato).findIndex(task => task.id === targetTask.id)
                            ? -1
                            : 1;
                        const previousPriority = draggedTask.priorita;
                        const newPriority = Math.max(0, Math.min(2, targetTask.priorita + direction));
                        if (newPriority === previousPriority)
                            return;

                        draggedTask.priorita = newPriority;
                        try {
                            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value || "";
                            const response = await fetch(moveTaskUrl, {
                                method: "POST",
                                headers: {
                                    "Content-Type": "application/json",
                                    "RequestVerificationToken": token
                                },
                                body: JSON.stringify({
                                    taskId: draggedTask.id,
                                    nuovoStato: draggedTask.stato,
                                    nuovaPriorita: newPriority
                                })
                            });
                            if (!response.ok)
                                throw new Error("Richiesta fallita: " + response.status);
                        } catch (error) {
                            draggedTask.priorita = previousPriority;
                            console.error(error);
                            alert("Non è stato possibile aggiornare la priorità.");
                        }
                    },
                    resetFilters() {
                        this.filters = { search: "", state: "", priority: "", assignee: "" };
                    },
                    openNewTask() {
                        this.taskModal = {
                            open: true, id: "", boardId: this.selectedBoard.id, titolo: "",
                            descrizione: "", priorita: "1", scadenza: "", assignedUserId: ""
                        };
                    },
                    openEditTask(task) {
                        this.taskModal = {
                            open: true, id: task.id, boardId: this.selectedBoard.id,
                            titolo: task.titolo, descrizione: task.descrizione || "",
                            priorita: String(task.priorita), scadenza: task.scadenza ? task.scadenza.substring(0, 10) : "",
                            assignedUserId: task.assignedUserId || ""
                        };
                    },
                    handleCardClick(task) {
                        if (this.suppressCardClick)
                            return;
                        this.openEditTask(task);
                    },
                    startDragging(task) {
                        this.suppressCardClick = true;
                        this.draggedTask = task;
                    },
                    endDragging() {
                        window.setTimeout(() => {
                            this.suppressCardClick = false;
                        }, 0);
                    },
                    closeTaskModal() {
                        this.taskModal.open = false;
                    },
                    async saveTask(event) {
                        const form = event.target;
                        const formData = new FormData(form);
                        try {
                            const response = await fetch(form.action, {
                                method: "POST",
                                body: formData,
                                headers: {
                                    "Accept": "application/json",
                                    "X-Requested-With": "XMLHttpRequest"
                                }
                            });
                            const result = await response.json();
                            if (!response.ok || !result.task)
                                throw new Error(result.error || "Salvataggio non riuscito.");

                            const board = this.boards.find(item => item.id === result.task.boardId);
                            if (board) {
                                const existingTask = board.tasks.find(item => item.id === result.task.id);
                                if (existingTask)
                                    Object.assign(existingTask, result.task);
                                else
                                    board.tasks.push(result.task);
                            }
                            this.closeTaskModal();
                        } catch (error) {
                            console.error(error);
                            alert("Non è stato possibile salvare l'attività.");
                        }
                    },
                    formatDate(value) {
                        return new Date(value).toLocaleDateString("it-IT", { day: "2-digit", month: "2-digit" });
                    },
                    formatMessageDate(value) {
                        return new Date(value).toLocaleString("it-IT", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
                    },
                    async dropTask(state) {
                        if (!this.draggedTask || this.draggedTask.stato === state)
                            return;
                        const task = this.draggedTask;
                        this.draggedTask = null;
                        const previousState = task.stato;
                        task.stato = state;
                        try {
                            const token = document.querySelector("input[name='__RequestVerificationToken']")?.value || "";
                            const response = await fetch(moveTaskUrl, {
                                method: "POST",
                                headers: { "Content-Type": "application/json", "RequestVerificationToken": token },
                                body: JSON.stringify({ taskId: task.id, nuovoStato: state })
                            });
                            if (!response.ok)
                                throw new Error("Richiesta fallita: " + response.status);
                        } catch (error) {
                            task.stato = previousState;
                            console.error(error);
                            alert("Non è stato possibile salvare lo spostamento.");
                        }
                    },
                    addTask(taskEvent) {
                        const boardId = taskEvent.boardId || taskEvent.idGroup;
                        const board = this.boards.find(item => item.id === boardId);
                        if (board && !board.tasks.some(task => task.id === taskEvent.taskId)) {
                            board.tasks.push({
                                id: taskEvent.taskId,
                                boardId,
                                titolo: taskEvent.titolo,
                                priorita: taskEvent.priorita,
                                stato: taskEvent.stato,
                                scadenza: taskEvent.scadenza,
                                assignedUserId: taskEvent.assignedUserId,
                                assignedUserName: taskEvent.assignedUserName,
                                descrizione: ""
                            });
                        }
                    },
                    updateTask(taskEvent) {
                        const update = taskEvent.task || taskEvent;
                        const taskId = update.taskId || update.id;
                        for (const board of this.boards) {
                            const task = board.tasks.find(item => item.id === taskId);
                            if (task) {
                                Object.assign(task, {
                                    titolo: update.titolo,
                                    priorita: update.priorita,
                                    stato: update.stato,
                                    scadenza: update.scadenza,
                                    assignedUserId: update.assignedUserId,
                                    ...(update.assignedUserName !== undefined
                                        ? { assignedUserName: update.assignedUserName }
                                        : {})
                                });
                                return;
                            }
                        }
                    },
                    removeTask(taskEvent) {
                        for (const board of this.boards) {
                            const index = board.tasks.findIndex(task => task.id === taskEvent.taskId);
                            if (index >= 0) {
                                board.tasks.splice(index, 1);
                                return;
                            }
                        }
                    }
                },
                mounted() {
                    this.$watch("selectedBoardId", value => {
                        if (value)
                            window.localStorage.setItem("ordo:last-board:" + projectId, value);
                    });
                    const manager = new SignalRConnectionManager("/OrdoHub", projectId, "JoinGroup", "LeaveGroup");
                    manager.registerEvents();
                    this.boards.forEach(board => manager.addAdditionalGroup(board.id));
                    manager.connection.on("TaskMoved", (taskId, state) => {
                        for (const board of this.boards) {
                            const task = board.tasks.find(item => item.id === taskId);
                            if (task) task.stato = state;
                        }
                    });
                    manager.connection.on("TaskCreated", task => this.addTask(task));
                    manager.connection.on("TaskUpdated", task => this.updateTask(task));
                    manager.connection.on("TaskDeleted", task => this.removeTask(task));
                    manager.connection.on("ProjectChatMessageAdded", message => this.messages.push(message));
                    manager.startConnection();

                    const taskId = new URLSearchParams(window.location.search).get("task");
                    if (taskId) {
                        const task = this.boards
                            .flatMap(board => board.tasks)
                            .find(item => item.id === taskId);
                        if (task) {
                            this.activeTab = "board";
                            this.$nextTick(() => this.openEditTask(task));
                        }
                    }
                }
            });
            app.mount("#projectWorkspace");
        }

        ProjectWorkspace.create = create;
    })(ProjectWorkspace = Ordo.ProjectWorkspace || (Ordo.ProjectWorkspace = {}));
})(Ordo || (Ordo = {}));
