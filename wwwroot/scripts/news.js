// ✅ משתנים גלובליים
let currentPage = 1;
const pageSize = 10;
let allArticles = [];

// ✅ עוזר: קבלת משתמש מחובר
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ טופס שיתוף כאב טיפוס
function getShareForm(articleId) {
    return `
        <div id="shareForm-${articleId}" class="share-form mt-2" style="display:none;">
            <select class="form-select mb-2" id="shareType-${articleId}" onchange="toggleShareType(${articleId})">
                <option value="private">📤 Share with user</option>
                <option value="public">🌍 Share with everyone</option>
            </select>
            <input type="text" placeholder="Target username" id="targetUser-${articleId}" class="form-control mb-2" />
            <textarea placeholder="Add a comment" id="comment-${articleId}" class="form-control mb-2"></textarea>
            <button onclick="sendShare(${articleId})" class="btn btn-primary btn-sm">Send</button>
        </div>`;
}

// ✅ טעינה ראשונית

document.addEventListener("DOMContentLoaded", () => {
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(loadArticles);
});

function loadArticles() {
    fetch("/api/Articles/WithTags")
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(articles => {
            articles.reverse(); // מהעדכניות לישנות
            allArticles = articles;
            renderPage(currentPage);
        })
        .catch(() => {
            document.getElementById("articlesContainer").innerHTML = `
                <div class="alert alert-danger">An error occurred while loading the articles.</div>`;
        });
}

function renderPage(page) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    const pageArticles = allArticles.slice(start, end);

    const user = getLoggedUser();

    let html = "";
    for (const article of pageArticles) {
        const image = article.imageUrl
            ? `<img src="${article.imageUrl}" style="max-height:200px;" class="img-fluid mb-2">`
            : "";

        const actionsHtml = (user && article.id)
            ? `<button onclick="saveArticle(${article.id})" class="btn btn-success btn-sm me-2">💾 Save</button>
               <button onclick="toggleShare(${article.id})" class="btn btn-secondary btn-sm">🔗 Share</button>`
            : "";

        const tagHtml = article.tags && article.tags.length > 0
            ? article.tags.map(tag => `<span class="badge bg-secondary me-1">${tag.name || tag}</span>`).join("")
            : `<span class="text-muted">No tags</span>`;

        html += `
            <div id="article-card-${article.id}" class="article-card mb-4 p-3 border rounded bg-white shadow-sm">
                ${image}
                <h5>${article.title}</h5>
                <p>${article.description || ""}</p>
                <div class="mb-2 tags-container">${tagHtml}</div>
                <a href="${article.sourceUrl}" target="_blank" class="btn btn-outline-primary btn-sm mb-2">Read Full Article</a><br/>
                ${actionsHtml}
                ${getShareForm(article.id)}
            </div>`;
    }

    container.innerHTML = html;
    renderPagination(Math.ceil(allArticles.length / pageSize));
}

function renderPagination(totalPages) {
    const container = document.getElementById("articlesContainer");
    let html = `<div class="text-center mt-3">`;
    for (let i = 1; i <= totalPages; i++) {
        html += `<button onclick="goToPage(${i})" class="btn btn-sm ${i === currentPage ? 'btn-primary' : 'btn-outline-primary'} mx-1">${i}</button>`;
    }
    html += `</div>`;
    container.innerHTML += html;
}

function goToPage(page) {
    currentPage = page;
    renderPage(page);
}

function saveArticle(articleId) {
    const user = getLoggedUser();

    if (!user?.id || !articleId) {
        alert("Invalid data. Please log in again and try.");
        return;
    }

    fetch("https://localhost:7084/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => res.ok ? alert("Article saved to favorites.") : alert("Failed to save the article."))
        .catch(() => alert("Network error occurred."));
}

function toggleShare(articleId) {
    const form = document.getElementById(`shareForm-${articleId}`);
    if (form) {
        form.style.display = form.style.display === "none" ? "block" : "none";
    }
}

function toggleShareType(articleId) {
    const type = document.getElementById(`shareType-${articleId}`).value;
    const targetInput = document.getElementById(`targetUser-${articleId}`);
    targetInput.style.display = type === "public" ? "none" : "block";
}

function sendShare(articleId) {
    const user = getLoggedUser();
    const type = document.getElementById(`shareType-${articleId}`).value;
    const comment = document.getElementById(`comment-${articleId}`).value.trim();

    if (!user?.name || !user?.id) {
        alert("Please log in.");
        return;
    }

    if (type === "private") {
        const toUsername = document.getElementById(`targetUser-${articleId}`).value.trim();
        if (!toUsername) {
            alert("Please enter a username.");
            return;
        }

        fetch("/api/Articles/Share", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ senderUsername: user.name, toUsername, articleId, comment })
        })
            .then(res => res.ok ? alert("Shared with user!") : alert("Error sharing."))
            .catch(() => alert("Error sharing."));

    } else {
        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, articleId, comment })
        })
            .then(res => res.ok
                ? alert("Publicly shared!")
                : res.text().then(text => { console.error("❌ SharePublic error:", text); throw new Error(text); }))
            .catch(err => {
                alert("Error sharing publicly.");
                console.error(err);
            });
    }
}
