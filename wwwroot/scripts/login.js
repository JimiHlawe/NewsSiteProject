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
                const tagBubble = document.createElement("div");
                tagBubble.className = "tag-bubble";
                tagBubble.innerHTML = `
                    <input type="checkbox" id="tag-${tag.id}" value="${tag.id}">
                    <label for="tag-${tag.id}">${tag.name}</label>
                `;

                // הוספת אירוע קליק לתגית
                const checkbox = tagBubble.querySelector('input[type="checkbox"]');
                const label = tagBubble.querySelector('label');

                label.addEventListener('click', function (e) {
                    e.preventDefault();

                    if (!checkbox.checked) {
                        // אם התגית לא נבחרת - בחר אותה
                        checkbox.checked = true;
                        label.style.background = 'var(--primary-slate)';
                        label.style.color = 'white';
                    } else {
                        // אם התגית נבחרת - התנפצות ובטל בחירה
                        tagBubble.classList.add('exploding');

                        setTimeout(() => {
                            checkbox.checked = false;
                            tagBubble.classList.remove('exploding');
                            label.style.background = '';
                            label.style.color = '';
                        }, 600); // זמן האנימציה
                    }
                });

                container.appendChild(tagBubble);
            });
        })
        .catch(error => {
            console.error('Error loading tags:', error);
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

        // הוספת אנימציית טעינה
        $(this).addClass('loading');

        $.ajax({
            url: apiBase + "/Users/Login",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(requestData),
            success: function (user) {
                $(this).removeClass('loading');

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
                $(this).removeClass('loading');
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

        if (selectedTags.length === 0) {
            $("#signupError").text("Please select at least one interest.").addClass("show");
            return;
        }

        var user = {
            name: name,
            email: email,
            password: password,
            active: true,
            tags: selectedTags
        };

        // הוספת אנימציית טעינה
        $(this).addClass('loading');

        $.ajax({
            url: apiBase + "/Users/Register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(user),
            success: function (createdUser) {
                $(this).removeClass('loading');
                alert("Registered successfully");
                sessionStorage.setItem("loggedUser", JSON.stringify(createdUser));
                window.location.href = "index.html";
            },
            error: function () {
                $(this).removeClass('loading');
                $("#signupError").text("Register failed. Email may already exist.").addClass("show");
            }
        });
    });

    // הסתרת הודעות שגיאה כשמתחילים להקליד
    $('.form-input').on('input', function () {
        $('.error-message').removeClass('show');
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