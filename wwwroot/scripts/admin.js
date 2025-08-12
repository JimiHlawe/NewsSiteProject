// ======================================================
// Admin Page Script – simple, clean, student-friendly
// Shows users, reports, site stats, and a small chart
// ======================================================
// --- API BASE ---
const API_BASE = location.hostname.includes("localhost")
    ? "https://localhost:7084/api"
    : "https://proj.ruppin.ac.il/cgroup13/test2/tar1/api";


// ✅ Keep a reference to the chart so we can update/destroy it
let _dailyChart = null;

// ✅ On page load – initialize admin features
document.addEventListener("DOMContentLoaded", () => {
    // ✅ Check if user is logged in
    const rawUser = sessionStorage.getItem("loggedUser");
    if (!rawUser) {
        window.location.href = "/html/login.html";
        return;
    }

    // ✅ Load admin data
    loadUsers();
    loadAllStats();      // this will also render the chart
    loadReports();
    setupImportExternal();
    setupTagging();
    setupFixMissingImages();

    // ✅ Add fade-in animations (nice touch)
    setTimeout(() => {
        document.querySelectorAll('.fade-in').forEach((el, index) => {
            setTimeout(() => {
                el.style.opacity = '1';
                el.style.transform = 'translateY(0)';
            }, index * 200);
        });
    }, 100);
});

// ------------------------------------------------------
// Users table
// ------------------------------------------------------
function loadUsers() {
    fetch(`${API_BASE}/Users/AllUsers`)
        .then(res => res.json())
        .then(users => {
            const tbody = document.querySelector("#usersTable tbody");
            if (!tbody) return;
            tbody.innerHTML = "";
            users.forEach(u => {
                const activeStatus = u.active
                    ? '<span class="status-true status-active">Active</span>'
                    : '<span class="status-false status-inactive">Inactive</span>';

                const shareStatus = u.canShare
                    ? '<span class="status-true">Yes</span>'
                    : '<span class="status-false">No</span>';

                const commentStatus = u.canComment
                    ? '<span class="status-true">Yes</span>'
                    : '<span class="status-false">No</span>';

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
        .catch(() => {
            const tbody = document.querySelector("#usersTable tbody");
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center">Failed to load users</td></tr>';
            }
        });
}

// ✅ Set user active/inactive status
function setStatus(userId, isActive) {
    fetch(`${API_BASE}/Admin/SetActiveStatus`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, isActive })
    }).then(() => loadUsers());
}

// ------------------------------------------------------
// Stats + Daily Activity Chart
// ------------------------------------------------------
function loadAllStats() {
    Promise.all([
        fetch(`${API_BASE}/Admin/GetStatistics`)
        .then(res => res.json()),
        fetch(`${API_BASE}/Admin/LikesStats`)
        .then(res => res.json())
    ])
        .then(async ([stats, likes]) => {
            const container = document.getElementById("statsContainer");
            if (!container) return;

            // ✅ Render the stat cards
            container.innerHTML = `
                <div class="stat-card"><span class="stat-value">${stats.totalUsers || 0}</span><span class="stat-label"> Total Users</span></div>
                <div class="stat-card"><span class="stat-value">${stats.totalArticles || 0}</span><span class="stat-label"> Total Articles</span></div>
                <div class="stat-card"><span class="stat-value">${stats.totalSaved || 0}</span><span class="stat-label"> Saved Articles</span></div>
                <div class="stat-card"><span class="stat-value">${stats.todayLogins || 0}</span><span class="stat-label"> Today's Logins</span></div>
                <div class="stat-card"><span class="stat-value">${likes.articleLikes || 0}</span><span class="stat-label"> Article Likes</span></div>
                <div class="stat-card"><span class="stat-value">${likes.threadLikes || 0}</span><span class="stat-label"> Thread Likes</span></div>
                <div class="stat-card"><span class="stat-value">${stats.todayFetches || 0}</span><span class="stat-label"> Today's Fetches</span></div>
                <div class="stat-card"><span class="stat-value">${likes.articleLikesToday || 0}</span><span class="stat-label"> Today's Article Likes</span></div>
                <div class="stat-card"><span class="stat-value">${likes.threadLikesToday || 0}</span><span class="stat-label"> Today's Thread Likes</span></div>
            `;

            // ✅ Ensure we have a chart container right after the stats
            const chartHostId = "dailyChartContainer";
            let chartHost = document.getElementById(chartHostId);
            if (!chartHost) {
                chartHost = document.createElement("div");
                chartHost.id = chartHostId;
                chartHost.style.marginTop = "16px";
                chartHost.innerHTML = `
                      <div class="card">
                        <h3 style="margin:0 0 12px 0;">Today Overview</h3>
                        <canvas id="dailyActivityChart"></canvas>
                      </div>`;

                // Insert **after** statsContainer
                container.parentNode.insertBefore(chartHost, container.nextSibling);
            } else {
                // If exists, make sure canvas exists
                if (!chartHost.querySelector("#dailyActivityChart")) {
                    chartHost.innerHTML = `
                        <div class="card" style="padding:16px;">
                            <h3 style="margin:0 0 12px 0;">Today Overview</h3>
                            <canvas id="dailyActivityChart" height="120"></canvas>
                        </div>`;
                }
            }

            // ✅ Prepare data for the requested chart (exactly these three)
            const todayThreadLikes = Number(likes.threadLikesToday || 0);
            const todayFetches = Number(stats.todayFetches || 0);
            const todayLogins = Number(stats.todayLogins || 0);

            // ✅ Make sure Chart.js is available, then render the chart
            await ensureChartJsLoaded();
            renderDailyActivityChart(todayThreadLikes, todayFetches, todayLogins);
        })
        .catch(() => {
            const container = document.getElementById("statsContainer");
            if (container) {
                container.innerHTML = `
                <div class="stat-card">
                    <span class="stat-value">❌</span>
                    <span class="stat-label">Error Loading Stats</span>
                </div>`;
            }
        });
}

