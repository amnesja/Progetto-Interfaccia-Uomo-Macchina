var Ordo = Ordo || {};
Ordo.Toast = {
    showAssigned: function (titolo, projectNome) {
        var toast = document.createElement("div");
        toast.className = "ordo-toast-success";
        toast.innerHTML = '<i class="fa-solid fa-circle-check"></i> Ti è stato assegnato il task "' +
            Ordo.Toast.escapeHtml(titolo) + '"' +
            (projectNome ? ' nel progetto "' + Ordo.Toast.escapeHtml(projectNome) + '"' : '') + '.';
        document.body.appendChild(toast);

        setTimeout(function () {
            toast.style.opacity = "0";
            setTimeout(function () { toast.remove(); }, 400);
        }, 4000);
    },
    escapeHtml: function (str) {
        var div = document.createElement("div");
        div.textContent = str || "";
        return div.innerHTML;
    }
};