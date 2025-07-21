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

    // Load user data
    loadUserTags();
    loadAllTags();
    loadBlockedUsers();

});

function loadUserTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("userTags");

    // Show loading state
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

function loadAllTags() {
    const container = document.getElementById("allTagsContainer");

    // Show loading state
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

function removeTag(tagId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    // Show confirmation
    if (!confirm("Are you sure you want to remove this interest?")) {
        return;
    }

    // Add loading state to the specific tag
    const tagElement = event.target.closest('.user-tag');
    tagElement.classList.add('loading');

    fetch(`${apiBase}/RemoveTag`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, tagId })
    })
        .then(response => {
            if (response.ok) {
                // Add remove animation
                tagElement.style.transform = 'scale(0)';
                tagElement.style.opacity = '0';

                setTimeout(() => {
                    loadUserTags();
                    showNotification("Interest removed successfully!", "success");
                }, 300);
            } else {
                throw new Error('Failed to remove tag');
            }
        })
        .catch(error => {
            console.error('Error removing tag:', error);
            tagElement.classList.remove('loading');
            showNotification("Failed to remove interest. Please try again.", "error");
        });
}

function addSelectedTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const selected = document.querySelectorAll("#allTagsContainer input:checked");

    if (selected.length === 0) {
        showNotification("Please select at least one interest to add.", "warning");
        return;
    }

    // Add loading state to button
    const button = event.target;
    button.classList.add('loading');
    button.disabled = true;

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
                // Uncheck all selected checkboxes
                selected.forEach(checkbox => {
                    checkbox.checked = false;
                    const label = checkbox.nextElementSibling;
                    label.style.background = '';
                    label.style.color = '';
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
        });
}

function updatePassword() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const newPassword = document.getElementById("newPassword").value.trim();

    // Validation
    if (newPassword.length < 8) {
        showNotification("Password must be at least 8 characters long.", "warning");
        return;
    }

    const passwordRegex = /^(?=.*[A-Z])(?=.*\d).{8,}$/;
    if (!passwordRegex.test(newPassword)) {
        showNotification("Password must contain at least 1 uppercase letter and 1 number.", "warning");
        return;
    }

    // Add loading state to button
    const button = event.target;
    button.classList.add('loading');
    button.disabled = true;

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
        });
}

// Notification system
function showNotification(message, type = 'info') {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-icon">${getNotificationIcon(type)}</span>
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
    `;

    // Add styles
    notification.style.cssText = `
        position: fixed;
        top: 100px;
        right: 20px;
        background: ${getNotificationColor(type)};
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 12px;
        box-shadow: 0 8px 25px rgba(0,0,0,0.15);
        z-index: 10000;
        animation: slideInRight 0.3s ease-out;
        max-width: 400px;
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
        success: 'linear-gradient(135deg, #10b981, #059669)',
        error: 'linear-gradient(135deg, #ef4444, #dc2626)',
        warning: 'linear-gradient(135deg, #f59e0b, #d97706)',
        info: 'linear-gradient(135deg, #3b82f6, #2563eb)'
    };
    return colors[type] || colors.info;
}

function loadBlockedUsers() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const container = document.getElementById("blockedUsersContainer");
    if (!container) return;

    container.innerHTML = "<p>Loading blocked users...</p>";

    fetch(`/api/Users/BlockedByUser/${user.id}`)
        .then(res => res.json())
        .then(data => {
            if (data.length === 0) {
                container.innerHTML = "<p>You haven't blocked anyone.</p>";
                return;
            }

            container.innerHTML = "";
            data.forEach(u => {
                const div = document.createElement("div");
                div.className = "blocked-user-item";
                div.innerHTML = `
                    <strong>${u.name}</strong>
                    <button class="btn btn-sm btn-outline-danger ms-2" onclick="unblockUser(${u.id})">Unblock</button>
                `;
                container.appendChild(div);
            });
        })
        .catch(err => {
            console.error("Error loading blocked users", err);
            container.innerHTML = "<p class='text-danger'>Failed to load blocked users.</p>";
        });
}


function unblockUser(blockedUserId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!confirm("Are you sure you want to unblock this user?")) return;

    fetch("/api/Users/UnblockUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ blockerUserId: user.id, blockedUserId })
    })
        .then(res => {
            if (res.ok) {
                showNotification("✅ Unblocked successfully", "success");
                loadBlockedUsers();
            } else {
                throw new Error("Failed to unblock");
            }
        })
        .catch(() => showNotification("❌ Failed to unblock", "error"));
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
        gap: 0.5rem;
    }
    
    .notification-close {
        background: none;
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
        background: rgba(255,255,255,0.2);
    }
    
    .loading-placeholder, .empty-state, .error-state {
        color: var(--text-light);
        font-style: italic;
        text-align: center;
        padding: 2rem;
    }
    
    .error-state {
        color: #ef4444;
    }
`;
document.head.appendChild(style);

