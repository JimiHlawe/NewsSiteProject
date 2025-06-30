var apiBase = "https://localhost:7084/api";

function switchToSignup() {
    document.getElementById("signinForm").style.display = "none";
    document.getElementById("signupForm").style.display = "block";

    // טען את התגיות ברגע שעוברים למסך ההרשמה
    fetch(apiBase + "/Users/AllTags")
        .then(res => res.json())
        .then(tags => {
            const container = document.getElementById("signupTagsContainer");
            container.innerHTML = "";
            tags.forEach(tag => {
                container.innerHTML += `
                    <label>
                        <input type="checkbox" value="${tag.id}"> ${tag.name}
                    </label><br>`;
            });
        });
}


function switchToSignin() {
    document.getElementById("signupForm").style.display = "none";
    document.getElementById("signinForm").style.display = "block";
}

$(document).ready(function () {
    // בדיקה אם כתובת מייל שמורה בלוקל סטורג'
    var savedEmail = localStorage.getItem("rememberedEmail");
    if (savedEmail) {
        $("#signinEmail").val(savedEmail);
        $("#rememberMe").prop("checked", true);
    }

    // התחברות
    $("#signinFormSubmit").submit(function (e) {
        e.preventDefault();

        var email = $("#signinEmail").val();
        var password = $("#signinPassword").val();

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

                    if ($("#rememberMe").is(":checked")) {
                        localStorage.setItem("rememberedEmail", email);
                    } else {
                        localStorage.removeItem("rememberedEmail");
                    }

                    window.location.href = "index.html";
                } else {
                    $("#signinError").text("Wrong email or password.").addClass("show");
                }
            },
            error: function () {
                $("#signinError").text("Sign in failed. Try again.").addClass("show");
            }
        });
    });

    // הרשמה
    $("#signupFormSubmit").submit(function (e) {
        e.preventDefault();

        var name = $("#signupName").val();
        var email = $("#signupEmail").val();
        var password = $("#signupPassword").val();

        var nameRegex = /^[A-Za-z0-9]{2,}$/;
        var passwordRegex = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

        if (!nameRegex.test(name)) {
            $("#signupError").text("Name must have at least 2 letters.").addClass("show");
            return;
        }

        if (!passwordRegex.test(password)) {
            $("#signupError").text("Password must contain at least 1 uppercase, 1 number, and 8 characters.").addClass("show");
            return;
        }

        var checked = document.querySelectorAll("#signupTagsContainer input:checked");
        var selectedTags = [];
        checked.forEach(chk => selectedTags.push(parseInt(chk.value)));

        var user = {
            name: name,
            email: email,
            password: password,
            active: true,
            tags: selectedTags // 🔥 שולח את התגיות ביחד!
        };

        $.ajax({
            url: apiBase + "/Users/Register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(user),
            success: function (createdUser) {
                alert("Registered successfully");
                sessionStorage.setItem("loggedUser", JSON.stringify(createdUser));
                window.location.href = "index.html";
            },
            error: function () {
                $("#signupError").text("Register failed. Email may already exist.").addClass("show");
            }
        });
    });
});

// ✅ פונקציה לשמירת תחומי עניין
function saveUserTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const checked = document.querySelectorAll("#tagsContainer input:checked");
    checked.forEach(chk => {
        fetch(apiBase + "/Users/AddTag", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, tagId: chk.value })
        });
    });

    alert("Tags saved!");
    window.location.href = "index.html";
}
