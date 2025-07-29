document.addEventListener("DOMContentLoaded", () => {
    loadUsers();
    loadAllStats();
    loadReports();
    setupImportExternal();
    setupTagging();
    setupFixMissingImages();

    // Add fade-in animations
    setTimeout(() => {
        document.querySelectorAll('.fade-in').forEach((el, index) => {
            setTimeout(() => {
                el.style.opacity = '1';
                el.style.transform = 'translateY(0)';
            }, index * 200);
        });
    }, 100);
});

function loadUsers() {
    fetch("/api/Users/AllUsers")
        .then(res => res.json())
        .then(users => {
            const tbody = document.querySelector("#usersTable tbody");
            tbody.innerHTML = "";
            users.forEach(u => {
                const activeStatus = u.active ?
                    '<span class="status-true status-active">Active</span>' :
                    '<span class="status-false status-inactive">Inactive</span>';

                const shareStatus = u.canShare ?
                    '<span class="status-true">Yes</span>' :
                    '<span class="status-false">No</span>';

                const commentStatus = u.canComment ?
                    '<span class="status-true">Yes</span>' :
                    '<span class="status-false">No</span>';

                tbody.innerHTML += `
                    <tr>
                        <td><strong>${u.name}</strong></td>
                        <td>${u.email}</td>
                        <td>${activeStatus}</td>
                        <td>${shareStatus}</td>
                        <td>${commentStatus}</td>
                        <td>
                            <button class="action-btn ${u.active ? 'danger' : 'success'}" onclick="setStatus(${u.id}, ${!u.active})">
                                ${u.active ? "BAN" : "UNBAN"}
                            </button>
                            <button class="action-btn ${u.canShare ? 'warning' : 'success'}" onclick="toggleSharing(${u.id}, ${!u.canShare})">
                                ${u.canShare ? "BLOCK SHARE" : "ALLOW SHARE"}
                            </button>
                            <button class="action-btn ${u.canComment ? 'warning' : 'success'}" onclick="toggleCommenting(${u.id}, ${!u.canComment})">
                                ${u.canComment ? "BLOCK COMMENT" : "ALLOW COMMENT"}
                            </button>
                        </td>
                    </tr>`;
            });
        })
        .catch(err => {
            console.error("Error loading users:", err);
            const tbody = document.querySelector("#usersTable tbody");
            tbody.innerHTML = '<tr><td colspan="6" class="text-center">❌ Failed to load users</td></tr>';
        });
}

function setStatus(userId, isActive) {
    fetch("/api/Users/SetActiveStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, isActive })
    }).then(() => loadUsers())
        .catch(err => console.error("Error setting status:", err));
}

function loadAllStats() {
    Promise.all([
        fetch("/api/Users/GetStatistics").then(res => res.json()),
        fetch("/api/Admin/LikesStats").then(res => res.json())
    ])
        .then(([stats, likes]) => {
            const container = document.getElementById("statsContainer");
            if (!container) return;

            container.innerHTML = `
                <div class="stat-card">
                    <span class="stat-value">${stats.totalUsers || 0}</span>
                    <span class="stat-label">👥 Total Users</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${stats.totalArticles || 0}</span>
                    <span class="stat-label">📰 Total Articles</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${stats.totalSaved || 0}</span>
                    <span class="stat-label">💾 Saved Articles</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${stats.todayLogins || 0}</span>
                    <span class="stat-label">🔐 Today's Logins</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${stats.todayFetches || 0}</span>
                    <span class="stat-label">📊 Today's Fetches</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${likes.articleLikes || 0}</span>
                    <span class="stat-label">❤️ Article Likes</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${likes.articleLikesToday || 0}</span>
                    <span class="stat-label">💕 Today's Article Likes</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${likes.threadLikes || 0}</span>
                    <span class="stat-label">🧵 Thread Likes</span>
                </div>
                <div class="stat-card">
                    <span class="stat-value">${likes.threadLikesToday || 0}</span>
                    <span class="stat-label">🔥 Today's Thread Likes</span>
                </div>
            `;
        })
        .catch(err => {
            console.error("Error loading statistics:", err);
            const container = document.getElementById("statsContainer");
            if (container) {
                container.innerHTML = `
                    <div class="stat-card">
                        <span class="stat-value">❌</span>
                        <span class="stat-label">Error Loading Stats</span>
                    </div>
                `;
            }
        });
}

function toggleSharing(userId, canShare) {
    fetch("/api/Users/SetSharingStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canShare })
    }).then(() => loadUsers())
        .catch(err => console.error("Error toggling sharing:", err));
}

function toggleCommenting(userId, canComment) {
    fetch("/api/Users/SetCommentingStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canComment })
    }).then(() => loadUsers())
        .catch(err => console.error("Error toggling commenting:", err));
}