// ✅ Loads Chart.js from CDN only if it isn't already on the page
function ensureChartJsLoaded() {
    return new Promise((resolve) => {
        // Already loaded
        if (window.Chart) {
            resolve();
            return;
        }
        // Look for existing script tag
        if (document.querySelector('script[data-chartjs-cdn]')) {
            // Wait a little and resolve (script will finish loading)
            const checkReady = () => {
                if (window.Chart) resolve();
                else setTimeout(checkReady, 100);
            };
            checkReady();
            return;
        }
        // Inject script tag
        const s = document.createElement("script");
        s.src = "https://cdn.jsdelivr.net/npm/chart.js";
        s.async = true;
        s.setAttribute("data-chartjs-cdn", "1");
        s.onload = () => resolve();
        document.head.appendChild(s);
    });
}

// ✅ Draw/Update the "Today Overview" bar chart
function renderDailyActivityChart(threadLikes, fetches, logins) {
    const canvas = document.getElementById("dailyActivityChart");
    if (!canvas) return;

    // If a chart already exists on this canvas, destroy it first
    if (_dailyChart) {
        _dailyChart.destroy();
        _dailyChart = null;
    }

    // Small, readable labels; values are integers
    const data = {
        labels: ["Today's Thread Likes", "Today's Fetches", "Today's Logins"],
        datasets: [{
            label: "Count",
            data: [threadLikes, fetches, logins],
            // Let Chart.js pick default colors; keep it simple
        }]
    };

    // Simple bar chart with basic options
    _dailyChart = new Chart(canvas.getContext("2d"), {
        type: "bar",
        data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: { enabled: true }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { precision: 0 } // show integers
                }
            }
        }
    });
}

// ------------------------------------------------------
// Sharing / Commenting toggles
// ------------------------------------------------------
function toggleSharing(userId, canShare) {
    fetch(`${API_BASE}/Admin/SetSharingStatus`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canShare })
    }).then(() => loadUsers());
}

function toggleCommenting(userId, canComment) {
    fetch(`${API_BASE}/Admin/SetCommentingStatus`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canComment })
    }).then(() => loadUsers());
}

