// --- API BASE ---
const API_BASE = location.hostname.includes("localhost")
    ? "https://localhost:7084/api"
    : "https://proj.ruppin.ac.il/cgroup13/test2/tar1/api";


// ✅ On page load – fetch updated user and load profile
document.addEventListener("DOMContentLoaded", () => {
    const rawUser = sessionStorage.getItem("loggedUser");
    if (!rawUser) {
        window.location.href = "index.html";
        return;
    }

    const user = JSON.parse(rawUser);

    fetch(`${API_BASE}/Users/GetUserById/${user.id}`)
        .then(res => res.json())
        .then(updatedUser => {
            sessionStorage.setItem("loggedUser", JSON.stringify(updatedUser));
            loadUserProfile(updatedUser);
        })
        .catch(() => {
            showNotification("Failed to load user data", "error");
        });
});

// ✅ Load user profile info into page
function loadUserProfile(user) {
    document.getElementById("profileName").innerText = user.name;
    document.getElementById("profileEmail").innerText = user.email;

    const notificationToggle = document.getElementById("notificationToggle");
    if (notificationToggle) {
        notificationToggle.checked = user.receiveNotifications === true;
    }

    loadSavedProfileImage();
    loadUserTags();
    loadAllTags();
    loadBlockedUsers();
    loadAvatarLevel(user.avatarLevel);
}

// ✅ Load saved profile image or fallback to default
function loadSavedProfileImage() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const img = document.getElementById('profileImage');
    const icon = document.getElementById('avatarIcon');

    // בסיס פרונט (tar5) מחושב לפי הנתיב הנוכחי של הדף
    const path = location.pathname;
    let frontBase = "";
    const splitAt = "/html/";
    const idx = path.indexOf(splitAt);
    if (idx !== -1) frontBase = path.slice(0, idx); // למשל: /cgroup13/test2/tar5

    // בסיס אפליקציית ה-API (tar1) מחושב מתוך API_BASE
    const apiAppBase = API_BASE.replace(/\/api\/?$/, "");

    let profileImage;

    if (user && user.profileImagePath) {
        let p = user.profileImagePath;
        if (/^https?:\/\//i.test(p)) {
            // URL מלא – משתמשים כמו שהוא
            profileImage = p;
        } else {
            if (!p.startsWith("/")) p = "/" + p;
            // uploads/images מגיעים מהשרת של ה-API (tar1)
            if (p.startsWith("/uploads") || p.startsWith("/images")) {
                profileImage = apiAppBase + p;
            } else {
                // כל השאר – סטטיים של הפרונט (tar5)
                profileImage = frontBase + p;
            }
        }
    } else {
        // תמונת ברירת מחדל מהפרונט (tar5)
        profileImage = frontBase + "/pictures/default-avatar.jpg";
    }

    img.src = profileImage;
    img.style.display = 'block';
    if (icon) icon.style.display = 'none';
}


// ✅ Trigger file upload input (when clicking upload button)
function triggerImageUpload() {
    document.getElementById('imageUpload').click();
}

// ✅ Handle image upload and update UI + sessionStorage
function handleImageUpload(event) {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
        showNotification("Image size must be under 5MB", "warning");
        return;
    }
    if (!file.type.startsWith('image/')) {
        showNotification("Invalid image file", "warning");
        return;
    }

    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const formData = new FormData();
    formData.append("file", file);

    fetch(`${API_BASE}/Users/UploadProfileImage?userId=${user.id}`, {
        method: "POST",
        body: formData
    })
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(data => {
            // חישוב בסיסים (כמו בפונקציה הקודמת)
            const path = location.pathname;
            let frontBase = "";
            const splitAt = "/html/";
            const idx = path.indexOf(splitAt);
            if (idx !== -1) frontBase = path.slice(0, idx);
            const apiAppBase = API_BASE.replace(/\/api\/?$/, "");

            // הנתיב שחזר מהשרת (יכול להיות יחסי או מלא)
            let p = data.imageUrl || "";
            let resolved;

            if (/^https?:\/\//i.test(p)) {
                resolved = p;
            } else {
                if (!p.startsWith("/")) p = "/" + p;
                if (p.startsWith("/uploads") || p.startsWith("/images")) {
                    resolved = apiAppBase + p;     // קבצים שנשמרים בשרת ה-API
                } else {
                    resolved = frontBase + p;      // סטטיים של הפרונט
                }
            }

            document.getElementById("profileImage").src = resolved;
            const icon = document.getElementById("avatarIcon");
            if (icon) icon.style.display = "none";

            // שמירה ב-sessionStorage את הנתיב המקורי שחזר מהשרת
            const u = JSON.parse(sessionStorage.getItem("loggedUser"));
            u.profileImagePath = data.imageUrl || "";
            sessionStorage.setItem("loggedUser", JSON.stringify(u));

            showNotification("Image uploaded!", "success");
            setTimeout(() => window.location.reload(), 1000);
        })
        .catch(() => {
            showNotification("Failed to upload image", "error");
        });
}

