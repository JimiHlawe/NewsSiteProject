const apiBase = "https://localhost:7084/api";

    $(document).ready(function () {
        $("#loginForm").submit(function (e) {
            e.preventDefault();

            var email = $("#email").val();
            var password = $("#password").val();

            var requestData = {
                email: email,
                password: password
            };

            $.ajax({
                url: apiBase + "/Users/Login",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(requestData),
                success: function (user) {
                    if (user) {
                        sessionStorage.setItem("loggedUser", JSON.stringify(user));
                        localStorage.setItem("user", JSON.stringify(user));
                        window.location.href = "index.html";
                    } else {
                        $("#loginError").text("Wrong email or password.");
                    }
                },
                error: function () {
                    $("#loginError").text("Login failed. Try again.");
                }
            });
        });
    });

