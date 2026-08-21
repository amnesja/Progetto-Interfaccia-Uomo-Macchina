"use strict";
function showSignalRMessage(message, duration = 4000, callback) {
    if (typeof Toastify !== "function")
        return;
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
                }
                else {
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
            document.getElementById('lostConnection').classList.remove('d-none');
            console.log("[" + new Date().toISOString() + "] SignalR in riconnessione. " + error + ".");
        });
        this.connection.onreconnected(async (connectionId) => {
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);
            try {
                if (this.joinGroupParamethers) {
                    await this.connection.invoke(this.joinGroupMethod, this.joinGroupParamethers);
                }
                else {
                    await this.connection.invoke(this.joinGroupMethod);
                }
                console.log("[" + new Date().toISOString() + "] SignalR riconnesso");
                document.getElementById('lostConnection').classList.add('d-none');
                document.getElementById('lostConnectionManualRetry').classList.add('d-none');
                window.dispatchEvent(new CustomEvent("ordo:signalr-reconnected"));
            }
            catch (err) {
                console.error("Impossibile rientrare nel gruppo SignalR", err);
                document.getElementById('lostConnection').classList.add('d-none');
                document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
            }
        });
        this.connection.onclose(async (error) => {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);
            console.log("[" + new Date().toISOString() + "] SignalR scollegato definitivamente");
            document.getElementById('lostConnection').classList.add('d-none');
            document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
        });
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
        console.log("[" + new Date().toISOString() + "] SignalR in connessione");
        try {
            await this.connection.start();
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);
            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.joinGroupMethod, this.joinGroupParamethers);
            }
            else {
                await this.connection.invoke(this.joinGroupMethod);
            }
            console.log("[" + new Date().toISOString() + "] SignalR connesso");
            document.getElementById('lostConnection').classList.add('d-none');
            document.getElementById('lostConnectionManualRetry').classList.add('d-none');
        }
        catch (err) {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);
            if (this.connection.state === signalR.HubConnectionState.Connected) {
                await this.connection.stop();
            }
            document.getElementById('lostConnection').classList.remove('d-none');
            document.getElementById('lostConnectionManualRetry').classList.add('d-none');
            console.log("[" + new Date().toISOString() + "] SignalR erore in connessione " + err);
            console.log("[" + new Date().toISOString() + "] SignalR riprovo la connessione tra 5000ms");
            setTimeout(() => this.startConnection(), 5000);
        }
    }
    ;
    async stopConnection() {
        console.log("[" + new Date().toISOString() + "] SignalR in uscita");
        try {
            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.leaveGroupMethod, this.joinGroupParamethers);
            }
            else {
                await this.connection.invoke(this.leaveGroupMethod);
            }
            await this.connection.stop();
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);
            console.log("[" + new Date().toISOString() + "] SignalR disconnesso");
            document.getElementById('lostConnection').classList.remove('d-none');
            document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
        }
        catch (err) {
            console.assert(this.connection.state !== signalR.HubConnectionState.Disconnected);
            console.log("[" + new Date().toISOString() + "] SignalR erore in disconnessione " + err);
        }
    }
    ;
}
//# sourceMappingURL=signalRConnectionManager.js.map