function loadReports() {
    fetch("/api/Admin/AllReports")
        .then(res => {
            if (!res.ok) throw new Error("Failed to load reports");
            return res.json();
        })
        .then(reports => {
            const container = document.getElementById("reportsContainer");
            if (!container) return;

            if (reports.length === 0) {
                container.innerHTML = `
                    <div class="no-data-state">
                        <span class="icon">🎉</span>
                        <h3>No Reports!</h3>
                        <p>All clear - no reports to review.</p>
                    </div>
                `;
                return;
            }

            let html = `
                <div class="table-container">
                    <table class="admin-table reports-table">
                        <thead>
                            <tr>
                                <th>Reporter</th>
                                <th>Target</th>
                                <th>Type</th>
                                <th>Reason</th>
                                <th>Content</th>
                                <th>Date</th>
                            </tr>
                        </thead>
                        <tbody>
            `;

            reports.forEach(r => {
                const formattedDate = new Date(r.reportedAt).toLocaleDateString('en-US', {
                    month: 'short',
                    day: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                });

                html += `
                    <tr>
                        <td><strong>${r.reporterName}</strong></td>
                        <td>${r.targetName || "–"}</td>
                        <td><span style="background: var(--accent-red); color: white; padding: 0.2rem 0.5rem; border-radius: 4px; font-size: 0.8rem;">${r.reportType}</span></td>
                        <td>${r.reason}</td>
                        <td class="report-content">${r.content}</td>
                        <td class="report-date">${formattedDate}</td>
                    </tr>
                `;
            });

            html += "</tbody></table></div>";
            container.innerHTML = html;
        })
        .catch(err => {
            console.error("Error loading reports:", err);
            const container = document.getElementById("reportsContainer");
            if (container) {
                container.innerHTML = `
                    <div class="status-message error">
                        ⚠️ Failed to load reports
                    </div>
                `;
            }
        });
}

function setupImportExternal() {
    const importBtn = document.getElementById("importBtn");

    if (importBtn) {
        importBtn.addEventListener("click", function () {
            // Show loading state
            importBtn.disabled = true;
            importBtn.innerHTML = '⏳ Importing...';

            fetch("/api/Articles/ImportExternal", {
                method: "POST"
            })
                .then(res => {
                    if (!res.ok) throw new Error("Import failed");
                    return res.json();
                })
                .then(data => {
                    document.getElementById("importStatus").innerHTML =
                        `<div class='status-message success'>✅ ${data.length} new articles were imported successfully!</div>`;
                })
                .catch(err => {
                    document.getElementById("importStatus").innerHTML =
                        `<div class='status-message error'>❌ Error: ${err.message}</div>`;
                })
                .finally(() => {
                    // Reset button
                    importBtn.disabled = false;
                    importBtn.innerHTML = '📥 Import Articles';
                });
        });
    }
}

function setupTagging() {
    const taggingBtn = document.getElementById("taggingBtn");

    if (taggingBtn) {
        taggingBtn.addEventListener("click", function () {
            // Show loading state
            taggingBtn.disabled = true;
            taggingBtn.innerHTML = '⏳ Tagging...';

            fetch("/api/Tagging/Run", {
                method: "POST"
            })
                .then(res => {
                    if (!res.ok) throw new Error("Tagging failed");
                    return res.text();
                })
                .then(msg => {
                    document.getElementById("taggingStatus").innerHTML =
                        `<div class='status-message success'>🏷️ Tagging completed successfully!</div>`;
                })
                .catch(err => {
                    document.getElementById("taggingStatus").innerHTML =
                        `<div class='status-message error'>❌ Error: ${err.message}</div>`;
                })
                .finally(() => {
                    // Reset button
                    taggingBtn.disabled = false;
                    taggingBtn.innerHTML = '🏷️ Tag Articles';
                });
        });
    }
}

// Add some utility functions for better UX
function showNotification(message, type = 'success') {
    const notification = document.createElement('div');
    notification.className = `status-message ${type}`;
    notification.style.position = 'fixed';
    notification.style.top = '100px';
    notification.style.right = '20px';
    notification.style.zIndex = '10000';
    notification.style.minWidth = '300px';
    notification.innerHTML = message;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.opacity = '0';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}

// Enhanced error handling
window.addEventListener('error', (e) => {
    console.error('Global error:', e.error);
});

// Add loading states to all action buttons
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('action-btn')) {
        const btn = e.target;
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '⏳';

        setTimeout(() => {
            btn.disabled = false;
            btn.innerHTML = originalText;
        }, 1000);
    }
});

function setupFixMissingImages() {
    const fixImagesBtn = document.getElementById("fixImagesBtn");

    if (fixImagesBtn) {
        fixImagesBtn.addEventListener("click", function () {
            fixImagesBtn.disabled = true;
            fixImagesBtn.innerHTML = '⏳ Fixing...';

            fetch("/api/Articles/FixMissingImages", {
                method: "POST"
            })
                .then(res => {
                    if (!res.ok) throw new Error("Fixing failed");
                    return res.json();
                })
                .then(data => {
                    document.getElementById("fixImagesStatus").innerHTML = `
                        <div class='status-message success'>
                            ✅ Fixed: ${data.success}, Skipped: ${data.skippedDueToContentPolicy}, Failed: ${data.failed}
                        </div>`;
                })
                .catch(err => {
                    document.getElementById("fixImagesStatus").innerHTML = `
                        <div class='status-message error'>❌ Error: ${err.message}</div>`;
                })
                .finally(() => {
                    fixImagesBtn.disabled = false;
                    fixImagesBtn.innerHTML = '🖼️ Fix Missing Images';
                });
        });
    }
}
