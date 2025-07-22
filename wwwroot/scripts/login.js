var apiBase = "https://localhost:7084/api";

// DOM Elements
const container = document.getElementById("container");
const registerBtn = document.getElementById("register");
const loginBtn = document.getElementById("login");

// פאנלים במובייל
const signupSwitches = document.querySelectorAll(".signup .form-switch");
const signinSwitches = document.querySelectorAll(".signin .form-switch");

// מעבר בין Sign In ל־Sign Up
function toggleActiveClass(showSignup) {
    container.classList.toggle("active", showSignup);
    if (showSignup) loadTags();
}

// כפתורי דסקטופ
if (registerBtn) {
    registerBtn.addEventListener("click", () => toggleActiveClass(true));
}
if (loginBtn) {
    loginBtn.addEventListener("click", () => toggleActiveClass(false));
}

// הטעינת תגיות
function loadTags() {
    fetch(apiBase + "/Users/AllTags")
        .then(res => res.json())
        .then(tags => {
            const container = document.getElementById("signupTagsContainer");
            if (!container) return;

            container.innerHTML = "";

            tags.forEach(tag => {
                const tagBubble = document.createElement("div");
                tagBubble.className = "tag-bubble";
                tagBubble.innerHTML = `
                    <input type="checkbox" id="tag-${tag.id}" value="${tag.id}">
                    <label for="tag-${tag.id}">${tag.name}</label>
                `;

                const checkbox = tagBubble.querySelector('input[type="checkbox"]');
                const label = tagBubble.querySelector('label');

                label.addEventListener('click', function (e) {
                    e.preventDefault();

                    if (!checkbox.checked) {
                        checkbox.checked = true;
                        label.style.background = 'var(--primary-slate)';
                        label.style.color = 'white';
                    } else {
                        tagBubble.classList.add('exploding');
                        setTimeout(() => {
                            checkbox.checked = false;
                            tagBubble.classList.remove('exploding');
                            label.style.background = '';
                            label.style.color = '';
                        }, 600);
                    }
                });

                container.appendChild(tagBubble);
            });
        })
        .catch(error => {
            console.error('Error loading tags:', error);
        });
}

// כפתורים בטפסים (מובייל)
signupSwitches.forEach(btn =>
    btn.addEventListener("click", () => toggleActiveClass(false))
);

signinSwitches.forEach(btn =>
    btn.addEventListener("click", () => toggleActiveClass(true))
);

// JQuery on document ready
$(document).ready(function () {
    // התחברות
    $("#signinFormSubmit").submit(function (e) {
        e.preventDefault();
        const email = $("#signinEmail").val().trim();
        const password = $("#signinPassword").val().trim();

        if (!email || !password) {
            $("#signinError").text("Please enter email and password");
            $("#signinError").addClass("show");
            return;
        }

        fetch("/api/Users/Login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password })
        })
            .then(res => {
                if (res.status === 403) throw new Error("blocked");
                if (!res.ok) throw new Error("invalid");
                return res.json();
            })
            .then(user => {
                if (!user.active) {
                    alert("🚫 Your account is blocked.");
                    return;
                }

                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                sessionStorage.setItem("canShare", user.canShare);
                sessionStorage.setItem("canComment", user.canComment);
                window.location.href = "../html/index.html";
            })
            .catch(err => {
                if (err.message === "blocked")
                    alert("🚫 Your account is blocked.");
                else {
                    $("#signinError").text("Invalid email or password");
                    $("#signinError").addClass("show");
                }
            });
    });

    // הרשמה
    $("#signupFormSubmit").submit(function (e) {
        e.preventDefault();

        const name = $("#signupName").val().trim();
        const email = $("#signupEmail").val().trim();
        const password = $("#signupPassword").val().trim();

        if (!name || !email || !password) {
            const msg = "Please fill in all fields";
            $("#signupError").text(msg);
            $("#signupError").addClass("show");
            alert(msg);
            return;
        }

        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/;
        if (!passwordRegex.test(password)) {
            const msg = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character";
            $("#signupError").text(msg);
            $("#signupError").addClass("show");
            alert(msg);
            return;
        }

        const selectedTags = [];
        $("#signupTagsContainer input:checked").each(function () {
            selectedTags.push(parseInt($(this).val()));
        });

        fetch("/api/Users/Register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                name,
                email,
                password,
                tags: selectedTags
            })
        })
            .then(async res => {
                if (!res.ok) {
                    const text = await res.text();

                    let msg = "❌ Registration failed.";
                    if (text === "email")
                        msg = "⚠️ Email is already in use";
                    else if (text === "name")
                        msg = "⚠️ Username is already taken";

                    $("#signupError").text(msg);
                    $("#signupError").addClass("show");
                    alert(msg);
                    throw new Error(msg);
                }

                return res.json();
            })
            .then(user => {
                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                window.location.href = "../html/index.html";
            })
            .catch(err => {
                console.error("Registration error:", err.message);
            });
    });


});

// שמירת תחומי עניין
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
