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
    // התחברות
    $("#signinFormSubmit").submit(function (e) {
        e.preventDefault();
        const email = $("#signinEmail").val().trim();
        const password = $("#signinPassword").val().trim();

        if (!email || !password) {
            $("#signinError").text("Please enter email and password");
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
                else
                    $("#signinError").text("Invalid email or password");
            });
    });

    // הרשמה
    $("#signupFormSubmit").submit(function (e) {
        e.preventDefault();

        const name = $("#signupName").val().trim();
        const email = $("#signupEmail").val().trim();
        const password = $("#signupPassword").val().trim();

        if (!name || !email || !password) {
            $("#signupError").text("Please fill in all fields");
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
            .then(res => {
                if (!res.ok) throw new Error("Failed to register");
                return res.json();
            })
            .then(user => {
                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                window.location.href = "../html/index.html";
            })
            .catch(err => {
                console.error(err);
                $("#signupError").text("Registration failed. Try a different email.");
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