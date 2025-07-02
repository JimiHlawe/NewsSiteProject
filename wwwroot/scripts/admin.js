document.addEventListener("DOMContentLoaded", () => {
    loadUsers();
    loadStats();
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

function loadStats() {
    fetch("/api/Users/GetStatistics")
        .then(res => res.json())
        .then(stats => {
            document.getElementById("statsContainer").innerHTML = `
                <p>Total Users: ${stats.totalUsers}</p>
                <p>Total Articles: ${stats.totalArticles}</p>
                <p>Total Saved Articles: ${stats.totalSaved}</p>
                <p>Today's Logins: ${stats.todayLogins}</p>
                <p>Today's Article Fetches: ${stats.todayFetches}</p>
            `;
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
