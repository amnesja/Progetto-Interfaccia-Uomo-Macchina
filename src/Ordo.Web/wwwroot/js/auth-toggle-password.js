document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".ordo-toggle-password").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var input = document.getElementById(btn.getAttribute("data-target"));
            if (!input) return;

            var icon = btn.querySelector("i");
            if (input.type === "password") {
                input.type = "text";
                icon.classList.remove("fa-eye");
                icon.classList.add("fa-eye-slash");
            } else {
                input.type = "password";
                icon.classList.remove("fa-eye-slash");
                icon.classList.add("fa-eye");
            }
        });
    });

    document.querySelectorAll("[data-password-strength]").forEach(function (indicator) {
        var input = document.getElementById(indicator.getAttribute("data-password-strength"));
        var label = indicator.querySelector(".ordo-strength-label");
        if (!input || !label) return;

        input.addEventListener("input", function () {
            var password = input.value;
            var score = 0;

            if (password.length >= 6) score++;
            if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score++;
            if (/\d/.test(password) || /[^a-zA-Z\d]/.test(password)) score++;

            indicator.removeAttribute("data-strength");

            if (password.length === 0) {
                label.textContent = "Minimo 6 caratteri";
            } else if (password.length < 6 || score === 1) {
                indicator.setAttribute("data-strength", "weak");
                label.textContent = "Password debole";
            } else if (score === 2) {
                indicator.setAttribute("data-strength", "medium");
                label.textContent = "Password media";
            } else {
                indicator.setAttribute("data-strength", "strong");
                label.textContent = "Password sicura";
            }
        });
    });
});