// ✅ Load tags that the user has already selected
function loadUserTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("userTags");
    container.innerHTML = '<div class="loading-placeholder">Loading your interests...</div>';

    fetch(`${API_BASE}/Users/GetTags/${user.id}`)
        .then(res => res.json())
        .then(tags => {
            container.innerHTML = "";

            if (tags.length === 0) {
                container.innerHTML = '<div class="empty-state">No interests selected yet</div>';
                return;
            }

            tags.forEach(tag => {
                const tagElement = document.createElement("div");
                tagElement.className = "user-tag";
                tagElement.innerHTML = `
                    <span>${tag.name}</span>
                    <button class="remove-tag-btn" onclick="removeTag(${tag.id})" title="Remove ${tag.name}">
                        ×
                    </button>
                `;
                container.appendChild(tagElement);
            });
        })
        .catch(() => {
            container.innerHTML = '<div class="error-state">Failed to load interests</div>';
            showNotification("Failed to load user tags", "error");
        });
}

// ✅ Load all available tags for the user to choose from
function loadAllTags() {
    const container = document.getElementById("allTagsContainer");
    container.innerHTML = '<div class="loading-placeholder">Loading available interests...</div>';

    fetch(`${API_BASE}/Users/AllTags`)
        .then(res => res.json())
        .then(tags => {
            container.innerHTML = "";

            tags.forEach(tag => {
                const tagElement = document.createElement("div");
                tagElement.className = "available-tag";
                const uniqueId = `available-tag-${tag.id}`;
                tagElement.innerHTML = `
                    <input type="checkbox" id="${uniqueId}" value="${tag.id}">
                    <label for="${uniqueId}">${tag.name}</label>
                `;
                container.appendChild(tagElement);
            });
        })
        .catch(() => {
            container.innerHTML = '<div class="error-state">Failed to load available interests</div>';
            showNotification("Failed to load tag list", "error");
        });
}


// ✅ Remove a tag from the user's interests
function removeTag(tagId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const tagElement = event.target.closest('.user-tag');
    tagElement.style.opacity = '0.5';

    fetch(`${API_BASE}/Users/RemoveTag`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, tagId })
    })
        .then(response => {
            if (response.ok) {
                loadUserTags();
                showNotification("Interest removed successfully!", "success");
            } else {
                throw new Error();
            }
        })
        .catch(() => {
            tagElement.style.opacity = '1';
            showNotification("Failed to remove interest", "error");
        });
}

// ✅ Add selected tags to user interests
function addSelectedTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const selected = document.querySelectorAll("#allTagsContainer input:checked");

    if (selected.length === 0) {
        showNotification("Please select at least one interest", "warning");
        return;
    }

    const button = event.target;
    const originalText = button.innerHTML;
    button.classList.add('loading');
    button.disabled = true;
    button.innerHTML = '<span>⏳</span><span>Adding...</span>';

    const promises = Array.from(selected).map(checkbox => {
        return fetch(`${API_BASE}/Users/AddTag`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, tagId: parseInt(checkbox.value) })
        });
    });

    Promise.all(promises)
        .then(responses => {
            const allSuccessful = responses.every(res => res.ok);
            if (allSuccessful) {
                selected.forEach(chk => chk.checked = false);
                loadUserTags();
                showNotification(`Successfully added ${selected.length} interest(s)!`, "success");
            } else {
                throw new Error();
            }
        })
        .catch(() => {
            showNotification("Failed to add some interests", "error");
        })
        .finally(() => {
            button.classList.remove('loading');
            button.disabled = false;
            button.innerHTML = originalText;
        });
}

// ✅ Update user's password with validation
function updatePassword() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const newPassword = document.getElementById("newPassword").value.trim();

    if (newPassword.length < 8) {
        showNotification("Password must be at least 8 characters", "warning");
        return;
    }

    const passwordRegex = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{8,}$/;
    if (!passwordRegex.test(newPassword)) {
        showNotification("Password must include uppercase, number, and special character", "warning");
        return;
    }

    const button = event.target;
    const originalText = button.innerHTML;
    button.classList.add('loading');
    button.disabled = true;
    button.innerHTML = '<span>⏳</span><span>Updating...</span>';

    fetch(`${API_BASE}/Users/UpdatePassword`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, newPassword })
    })
        .then(res => {
            if (res.ok) {
                document.getElementById("newPassword").value = "";
                showNotification("Password updated!", "success");
            } else {
                throw new Error();
            }
        })
        .catch(() => {
            showNotification("Failed to update password", "error");
        })
        .finally(() => {
            button.classList.remove('loading');
            button.disabled = false;
            button.innerHTML = originalText;
        });
}

