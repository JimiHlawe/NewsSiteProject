document.addEventListener("DOMContentLoaded", () => {
    loadUsers();
    loadAllStats(); // טוען גם את הסטטיסטיקות הכלליות וגם הלייקים
    loadReports();
});

function loadUsers() {
    fetch("/api/Users/AllUsers")
        .then(res => res.json())
        .then(users => {
            const tbody = document.querySelector("#usersTable tbody");
            tbody.innerHTML = "";
            users.forEach(u => {
                tbody.innerHTML += `
                    <tr>
                        <td>${u.name}</td>
                        <td>${u.email}</td>
                        <td>${u.active}</td>
                        <td>${u.canShare}</td>
                        <td>${u.canComment}</td>
                        <td>
                            <button onclick="setStatus(${u.id}, ${!u.active})">
                                ${u.active ? "Block" : "Unblock"}
                            </button>
                            <button onclick="toggleSharing(${u.id}, ${!u.canShare})">
                                ${u.canShare ? "Block Sharing" : "Unblock Sharing"}
                            </button>
                            <button onclick="toggleCommenting(${u.id}, ${!u.canComment})">
                                ${u.canComment ? "Block Commenting" : "Unblock Commenting"}
                            </button>
                        </td>
                    </tr>`;
            });
        });
}

function setStatus(userId, isActive) {
    fetch("/api/Users/SetActiveStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, isActive })
    })
        .then(() => loadUsers());
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
                <p>Total Users: ${stats.totalUsers}</p>
                <p>Total Articles: ${stats.totalArticles}</p>
                <p>Total Saved Articles: ${stats.totalSaved}</p>
                <p>Today's Logins: ${stats.todayLogins}</p>
                <p>Today's Article Fetches: ${stats.todayFetches}</p>
                <p>Total Article Likes: ${likes.articleLikes}</p>
                <p>Today's Article Likes: ${likes.articleLikesToday}</p>
                <p>Total Thread Likes: ${likes.threadLikes}</p>
                <p>Today's Thread Likes: ${likes.threadLikesToday}</p>
            `;
        })
        .catch(err => {
            console.error("Error loading statistics:", err);
        });
}

function toggleSharing(userId, canShare) {
    fetch("/api/Users/SetSharingStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canShare })
    }).then(() => loadUsers());
}

function toggleCommenting(userId, canComment) {
    fetch("/api/Users/SetCommentingStatus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, canComment })
    }).then(() => loadUsers());
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

            let html = `
                <h3>📢 Reports</h3>
                <table class="table table-bordered">
                    <thead>
                        <tr>
                            <th>Reporter</th>
                            <th>Target</th> <!-- חדש -->
                            <th>Type</th>
                            <th>Reason</th>
                            <th>Content</th>
                            <th>Date</th>
                        </tr>
                    </thead>
                    <tbody>
            `;

            reports.forEach(r => {
                html += `
                    <tr>
                        <td>${r.reporterName}</td>
                        <td>${r.targetName || "–"}</td>
                        <td>${r.reportType}</td>
                        <td>${r.reason}</td>
                        <td>${r.content}</td>
                        <td>${new Date(r.reportedAt).toLocaleString()}</td>
                    </tr>
                `;
            });

            html += "</tbody></table>";
            container.innerHTML = html;
        })
        .catch(err => {
            console.error("Error loading reports:", err);
            const container = document.getElementById("reportsContainer");
            if (container)
                container.innerHTML = `<div class="alert alert-danger">⚠️ Failed to load reports</div>`;
        });
}