// ------------------------------------------------------
// Reports table
// ------------------------------------------------------
// ------------------------------------------------------
// Reports table (uses articleKind to label THREAD vs ARTICLE)
// ------------------------------------------------------
function loadReports() {
    fetch(`${API_BASE}/Admin/AllReports`)
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(reports => {
            const container = document.getElementById("reportsContainer");
            if (!container) return;

            if (!Array.isArray(reports) || reports.length === 0) {
                container.innerHTML = `
                    <div class="no-data-state">
                        <span class="icon">🎉</span>
                        <h3>No Reports!</h3>
                        <p>All clear - no reports to review.</p>
                    </div>`;
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
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>`;

            reports.forEach(r => {
                const date = new Date(r.reportedAt).toLocaleDateString('en-US', {
                    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
                });

                // For Articles, decide if it's THREAD or ARTICLE (based on articleKind)
                const isArticle = (r.reportType === 'Article');
                const kindLabel = (isArticle && r.articleKind === 'Thread') ? 'THREAD' : 'ARTICLE';

                // What we show in the "Type" column:
                // - If it's an Article, show THREAD/ARTICLE
                // - Else show the original type (e.g., Comment)
                const typeBadgeText = isArticle ? kindLabel : (r.reportType || '').toUpperCase();

                // What we send to server:
                // - For Articles we send 'Article'
                // - For comments we leave empty so server infers
                const inferredKind = isArticle ? 'Article' : '';

                const actionsCell = `
                    <button class="action-btn danger"
                            onclick="deleteReportedTarget('${inferredKind}', ${r.referenceId}, this)">
                        DELETE ${isArticle ? kindLabel : 'COMMENT'}
                    </button>`;

                html += `
                    <tr>
                        <td><strong>${r.reporterName ?? r.reporterId}</strong></td>
                        <td>${r.targetName || "–"}</td>
                        <td><span class="report-type-badge">${typeBadgeText}</span></td>
                        <td>${r.reason || ""}</td>
                        <td class="report-content">${r.content ?? ""}</td>
                        <td class="report-date">${date}</td>
                        <td class="report-actions">${actionsCell}</td>
                    </tr>`;
            });

            html += `
                        </tbody>
                    </table>
                </div>`;

            container.innerHTML = html;
        })
        .catch(() => {
            const container = document.getElementById("reportsContainer");
            if (container) {
                container.innerHTML = `
                    <div class="status-message error">
                        Failed to load reports
                    </div>`;
            }
        });
}


function deleteReportedTarget(targetKind, targetId, btnEl) {
    if (!targetId) return;
    if (!confirm(`Delete this ${targetKind || 'comment'}? This cannot be undone.`)) return;

    if (btnEl) {
        btnEl.disabled = true;
        btnEl.dataset.old = btnEl.innerHTML;
        btnEl.innerHTML = '⏳';
    }

    fetch(`${API_BASE}/Admin/DeleteReportedTarget`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ targetKind, targetId })
    })
        .then(res => { if (!res.ok) throw new Error('Delete failed'); return res.json(); })
        .then(() => {
            loadReports();
            // Recompute stats after deleting an article (counts may change)
            if (targetKind === 'Article' && typeof loadAllStats === "function") {
                loadAllStats();
            }
        })
        .catch(err => {
            alert("Error: " + (err.message || 'Delete failed'));
            if (btnEl) {
                btnEl.disabled = false;
                btnEl.innerHTML = btnEl.dataset.old || 'DELETE';
            }
        });
}

// ------------------------------------------------------
// Import external articles
// ------------------------------------------------------
function setupImportExternal() {
    const importBtn = document.getElementById("importBtn");
    if (!importBtn) return;

    importBtn.addEventListener("click", () => {
        importBtn.disabled = true;
        importBtn.innerHTML = 'Importing...';

        fetch(`${API_BASE}/Admin/GetFromNewsAPI`, { method: "POST" })
            .then(async res => {
                if (!res.ok) {
                    const txt = await res.text().catch(() => "");
                    // מציגים את מה שהשרת החזיר (למשל: "Error importing articles: ....")
                    throw new Error(txt || `HTTP ${res.status}`);
                }
                return res.json();
            })
            .then(data => {
                document.getElementById("importStatus").innerHTML =
                    `<div class='status-message success'>${data.length ?? 0} new articles were imported successfully!</div>`;
                loadAllStats();
            })
            .catch(err => {
                document.getElementById("importStatus").innerHTML =
                    `<div class='status-message error'>Error: ${err.message}</div>`;
            })
            .finally(() => {
                importBtn.disabled = false;
                importBtn.innerHTML = 'Import Articles';
            });
    });
}

// ------------------------------------------------------
// AI Tagging
// ------------------------------------------------------
function setupTagging() {
    const taggingBtn = document.getElementById("taggingBtn");
    if (!taggingBtn) return;

    taggingBtn.addEventListener("click", () => {
        taggingBtn.disabled = true;
        taggingBtn.innerHTML = 'Tagging...';

        fetch(`${API_BASE}/Tagging/RunTagging`, { method: "POST" })
            .then(res => {
                if (!res.ok) throw new Error();
                return res.text();
            })
            .then(() => {
                document.getElementById("taggingStatus").innerHTML =
                    `<div class='status-message success'>Tagging completed successfully!</div>`;
            })
            .catch(err => {
                document.getElementById("taggingStatus").innerHTML =
                    `<div class='status-message error'> Error: ${err.message}</div>`;
            })
            .finally(() => {
                taggingBtn.disabled = false;
                taggingBtn.innerHTML = 'Tag Articles';
            });
    });
}

// ------------------------------------------------------
// Fix missing images
// ------------------------------------------------------
function setupFixMissingImages() {
    const fixBtn = document.getElementById("fixImagesBtn");
    if (!fixBtn) return;

    fixBtn.addEventListener("click", () => {
        fixBtn.disabled = true;
        fixBtn.innerHTML = 'Fixing...';

        fetch(`${API_BASE}/Admin/FixMissingImages`, { method: "POST" })
            .then(res => {
                if (!res.ok) throw new Error();
                return res.json();
            })
            .then(data => {
                document.getElementById("fixImagesStatus").innerHTML = `
                    <div class='status-message success'>
                        Fixed: ${data.success}, Skipped: ${data.skippedDueToContentPolicy}, Failed: ${data.failed}
                    </div>`;
            })
            .catch(err => {
                document.getElementById("fixImagesStatus").innerHTML =
                    `<div class='status-message error'>Error: ${err.message}</div>`;
            })
            .finally(() => {
                fixBtn.disabled = false;
                fixBtn.innerHTML = 'Fix Missing Images';
            });
    });
}

// ------------------------------------------------------
// Small UX helpers
// ------------------------------------------------------

// ✅ Visual feedback for all admin action buttons
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

// ✅ Global error listener – logs errors for debugging
window.addEventListener('error', (e) => {
    console.error('Global error:', e.error);
});