// ✅ Load list of users blocked by current user
function loadBlockedUsers() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("blockedUsersContainer");
    if (!container) return;

    container.innerHTML = "<div class='loading-placeholder'>Loading blocked users...</div>";

    fetch(`${API_BASE}/Users/BlockedByUser/${user.id}`)
        .then(res => res.json())
        .then(data => {
            if (data.length === 0) {
                container.innerHTML = "<div class='empty-state'>No blocked users</div>";
                return;
            }

            container.innerHTML = "";
            data.forEach(u => {
                const div = document.createElement("div");
                div.className = "blocked-user-item";
                div.innerHTML = `
                                <strong>${u.name}</strong>
                                <button class="btn btn-danger" onclick="unblockUser(${u.id}, event)">Unblock</button>
                            `;

                container.appendChild(div);
            });
        })
        .catch(() => {
            container.innerHTML = "<div class='error-state'>Failed to load blocked users</div>";
            showNotification("Error loading blocked users", "error");
        });
}

// ✅ Sends a request to unblock a user
// ✅ Sends a request to unblock a user
function unblockUser(blockedUserId, ev) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }
    if (!confirm("Are you sure you want to unblock this user?")) return;

    // תופסים את ה־DOM של הפריט באופן בטוח: קודם ev, ואז ניסיון fallback (כרום)
    const userItem = (ev && ev.target ? ev.target : (window.event && window.event.target))?.closest('.blocked-user-item');
    if (userItem) userItem.style.opacity = '0.5';

    fetch(`${API_BASE}/Users/UnblockUser`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            blockerUserId: user.id,
            blockedUserId: blockedUserId
        })
    })
        .then(res => res.ok ? res.json() : res.text().then(t => { throw new Error(t || "Unblock failed"); }))
        .then(data => {
            showNotification((data && data.message) ? data.message : "User unblocked", "success");
            loadBlockedUsers(); // ריענון הרשימה
        })
        .catch(err => {
            if (userItem) userItem.style.opacity = '1';
            showNotification(err.message || "Failed to unblock user", "error");
        });
}





// ✅ Load avatar level and matching icon
function loadAvatarLevel(level) {
    const avatarLabel = document.getElementById("avatarRank");
    const avatarImage = document.getElementById("avatarRankImage");
    if (!avatarLabel || !avatarImage) return;

    avatarLabel.innerText = level;

    // בסיס פרונט (tar5) לפי מיקום הדף
    const path = location.pathname;
    let frontBase = "";
    const splitAt = "/html/";
    const idx = path.indexOf(splitAt);
    if (idx !== -1) frontBase = path.slice(0, idx);

    const avatarIcons = {
        "BRONZE": frontBase + "/pictures/avatar_bronze.png",
        "SILVER": frontBase + "/pictures/avatar_silver.png",
        "GOLD": frontBase + "/pictures/avatar_gold.png"
    };

    avatarImage.src = avatarIcons[level] || (frontBase + "/pictures/avatar_bronze.png");
}


// ✅ Toggle notifications setting and save to server
function toggleNotifications() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const isEnabled = document.getElementById("notificationToggle").checked;

    fetch(`${API_BASE}/Users/ToggleNotifications`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, enable: isEnabled })
    })
        .then(res => {
            if (res.ok) {
                user.receiveNotifications = isEnabled;
                sessionStorage.setItem("loggedUser", JSON.stringify(user));
                showNotification("Notification preferences updated", "success");
            } else {
                throw new Error();
            }
        })
        .catch(() => {
            showNotification("Failed to update preferences", "error");
        });
}


// ✅ Show a notification message with styling and auto-hide
function showNotification(message, type = 'info') {
    document.querySelectorAll('.notification').forEach(n => n.remove());

    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-icon">${getNotificationIcon(type)}</span>
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
    `;

    notification.className = `notification notification-${type}`;

    document.body.appendChild(notification);

    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = 'slideOutRight 0.3s ease-in';
            setTimeout(() => notification.remove(), 300);
        }
    }, 5000);
}

// ✅ Get icon emoji by type
function getNotificationIcon(type) {
    const icons = {
        success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
    };
    return icons[type] || icons.info;
}

// ✅ Get color gradient by type
function getNotificationColor(type) {
    const colors = {
        success: 'linear-gradient(135deg, #28a745, #20c997)',
        error: 'linear-gradient(135deg, #dc3545, #e74c3c)',
        warning: 'linear-gradient(135deg, #ffc107, #f39c12)',
        info: 'linear-gradient(135deg, #4285f4, #6fa8f5)'
    };
    return colors[type] || colors.info;
}
