const apiBase = "/api/Users";

document.addEventListener("DOMContentLoaded", () => {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user) {
        alert("Please log in");
        window.location.href = "index.html";
        return;
    }

    // Load user information
    document.getElementById("profileName").innerText = user.name;
    document.getElementById("profileEmail").innerText = user.email;

    // Load saved profile image
    loadSavedProfileImage();

    // Load user data
    loadUserTags();
    loadAllTags();
    loadBlockedUsers();
});

// ✅ LOAD SAVED PROFILE IMAGE
function loadSavedProfileImage() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const img = document.getElementById('profileImage');
    const icon = document.getElementById('avatarIcon');

    const profileImage = user.profileImagePath || "../pictures/default-avatar.jpg";

    img.src = profileImage;
    img.style.display = 'block';
    icon.style.display = 'none';
}



// ✅ IMAGE UPLOAD FUNCTIONALITY
function triggerImageUpload() {
    document.getElementById('imageUpload').click();
}

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

    fetch(`/api/Users/UploadProfileImage?userId=${user.id}`, {
        method: "POST",
        body: formData
    })
        .then(res => {
            if (!res.ok) throw new Error("Upload failed");
            return res.json();
        })
        .then(data => {
            document.getElementById("profileImage").src = data.imageUrl;
            document.getElementById("avatarIcon").style.display = "none";

            // שמירה ב-sessionStorage כדי לעדכן גם בצד הלקוח
            user.profileImagePath = data.imageUrl;
            sessionStorage.setItem("loggedUser", JSON.stringify(user));

            showNotification("✅ Image uploaded!", "success");
        })
        .catch(err => {
            console.error("Error uploading image:", err);
            showNotification("❌ Failed to upload image", "error");
        });
}

// ✅ LOAD USER TAGS
function loadUserTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("userTags");

    container.innerHTML = '<div class="loading-placeholder">Loading your interests...</div>';

    fetch(`${apiBase}/GetTags/${user.id}`)
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
        .catch(error => {
            console.error('Error loading user tags:', error);
            container.innerHTML = '<div class="error-state">Failed to load interests</div>';
        });
}

// ✅ LOAD ALL AVAILABLE TAGS
function loadAllTags() {
    const container = document.getElementById("allTagsContainer");

    container.innerHTML = '<div class="loading-placeholder">Loading available interests...</div>';

    fetch(`${apiBase}/AllTags`)
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
        .catch(error => {
            console.error('Error loading all tags:', error);
            container.innerHTML = '<div class="error-state">Failed to load available interests</div>';
        });
}

// ✅ REMOVE TAG
function removeTag(tagId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    const tagElement = event.target.closest('.user-tag');
    tagElement.style.opacity = '0.5';

    fetch(`${apiBase}/RemoveTag`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, tagId })
    })
        .then(response => {
            if (response.ok) {
                loadUserTags();
                showNotification("Interest removed successfully!", "success");
            } else {
                throw new Error('Failed to remove tag');
            }
        })
        .catch(error => {
            console.error('Error removing tag:', error);
            tagElement.style.opacity = '1';
            showNotification("Failed to remove interest. Please try again.", "error");
        });
}

// ✅ ADD SELECTED TAGS
function addSelectedTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const selected = document.querySelectorAll("#allTagsContainer input:checked");

    if (selected.length === 0) {
        showNotification("Please select at least one interest to add.", "warning");
        return;
    }

    const button = event.target;
    const originalText = button.innerHTML;
    button.classList.add('loading');
    button.disabled = true;
    button.innerHTML = '<span>⏳</span><span>Adding...</span>';

    const promises = Array.from(selected).map(checkbox => {
        return fetch(`${apiBase}/AddTag`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, tagId: parseInt(checkbox.value) })
        });
    });

    Promise.all(promises)
        .then(responses => {
            const allSuccessful = responses.every(response => response.ok);

            if (allSuccessful) {
                selected.forEach(checkbox => {
                    checkbox.checked = false;
                });

                loadUserTags();
                showNotification(`Successfully added ${selected.length} interest(s)!`, "success");
            } else {
                throw new Error('Some tags failed to add');
            }
        })
        .catch(error => {
            console.error('Error adding tags:', error);
            showNotification("Failed to add some interests. Please try again.", "error");
        })
        .finally(() => {
            button.classList.remove('loading');
            button.disabled = false;
            button.innerHTML = originalText;
        });
}

