const apiBase = "https://localhost:7084/api";

// --- On page load ---
$(document).ready(function () {
    $("#signupForm").submit(function (e) {
        e.preventDefault();

        // --- Get form values ---
        var name = $("#name").val();
        var email = $("#email").val();
        var password = $("#password").val();

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

        // --- Create user object ---
        var user = {
            name: name,
            email: email,
            password: password,
            active: true
        };

        // --- Send register request ---
        $.ajax({
            url: apiBase + "/user/Register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(user),
            success: function () {
                alert("Registered Sucesfully");
                window.location.href = "Login.html";
            },
            error: function () {
                $("#signupError").text("Register failed. Email may be taken.");
            }
        });
    });
});
