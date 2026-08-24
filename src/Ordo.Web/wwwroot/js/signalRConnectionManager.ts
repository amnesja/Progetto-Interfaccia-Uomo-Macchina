declare var signalR: any;
declare var Toastify: any;

function showSignalRMessage(message: string, duration = 4000, callback?: () => void) {
    if (typeof Toastify !== "function") return;

    Toastify({
        close: true,
        gravity: "top",
        position: "right",
        className: "onit-toastify onit-toastify-info ordo-realtime-toast",
        text: message,
        duration,
        callback
    }).showToast();
}

class SignalRConnectionManager {
    joinGroupMethod: string;
    joinGroupParamethers: string;
    leaveGroupMethod: string;
    additionalGroupParameters: string[] = [];
    connection: any;
    private reconnectNoticeTimer: number | undefined;
    private initialRetryTimer: number | undefined;

    constructor(connectionUrl, joinGroupParamethers, joinGroupMethod, leaveGroupMethod) {
        this.joinGroupMethod = joinGroupMethod;
        this.joinGroupParamethers = joinGroupParamethers;
        this.leaveGroupMethod = leaveGroupMethod;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(connectionUrl)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    const maxReconnectionMillisecondsDelay = 60000;
                    const retryDelays = [0, 2000, 5000, 10000, 30000];
                    if (retryContext.elapsedMilliseconds < maxReconnectionMillisecondsDelay) {
                        const retryDelay = retryDelays[Math.min(retryContext.previousRetryCount, retryDelays.length - 1)];
                        console.log("[" + new Date().toISOString() + "] SignalR riprovo la connessione tra " + retryDelay + "ms");
                        return retryDelay;
                    } else {
                        console.log("[" + new Date().toISOString() + "] SignalR non riprovo, ho superato " + maxReconnectionMillisecondsDelay + "ms di tentativi");
                        return null;
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Error)
            .build();
    }

    async registerEvents() {
        this.connection.onreconnecting(error => {
            console.assert(this.connection.state === signalR.HubConnectionState.Reconnecting);
            this.reconnectNoticeTimer = window.setTimeout(() => this.setConnectionBanner(true), 800);
            console.log("[" + new Date().toISOString() + "] SignalR in riconnessione. " + error + ".");
        });

        this.connection.onreconnected(async connectionId => {
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);
            try {
                await this.joinGroups();

                console.log("[" + new Date().toISOString() + "] SignalR riconnesso");
                this.setConnectionBanner(false);
            } catch (err) {
                console.error("Impossibile rientrare nel gruppo SignalR", err);
                this.setConnectionBanner(false, true);
            }
        });

        this.connection.onclose(async (error) => {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);
            console.log("[" + new Date().toISOString() + "] SignalR scollegato definitivamente");
            this.setConnectionBanner(false, true);
        });
    }

    private setConnectionBanner(isReconnecting: boolean, showManualRetry = false) {
        if (this.reconnectNoticeTimer) window.clearTimeout(this.reconnectNoticeTimer);
        this.reconnectNoticeTimer = undefined;
        document.getElementById('lostConnection')?.classList.toggle('d-none', !isReconnecting);
        document.getElementById('lostConnectionManualRetry')?.classList.toggle('d-none', !showManualRetry);
    }

    addAdditionalGroup(groupParameter: string) {
        if (groupParameter && groupParameter !== this.joinGroupParamethers && !this.additionalGroupParameters.includes(groupParameter)) {
            this.additionalGroupParameters.push(groupParameter);
        }
    }

    private async joinGroups() {
        if (this.joinGroupParamethers) {
            await this.connection.invoke(this.joinGroupMethod, this.joinGroupParamethers);
        } else {
            await this.connection.invoke(this.joinGroupMethod);
        }

        for (const groupParameter of this.additionalGroupParameters) {
            await this.connection.invoke(this.joinGroupMethod, groupParameter);
        }
    }

    async changeConnectionParamethers(joinLeaveGroupParamethers = this.joinGroupParamethers, joinGroupMethod = this.joinGroupMethod, leaveGroupMethod = this.leaveGroupMethod) {
        if (this.connection.state !== signalR.HubConnectionState.Disconnected)
            await this.stopConnection();

        this.joinGroupMethod = joinGroupMethod;
        this.joinGroupParamethers = joinLeaveGroupParamethers;
        this.leaveGroupMethod = leaveGroupMethod;

        await this.startConnection();
    }

    async startConnection() {
        if (this.connection.state === signalR.HubConnectionState.Connected ||
            this.connection.state === signalR.HubConnectionState.Connecting ||
            this.connection.state === signalR.HubConnectionState.Reconnecting) return;

        console.log("[" + new Date().toISOString() + "] SignalR in connessione");
        try {
            await this.connection.start();
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);

            await this.joinGroups();

            console.log("[" + new Date().toISOString() + "] SignalR connesso");
            this.setConnectionBanner(false);
        } catch (err) {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);

            if (this.connection.state === signalR.HubConnectionState.Connected) {
                await this.connection.stop();
            }

            this.setConnectionBanner(true);

            console.log("[" + new Date().toISOString() + "] SignalR erore in connessione " + err);
            console.log("[" + new Date().toISOString() + "] SignalR riprovo la connessione tra 5000ms");
            if (!this.initialRetryTimer) {
                this.initialRetryTimer = window.setTimeout(() => {
                    this.initialRetryTimer = undefined;
                    this.startConnection();
                }, 5000);
            }
        }
    };

    async stopConnection() {
        console.log("[" + new Date().toISOString() + "] SignalR in uscita");
        try {

            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.leaveGroupMethod, this.joinGroupParamethers);
            } else {
                await this.connection.invoke(this.leaveGroupMethod);
            }

            await this.connection.stop();
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);

            console.log("[" + new Date().toISOString() + "] SignalR disconnesso");
            this.setConnectionBanner(false, true);
        } catch (err) {
            console.assert(this.connection.state !== signalR.HubConnectionState.Disconnected);

            console.log("[" + new Date().toISOString() + "] SignalR erore in disconnessione " + err);
        }
    };
}