// ✅ UPDATE PASSWORD
function updatePassword() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const newPassword = document.getElementById("newPassword").value.trim();

    if (newPassword.length < 8) {
        showNotification("Password must be at least 8 characters long.", "warning");
        return;
    }

    const passwordRegex = /^(?=.*[A-Z])(?=.*\d).{8,}$/;
    if (!passwordRegex.test(newPassword)) {
        showNotification("Password must contain at least 1 uppercase letter and 1 number.", "warning");
        return;
    }

    const button = event.target;
    const originalText = button.innerHTML;
    button.classList.add('loading');
    button.disabled = true;
    button.innerHTML = '<span>⏳</span><span>Updating...</span>';

    fetch(`${apiBase}/UpdatePassword`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, newPassword })
    })
        .then(response => {
            if (response.ok) {
                document.getElementById("newPassword").value = "";
                showNotification("Password updated successfully!", "success");
            } else {
                throw new Error('Failed to update password');
            }
        })
        .catch(error => {
            console.error('Error updating password:', error);
            showNotification("Failed to update password. Please try again.", "error");
        })
        .finally(() => {
            button.classList.remove('loading');
            button.disabled = false;
            button.innerHTML = originalText;
        });
}

// ✅ LOAD BLOCKED USERS
function loadBlockedUsers() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("blockedUsersContainer");
    if (!container) return;

    container.innerHTML = "<div class='loading-placeholder'>Loading blocked users...</div>";

    fetch(`/api/Users/BlockedByUser/${user.id}`)
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
                    <button class="btn btn-danger" onclick="unblockUser(${u.id})">Unblock</button>
                `;
                container.appendChild(div);
            });
        })
        .catch(err => {
            console.error("Error loading blocked users", err);
            container.innerHTML = "<div class='error-state'>Failed to load blocked users</div>";
        });
}

// ✅ UNBLOCK USER
function unblockUser(blockedUserId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    if (!confirm("Are you sure you want to unblock this user?")) return;

    const userItem = event.target.closest('.blocked-user-item');
    userItem.style.opacity = '0.5';

    fetch("/api/Users/UnblockUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ blockerUserId: user.id, blockedUserId })
    })
        .then(res => {
            if (res.ok) {
                showNotification("User unblocked successfully", "success");
                loadBlockedUsers();
            } else {
                throw new Error("Failed to unblock");
            }
        })
        .catch(() => {
            userItem.style.opacity = '1';
            showNotification("Failed to unblock user", "error");
        });
}

// ✅ NOTIFICATION SYSTEM
function showNotification(message, type = 'info') {
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-icon">${getNotificationIcon(type)}</span>
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
    `;

    notification.style.cssText = `
        position: fixed;
        top: 120px;
        right: 20px;
        background: ${getNotificationColor(type)};
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 12px;
        box-shadow: 0 8px 25px rgba(0,0,0,0.15);
        z-index: 10000;
        animation: slideInRight 0.3s ease-out;
        max-width: 400px;
        font-weight: 500;
    `;

    document.body.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = 'slideOutRight 0.3s ease-in';
            setTimeout(() => notification.remove(), 300);
        }
    }, 5000);
}

function getNotificationIcon(type) {
    const icons = {
        success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
    };
    return icons[type] || icons.info;
}

function getNotificationColor(type) {
    const colors = {
        success: 'linear-gradient(135deg, #28a745, #20c997)',
        error: 'linear-gradient(135deg, #dc3545, #e74c3c)',
        warning: 'linear-gradient(135deg, #ffc107, #f39c12)',
        info: 'linear-gradient(135deg, #4285f4, #6fa8f5)'
    };
    return colors[type] || colors.info;
}

// Add notification animations to CSS
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    .notification-content {
        display: flex;
        align-items: center;
        gap: 0.8rem;
    }
    
    .notification-close {
        background: rgba(255,255,255,0.2);
        border: none;
        color: white;
        font-size: 1.2rem;
        cursor: pointer;
        margin-left: auto;
        padding: 0;
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 50%;
        transition: background 0.2s ease;
    }
    
    .notification-close:hover {
        background: rgba(255,255,255,0.3);
    }
`;
document.head.appendChild(style);