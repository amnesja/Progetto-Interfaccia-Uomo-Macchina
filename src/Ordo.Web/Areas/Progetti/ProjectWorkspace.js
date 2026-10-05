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


        function create(
            seed,
            initialTab,
            currentUserId,
            moveTaskUrl,
            projectId,
            editProjectUrl,
            deleteProjectUrl,
            dashboardUrl,
            chatSendUrl,
            chatMessagesUrl
        ) {

            const mountWorkspace = () => {
                const app = Vue.createApp({

                data() {

                    const boards = (seed.boards || []).map(board => ({
                        ...board,
                        tasks: (board.tasks || []).map(task => ({
                            ...task
                        }))
                    }));

                    const savedBoardId =
                        window.localStorage.getItem(
                            "ordo:last-board:" + projectId
                        );

                    const selectedBoardId =
                        boards.some(board => board.id === savedBoardId)
                            ? savedBoardId
                            : (boards.length
                                ? boards[0].id
                                : null);


                    return {

                        activeTab: initialTab || "overview",

                        boards,

                        members: seed.membri || [],

                        messages: seed.messages || [],

                        loadedChatHistoryCount: (seed.messages || []).length,

                        hasOlderMessages: seed.hasMoreMessages || false,

                        chatDraft: "",

                        sendingChatMessage: false,

                        loadingOlderMessages: false,

                        chatError: "",

                        newChatMessages: 0,

                        selectedBoardId,

                        draggedTask: null,

                        suppressCardClick: false,

                        currentUserId,

                        ownerId: seed.ownerId || null,

                        columns: COLUMNS,

                        filters: {
                            search: "",
                            state: "",
                            priority: "",
                            assignee: ""
                        },


                        taskModal: {

                            open: false,

                            mode: "create",

                            id: "",

                            boardId: null,

                            titolo: "",

                            descrizione: "",

                            priorita: "1",

                            scadenza: "",

                            assignedUserId: ""

                        },


                        savingTask: false,


                        projectEdit: {

                            open: false,

                            nome: seed.nome || "",

                            descrizione: seed.descrizione || "",

                            saving: false

                        },


                        projectDelete: {

                            open: false,

                            deleting: false

                        }

                    };

                },


                computed: {

                    selectedBoard() {

                        return this.boards.find(
                            board => board.id === this.selectedBoardId
                        ) || this.boards[0] || {
                            id: null,
                            nome: "",
                            tasks: []
                        };

                    },


                    totalTasks() {

                        return this.boards.reduce(
                            (total, board) =>
                                total + board.tasks.length,
                            0
                        );

                    },


                    filteredTaskCount() {

                        return this.selectedBoard.tasks.filter(
                            task => this.matchesFilters(task)
                        ).length;

                    },

                    taskMinDate() {

                        const today = new Date();

                        return (
                            today.getFullYear() +
                            "-" +
                            String(
                                today.getMonth() + 1
                            ).padStart(2, "0") +
                            "-" +
                            String(
                                today.getDate()
                            ).padStart(2, "0")
                        );
                    }

                },


                methods: {

                    selectTab(tab) {

                        this.activeTab = tab;

                        if (tab === "chat") {
                            this.$nextTick(() => this.scrollChatToBottom(true));
                        }

                        window.history.replaceState(
                            null,
                            "",
                            "?tab=" + encodeURIComponent(tab)
                        );

                    },


                    openDatePicker(event) {

                        const input = event.currentTarget;

                        if (typeof input.showPicker !== "function") {
                            return;
                        }

                        try {
                            input.showPicker();
                        } catch (error) {
                            // Il browser mantiene il comportamento nativo del campo.
                        }

                    },


                    tasksByState(state) {

                        return this.selectedBoard.tasks

                            .filter(task =>
                                task.stato === state
                            )

                            .sort(
                                (left, right) =>
                                    right.priorita - left.priorita
                            );

                    },


                    matchesFilters(task) {

                        const search =
                            this.filters.search.toLowerCase();

                        return (
                                !search ||
                                task.titolo
                                    .toLowerCase()
                                    .includes(search)
                            )
                            &&
                            (
                                !this.filters.state ||
                                String(task.stato) ===
                                this.filters.state
                            )
                            &&
                            (
                                !this.filters.priority ||
                                String(task.priorita) ===
                                this.filters.priority
                            )
                            &&
                            (
                                !this.filters.assignee ||
                                String(
                                    task.assignedUserId || ""
                                ) === this.filters.assignee
                            );

                    },


                    filteredTasksByState(state) {

                        return this.tasksByState(state)
                            .filter(task =>
                                this.matchesFilters(task)
                            );

                    },


                    // Scambio esatto delle priorità tra due card della stessa colonna.
                    // Stessa logica (e stesso parametro "swapWithTaskId" verso il backend)
                    // usata dalla board Kanban a pagina intera, per coerenza tra le due viste.
                    async dropTaskOnTask(targetTask) {

                        const draggedTask =
                            this.draggedTask;

                        this.draggedTask = null;


                        if (
                            !draggedTask ||
                            draggedTask.id === targetTask.id ||
                            draggedTask.stato !== targetTask.stato
                        ) {
                            return;
                        }


                        const draggedPriority =
                            draggedTask.priorita;

                        const targetPriority =
                            targetTask.priorita;


                        // Aggiornamento ottimistico: scambiamo subito
                        // le due priorità nell'interfaccia.
                        draggedTask.priorita =
                            targetPriority;

                        targetTask.priorita =
                            draggedPriority;


                        try {

                            const token =
                                document.querySelector(
                                    "input[name='__RequestVerificationToken']"
                                )?.value || "";


                            const response =
                                await fetch(
                                    moveTaskUrl,
                                    {
                                        method: "POST",

                                        headers: {
                                            "Content-Type":
                                                "application/json",

                                            "RequestVerificationToken":
                                            token
                                        },

                                        body: JSON.stringify({
                                            taskId:
                                            draggedTask.id,

                                            nuovoStato:
                                            draggedTask.stato,

                                            swapWithTaskId:
                                            targetTask.id
                                        })
                                    }
                                );


                            if (!response.ok) {

                                throw new Error(
                                    "Richiesta fallita: " +
                                    response.status
                                );

                            }

                        }
                        catch (error) {

                            // Rollback nel caso in cui il backend rifiuti la richiesta.
                            draggedTask.priorita =
                                draggedPriority;

                            targetTask.priorita =
                                targetPriority;

                            console.error(error);

                            alert(
                                "Non è stato possibile aggiornare la priorità."
                            );

                        }

                    },


                    resetFilters() {

                        this.filters = {
                            search: "",
                            state: "",
                            priority: "",
                            assignee: ""
                        };

                    },


                    /* ============================
                       MODIFICA PROGETTO
                       ============================ */


                    openProjectEdit() {

                        if (this.savingTask) {
                            return;
                        }

                        this.taskModal.open = false;
                        this.projectEdit.open = true;

                        this.projectEdit.nome =
                            seed.nome || "";

                        this.projectEdit.descrizione =
                            seed.descrizione || "";

                        this.projectDelete.open = false;


                        this.$nextTick(() => {

                            document
                                .getElementById(
                                    "projectEditName"
                                )
                                ?.focus();

                        });

                    },


                    closeProjectEdit() {

                        if (this.projectEdit.saving) {
                            return;
                        }

                        this.projectEdit.open = false;

                    },


                    openDeleteProject() {

                        if (this.projectEdit.saving) {
                            return;
                        }

                        this.projectDelete.open = true;

                    },


                    closeDeleteProject() {

                        if (this.projectDelete.deleting) {
                            return;
                        }

                        this.projectDelete.open = false;

                    },


                    async saveProject(event) {

                        const form = event.target;

                        const formData =
                            new FormData(form);


                        this.projectEdit.saving = true;


                        try {

                            const response =
                                await fetch(
                                    editProjectUrl,
                                    {
                                        method: "POST",

                                        body: formData,

                                        headers: {
                                            "Accept":
                                                "application/json",

                                            "X-Requested-With":
                                                "XMLHttpRequest"
                                        }
                                    }
                                );


                            const result =
                                await response.json();


                            if (
                                !response.ok ||
                                !result.success
                            ) {

                                throw new Error(
                                    result.error ||
                                    "Salvataggio non riuscito."
                                );

                            }


                            seed.nome =
                                result.nome;

                            seed.descrizione =
                                result.descrizione || "";


                            this.projectEdit.nome =
                                result.nome;

                            this.projectEdit.descrizione =
                                result.descrizione || "";


                            const header =
                                document.querySelector(
                                    ".ordo-project-workspace-header"
                                );


                            const title =
                                header?.querySelector("h1");


                            const description =
                                header?.querySelector("p");


                            if (title) {

                                title.textContent =
                                    result.nome;

                            }


                            if (description) {

                                description.textContent =
                                    result.descrizione?.trim()
                                    ||
                                    "Workspace del progetto.";

                            }


                            document.title =
                                result.nome;


                            this.closeProjectEdit();

                        }
                        catch (error) {

                            console.error(error);

                            alert(
                                error.message ||
                                "Non è stato possibile salvare il progetto."
                            );

                        }
                        finally {

                            this.projectEdit.saving =
                                false;

                        }

                    },


                    async deleteProject() {

                        if (this.projectDelete.deleting) {
                            return;
                        }


                        this.projectDelete.deleting = true;


                        try {

                            const token =
                                document.querySelector(
                                    "input[name='__RequestVerificationToken']"
                                )?.value || "";


                            const body =
                                new URLSearchParams();


                            body.append(
                                "id",
                                projectId
                            );


                            body.append(
                                "__RequestVerificationToken",
                                token
                            );


                            const response =
                                await fetch(
                                    deleteProjectUrl,
                                    {
                                        method: "POST",

                                        headers: {
                                            "Accept":
                                                "application/json",

                                            "X-Requested-With":
                                                "XMLHttpRequest",

                                            "RequestVerificationToken":
                                            token,

                                            "Content-Type":
                                                "application/x-www-form-urlencoded;charset=UTF-8"
                                        },

                                        body
                                    }
                                );


                            const result =
                                await response.json();


                            if (
                                !response.ok ||
                                !result.success
                            ) {

                                throw new Error(
                                    result.error ||
                                    "Eliminazione non riuscita."
                                );

                            }


                            window.location.href =
                                result.redirectUrl ||
                                dashboardUrl;

                        }
                        catch (error) {

                            console.error(error);

                            alert(
                                error.message ||
                                "Non è stato possibile eliminare il progetto."
                            );

                            this.projectDelete.deleting =
                                false;

                        }

                    },


                    /* ============================
                       TASK
                       ============================ */


                    openNewTask() {

                        if (this.projectEdit.saving) {
                            return;
                        }

                        this.projectEdit.open = false;
                        this.taskModal = {

                            open: true,

                            mode: "create",

                            id: "",

                            boardId:
                            this.selectedBoard.id,

                            titolo: "",

                            descrizione: "",

                            priorita: "1",

                            scadenza: "",

                            assignedUserId: ""

                        };

                    },


                    openEditTask(task) {

                        if (this.projectEdit.saving) {
                            return;
                        }

                        this.projectEdit.open = false;
                        this.taskModal = {

                            open: true,

                            mode: "edit",

                            id: task.id,

                            boardId:
                            this.selectedBoard.id,

                            titolo:
                            task.titolo,

                            descrizione:
                                task.descrizione || "",

                            priorita:
                                String(task.priorita),

                            scadenza:
                                task.scadenza
                                    ? task.scadenza.substring(
                                        0,
                                        10
                                    )
                                    : "",

                            assignedUserId:
                                task.assignedUserId || ""

                        };

                    },


                    handleCardClick(task) {

                        if (this.suppressCardClick) {
                            return;
                        }

                        this.openEditTask(task);

                    },


                    startDragging(task) {

                        this.suppressCardClick = true;

                        this.draggedTask = task;

                    },


                    endDragging() {

                        window.setTimeout(() => {

                            this.suppressCardClick =
                                false;

                        }, 0);

                    },


                    closeTaskModal() {

                        this.taskModal.open = false;

                    },


                    async saveTask(event) {

                        const form = event.target;

                        const formData =
                            new FormData(form);


                        this.savingTask = true;


                        try {

                            const response =
                                await fetch(
                                    form.action,
                                    {
                                        method: "POST",

                                        body: formData,

                                        headers: {
                                            "Accept":
                                                "application/json",

                                            "X-Requested-With":
                                                "XMLHttpRequest"
                                        }
                                    }
                                );


                            const result =
                                await response.json();


                            if (
                                !response.ok ||
                                !result.task
                            ) {

                                throw new Error(
                                    result.error ||
                                    "Salvataggio non riuscito."
                                );

                            }


                            const board =
                                this.boards.find(
                                    item =>
                                        item.id ===
                                        result.task.boardId
                                );


                            if (board) {

                                const existingTask =
                                    board.tasks.find(
                                        item =>
                                            item.id ===
                                            result.task.id
                                    );


                                if (existingTask) {

                                    Object.assign(
                                        existingTask,
                                        result.task
                                    );

                                }
                                else {

                                    board.tasks.push(
                                        result.task
                                    );

                                }

                            }


                            this.closeTaskModal();

                        }
                        catch (error) {

                            console.error(error);

                            alert(
                                "Non è stato possibile salvare l'attività."
                            );

                        }
                        finally {

                            this.savingTask = false;

                        }

                    },


                    formatDate(value) {

                        return new Date(
                            value
                        ).toLocaleDateString(
                            "it-IT",
                            {
                                day: "2-digit",
                                month: "2-digit"
                            }
                        );

                    },


                    messageDayKey(value) {

                        const date = new Date(value);

                        return [
                            date.getFullYear(),
                            String(date.getMonth() + 1).padStart(2, "0"),
                            String(date.getDate()).padStart(2, "0")
                        ].join("-");

                    },


                    formatMessageDay(value) {

                        return new Date(value).toLocaleDateString(
                            "it-IT",
                            {
                                weekday: "long",
                                day: "numeric",
                                month: "long",
                                year: "numeric"
                            }
                        );

                    },


                    formatMessageTime(value) {

                        return new Date(value).toLocaleTimeString(
                            "it-IT",
                            {
                                hour: "2-digit",
                                minute: "2-digit"
                            }
                        );

                    },


                    isNewMessageDay(index) {

                        return index === 0 ||
                            this.messageDayKey(
                                this.messages[index - 1].dataCreazione
                            ) !== this.messageDayKey(
                                this.messages[index].dataCreazione
                            );

                    },


                    isGroupedMessage(index) {

                        if (index === 0) {
                            return false;
                        }

                        const previous = this.messages[index - 1];
                        const current = this.messages[index];

                        return previous.userId === current.userId &&
                            this.messageDayKey(previous.dataCreazione) ===
                            this.messageDayKey(current.dataCreazione);

                    },


                    chatIsNearBottom() {

                        const container = document.getElementById(
                            "projectChatMessages"
                        );

                        return !container ||
                            container.scrollHeight -
                            container.scrollTop -
                            container.clientHeight < 80;

                    },


                    handleChatScroll() {

                        if (this.chatIsNearBottom()) {
                            this.newChatMessages = 0;
                        }

                    },


                    scrollChatToBottom() {

                        this.$nextTick(() => {
                            const container = document.getElementById(
                                "projectChatMessages"
                            );

                            if (container) {
                                container.scrollTop = container.scrollHeight;
                            }

                            this.newChatMessages = 0;
                        });

                    },


                    appendChatMessage(message) {

                        if (
                            !message?.id ||
                            this.messages.some(
                                item => item.id === message.id
                            )
                        ) {
                            return;
                        }

                        const shouldScroll = this.chatIsNearBottom();
                        this.messages.push(message);

                        if (
                            shouldScroll ||
                            message.userId === this.currentUserId
                        ) {
                            this.scrollChatToBottom();
                        }
                        else {
                            this.newChatMessages++;
                        }

                    },


                    resizeChatInput(event) {

                        const input = event.currentTarget;
                        input.style.height = "auto";
                        input.style.height = Math.min(
                            input.scrollHeight,
                            140
                        ) + "px";
                        this.chatError = "";

                    },


                    async sendChatMessage() {

                        const text = this.chatDraft.trim();

                        if (!text || this.sendingChatMessage) {
                            return;
                        }

                        this.chatError = "";
                        this.sendingChatMessage = true;

                        try {
                            const formData = new FormData(this.$refs.chatForm);
                            formData.set("Testo", text);

                            const response = await fetch(chatSendUrl, {
                                method: "POST",
                                body: formData,
                                headers: {
                                    "Accept": "application/json",
                                    "X-Requested-With": "XMLHttpRequest"
                                }
                            });

                            const result = await response.json().catch(() => null);

                            if (!response.ok || !result?.id) {
                                throw new Error(
                                    result?.error || "Invio del messaggio non riuscito."
                                );
                            }

                            this.appendChatMessage(result);
                            this.chatDraft = "";
                            this.$nextTick(() => {
                                const composer = this.$refs.chatComposer;
                                composer.style.height = "auto";
                                composer.focus();
                            });
                        }
                        catch (error) {
                            this.chatError = error.message ||
                                "Impossibile inviare il messaggio. Riprova.";
                        }
                        finally {
                            this.sendingChatMessage = false;
                        }

                    },


                    async loadOlderMessages() {

                        if (this.loadingOlderMessages || !this.hasOlderMessages) {
                            return;
                        }

                        const container = document.getElementById(
                            "projectChatMessages"
                        );
                        const previousHeight = container?.scrollHeight || 0;
                        const previousTop = container?.scrollTop || 0;
                        this.loadingOlderMessages = true;
                        this.chatError = "";

                        try {
                            const url = new URL(chatMessagesUrl, window.location.origin);
                            url.searchParams.set(
                                "skip",
                                this.loadedChatHistoryCount
                            );

                            const response = await fetch(url, {
                                headers: { "Accept": "application/json" }
                            });

                            if (!response.ok) {
                                throw new Error("Caricamento dei messaggi non riuscito.");
                            }

                            const result = await response.json();

                            if (
                                !Array.isArray(result.messages) ||
                                typeof result.hasMore !== "boolean"
                            ) {
                                throw new Error(
                                    "Risposta non valida durante il caricamento dei messaggi."
                                );
                            }

                            const existingIds = new Set(
                                this.messages.map(message => message.id)
                            );
                            const olderMessages = result.messages.filter(
                                message => !existingIds.has(message.id)
                            );

                            this.messages = [
                                ...olderMessages,
                                ...this.messages
                            ];
                            this.loadedChatHistoryCount += result.messages.length;
                            this.hasOlderMessages = result.hasMore;

                            this.$nextTick(() => {
                                if (container) {
                                    container.scrollTop =
                                        previousTop +
                                        container.scrollHeight -
                                        previousHeight;
                                }
                            });
                        }
                        catch (error) {
                            this.chatError = error.message ||
                                "Impossibile caricare i messaggi precedenti.";
                        }
                        finally {
                            this.loadingOlderMessages = false;
                        }

                    },


                    async dropTask(state) {

                        if (
                            !this.draggedTask ||
                            this.draggedTask.stato === state
                        ) {
                            return;
                        }


                        const task =
                            this.draggedTask;

                        this.draggedTask = null;


                        const previousState =
                            task.stato;


                        task.stato = state;


                        try {

                            const token =
                                document.querySelector(
                                    "input[name='__RequestVerificationToken']"
                                )?.value || "";


                            const response =
                                await fetch(
                                    moveTaskUrl,
                                    {
                                        method: "POST",

                                        headers: {
                                            "Content-Type":
                                                "application/json",

                                            "RequestVerificationToken":
                                            token
                                        },

                                        body: JSON.stringify({
                                            taskId:
                                            task.id,

                                            nuovoStato:
                                            state
                                        })
                                    }
                                );


                            if (!response.ok) {

                                throw new Error(
                                    "Richiesta fallita: " +
                                    response.status
                                );

                            }

                        }
                        catch (error) {

                            task.stato =
                                previousState;

                            console.error(error);

                            alert(
                                "Non è stato possibile salvare lo spostamento."
                            );

                        }

                    },


                    addTask(taskEvent) {

                        const boardId =
                            taskEvent.boardId ||
                            taskEvent.idGroup;


                        const board =
                            this.boards.find(
                                item =>
                                    item.id === boardId
                            );


                        if (
                            board &&
                            !board.tasks.some(
                                task =>
                                    task.id ===
                                    taskEvent.taskId
                            )
                        ) {

                            board.tasks.push({

                                id:
                                taskEvent.taskId,

                                boardId,

                                titolo:
                                taskEvent.titolo,

                                priorita:
                                taskEvent.priorita,

                                stato:
                                taskEvent.stato,

                                scadenza:
                                taskEvent.scadenza,

                                assignedUserId:
                                taskEvent.assignedUserId,

                                assignedUserName:
                                taskEvent.assignedUserName,

                                descrizione: ""

                            });

                        }

                    },


                    updateTask(taskEvent) {

                        const update =
                            taskEvent.task ||
                            taskEvent;


                        const taskId =
                            update.taskId ||
                            update.id;


                        for (
                            const board of this.boards
                            ) {

                            const task =
                                board.tasks.find(
                                    item =>
                                        item.id ===
                                        taskId
                                );


                            if (task) {

                                Object.assign(
                                    task,
                                    {
                                        titolo:
                                        update.titolo,

                                        priorita:
                                        update.priorita,

                                        stato:
                                        update.stato,

                                        scadenza:
                                        update.scadenza,

                                        assignedUserId:
                                        update.assignedUserId,

                                        ...(update.assignedUserName !== undefined
                                            ? {
                                                assignedUserName:
                                                update.assignedUserName
                                            }
                                            : {})
                                    }
                                );

                                return;

                            }

                        }

                    },


                    removeTask(taskEvent) {

                        for (
                            const board of this.boards
                            ) {

                            const index =
                                board.tasks.findIndex(
                                    task =>
                                        task.id ===
                                        taskEvent.taskId
                                );


                            if (index >= 0) {

                                board.tasks.splice(
                                    index,
                                    1
                                );

                                return;

                            }

                        }

                    }

                },


                mounted() {
                    window.addEventListener(
                        "keydown",
                        event => {

                            if (event.key !== "Escape") {
                                return;
                            }


                            if (this.projectDelete.open) {

                                this.closeDeleteProject();

                            }
                            else if (this.projectEdit.open) {

                                this.closeProjectEdit();

                            }
                            else if (this.taskModal.open) {

                                this.closeTaskModal();

                            }

                        }
                    );


                    this.$watch(
                        "selectedBoardId",
                        value => {

                            if (value) {

                                window.localStorage.setItem(
                                    "ordo:last-board:" +
                                    projectId,
                                    value
                                );

                            }

                        }
                    );


                    const manager = window.ordoGlobalSignalR;

                    if (!manager) {
                        console.error("La connessione SignalR condivisa non è disponibile.");
                        return;
                    }

                    manager.addAdditionalGroup(projectId);

                    this.boards.forEach(
                        board =>
                            manager.addAdditionalGroup(
                                board.id
                            )
                    );


                    manager.connection.on(
                        "TaskMoved",
                        (taskId, state) => {

                            for (
                                const board of this.boards
                                ) {

                                const task =
                                    board.tasks.find(
                                        item =>
                                            item.id ===
                                            taskId
                                    );


                                if (task) {

                                    task.stato =
                                        state;

                                }

                            }

                        }
                    );


                    manager.connection.on(
                        "TaskCreated",
                        task =>
                            this.addTask(task)
                    );


                    manager.connection.on(
                        "TaskUpdated",
                        task =>
                            this.updateTask(task)
                    );


                    manager.connection.on(
                        "TaskDeleted",
                        task =>
                            this.removeTask(task)
                    );

                    manager.connection.on(
                        "ProjectDeleted",
                        deletedProjectId => {

                            if (
                                deletedProjectId &&
                                deletedProjectId !== projectId
                            ) {
                                return;
                            }

                            window.location.href = dashboardUrl;
                        }
                    );

                    manager.connection.on(
                        "ProjectUpdated",
                        project => {

                            if (
                                project?.projectId &&
                                project.projectId !==
                                projectId
                            ) {
                                return;
                            }


                            const nome =
                                project?.nome ||
                                project?.Nome;


                            const descrizione =
                                project?.descrizione ??
                                project?.Descrizione ??
                                "";


                            if (nome) {

                                seed.nome =
                                    nome;

                                this.projectEdit.nome =
                                    nome;


                                const title =
                                    document.querySelector(
                                        ".ordo-project-workspace-header h1"
                                    );


                                if (title) {

                                    title.textContent =
                                        nome;

                                }


                                document.title =
                                    nome;

                            }


                            seed.descrizione =
                                descrizione;


                            this.projectEdit.descrizione =
                                descrizione;


                            const description =
                                document.querySelector(
                                    ".ordo-project-workspace-header p"
                                );


                            if (description) {

                                description.textContent =
                                    descrizione.trim() ||
                                    "Workspace del progetto.";

                            }

                        }
                    );


                    manager.connection.on(
                        "ProjectChatMessageAdded",
                        message =>
                            this.appendChatMessage({
                                id: message.id || message.messageId,
                                userId: message.userId,
                                userName: message.userName,
                                testo: message.testo,
                                dataCreazione: message.dataCreazione
                            })
                    );


                    if (this.activeTab === "chat") {
                        this.scrollChatToBottom();
                    }


                    const taskId =
                        new URLSearchParams(
                            window.location.search
                        ).get("task");


                    if (taskId) {

                        const task =
                            this.boards
                                .flatMap(
                                    board =>
                                        board.tasks
                                )
                                .find(
                                    item =>
                                        item.id ===
                                        taskId
                                );


                        if (task) {

                            this.activeTab =
                                "board";


                            this.$nextTick(
                                () =>
                                    this.openEditTask(task)
                            );

                        }

                    }

                }

            });


                app.mount("#projectWorkspace");
            };

            if (window.ordoGlobalSignalR) {
                mountWorkspace();
            }
            else {
                window.addEventListener(
                    "ordo:signalr-ready",
                    mountWorkspace,
                    { once: true }
                );
            }

        }


        ProjectWorkspace.create = create;

    })(
        ProjectWorkspace =
            Ordo.ProjectWorkspace ||
            (Ordo.ProjectWorkspace = {})
    );

})(
    Ordo ||
    (Ordo = {})
);
