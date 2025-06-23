const apiBase = "https://localhost:7084/api";

$(document).ready(function () {
    $("#signupForm").submit(function (e) {
        e.preventDefault();

        // 🟢 עדכון IDs
        var name = $("#fullName").val();
        var email = $("#signupEmail").val();
        var password = $("#signupPassword").val();

        // --- Validate inputs ---
        var nameRegex = /^[A-Za-z0-9]{2,}$/;
        var passwordRegex = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

        if (!nameRegex.test(name)) {
            $("#signupError").text("Name must have at least 2 letters.");
            return;
        }

        if (!passwordRegex.test(password)) {
            $("#signupError").text("Password needs 1 uppercase, 1 number, min 8 chars.");
            return;
        }

        var user = {
            name: name,
            email: email,
            password: password,
            active: true
        };

        $.ajax({
            url: apiBase + "/Users/Register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(user),
            success: function () {
                alert("Registered successfully");
                window.location.href = "Login.html";
            },
            error: function () {
                $("#signupError").text("Register failed. Email may be taken.");
            }
        });
    });
});
