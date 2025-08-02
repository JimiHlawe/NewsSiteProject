// ✅ Base API URL
var apiBase = "https://localhost:7084/api";

// ✅ DOM elements
const container = document.getElementById("container");
const registerBtn = document.getElementById("register");
const loginBtn = document.getElementById("login");

// ✅ Mobile switch buttons
const signupSwitches = document.querySelectorAll(".signup .form-switch");
const signinSwitches = document.querySelectorAll(".signin .form-switch");

// ✅ Toggle between Sign Up and Sign In panels
function toggleActiveClass(showSignup) {
    container.classList.toggle("active", showSignup);
    if (showSignup) loadTags();
}

// ✅ Desktop switch buttons
if (registerBtn) {
    registerBtn.addEventListener("click", () => toggleActiveClass(true));
}
if (loginBtn) {
    loginBtn.addEventListener("click", () => toggleActiveClass(false));
}

// ✅ Notification system
function showNotification(message, type = "error") {
    document.querySelectorAll('.auth-notification').forEach(n => n.remove());

    const notification = document.createElement('div');
    notification.className = `auth-notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
    `;

    const bgColor = getNotificationColor(type);
    const isMobile = window.innerWidth <= 768;

    notification.style.cssText = `
        position: fixed;
        ${isMobile ? 'top: 15px; left: 15px; right: 15px;' : 'top: 20px; right: 20px; max-width: 400px;'}
        background: ${bgColor};
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 12px;
        box-shadow: 0 8px 25px rgba(0,0,0,0.15);
        z-index: 10000;
        animation: ${isMobile ? 'slideInTop' : 'slideInRight'} 0.3s ease-out;
        font-weight: 500;
        border: 1px solid rgba(255,255,255,0.2);
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = `${isMobile ? 'slideOutTop' : 'slideOutRight'} 0.3s ease-in`;
            setTimeout(() => notification.remove(), 300);
        }
    }, 5000);
}

// ✅ Return gradient background color based on type
function getNotificationColor(type) {
    const colors = {
        success: 'linear-gradient(135deg, #10b981, #059669)',
        error: 'linear-gradient(135deg, #ef4444, #dc2626)',
        warning: 'linear-gradient(135deg, #f59e0b, #d97706)',
        info: 'linear-gradient(135deg, #3b82f6, #2563eb)'
    };
    return colors[type] || colors.error;
}

// ✅ Load interest tags for signup
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

                const checkbox = tagBubble.querySelector("input");
                const label = tagBubble.querySelector("label");

                label.addEventListener("click", function (e) {
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
        .catch(() => {
            showNotification("Failed to load interest tags", "error");
        });
}

// ✅ Mobile switch buttons for forms
signupSwitches.forEach(btn => btn.addEventListener("click", () => toggleActiveClass(false)));
signinSwitches.forEach(btn => btn.addEventListener("click", () => toggleActiveClass(true)));

// ✅ On document ready – handle login and registration
$(document).ready(function () {
    // ✅ Sign In form
    $("#signinFormSubmit").submit(function (e) {
        e.preventDefault();

        const email = $("#signinEmail").val().trim();
        const password = $("#signinPassword").val().trim();

        if (!email || !password) {
            showNotification("Please enter email and password", "warning");
            return;
        }

        const submitBtn = $(this).find('button[type="submit"]');
        const originalText = submitBtn.text();
        submitBtn.text('Signing In...').prop('disabled', true);

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
                    showNotification("Your account is blocked. Please contact support.", "error");
                    return;
                }

                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                sessionStorage.setItem("canShare", user.canShare);
                sessionStorage.setItem("canComment", user.canComment);

                showNotification("Welcome back! Redirecting...", "success");
                setTimeout(() => {
                    window.location.href = "../html/index.html";
                }, 1500);
            })
            .catch(err => {
                if (err.message === "blocked") {
                    showNotification("Your account is blocked. Please contact support.", "error");
                } else {
                    showNotification("Invalid email or password. Please try again.", "error");
                }
            })
            .finally(() => {
                submitBtn.text(originalText).prop('disabled', false);
            });
    });

    // ✅ Sign Up form
    $("#signupFormSubmit").submit(function (e) {
        e.preventDefault();

        const name = $("#signupName").val().trim();
        const email = $("#signupEmail").val().trim();
        const password = $("#signupPassword").val().trim();

        if (!name || !email || !password) {
            showNotification("Please fill in all fields", "warning");
            return;
        }

        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/;
        if (!passwordRegex.test(password)) {
            showNotification("Password must be at least 8 characters and include uppercase, lowercase, number, and special character", "warning");
            return;
        }

        const selectedTags = [];
        $("#signupTagsContainer input:checked").each(function () {
            selectedTags.push(parseInt($(this).val()));
        });

        const submitBtn = $(this).find('button[type="submit"]');
        const originalText = submitBtn.text();
        submitBtn.text('Creating Account...').prop('disabled', true);

        fetch("/api/Users/Register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, email, password, tags: selectedTags })
        })
            .then(async res => {
                if (!res.ok) {
                    const text = await res.text();
                    let msg = "Registration failed. Please try again.";
                    if (text === "email") msg = "Email is already in use.";
                    else if (text === "name") msg = "Username is already taken.";
                    showNotification(msg, "error");
                    throw new Error(msg);
                }

                return res.json();
            })
            .then(user => {
                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                showNotification("Account created successfully! Welcome aboard!", "success");
                setTimeout(() => {
                    window.location.href = "../html/index.html";
                }, 1500);
            })
            .catch(() => { })
            .finally(() => {
                submitBtn.text(originalText).prop('disabled', false);
            });
    });
});

// ✅ Save selected interest tags for logged-in user
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

    showNotification("Interests saved successfully!", "success");
    setTimeout(() => {
        window.location.href = "index.html";
    }, 1000);
}
