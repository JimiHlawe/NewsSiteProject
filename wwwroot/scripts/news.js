let currentPage = 1;
const pageSize = 10;
let allArticles = [];

document.addEventListener("DOMContentLoaded", () => {
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(loadArticles);
});

function loadArticles() {
    fetch("/api/Articles/All")
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(articles => {
            articles.reverse(); // סדר מהעדכניות לישנות
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

    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    for (const article of pageArticles) {
        const image = article.imageUrl
            ? `<img src="${article.imageUrl}" style="max-height:200px;" class="img-fluid mb-2">`
            : "";

        const saveButton = (user && article.id)
            ? `<button onclick="saveArticle(${article.id})" class="btn btn-success btn-sm me-2">💾 Save</button>`
            : "";

        const shareButton = (user && article.id)
            ? `<button onclick="toggleShare(${article.id})" class="btn btn-secondary btn-sm">🔗 Share</button>`
            : "";

        const shareForm = `
    <div id="shareForm-${article.id}" class="share-form mt-2" style="display:none;">
        <select class="form-select mb-2" id="shareType-${article.id}" onchange="toggleShareType(${article.id})">
            <option value="private">📤 Share with user</option>
            <option value="public">🌍 Share with everyone</option>
        </select>

        <input type="text" placeholder="Target username" id="targetUser-${article.id}" class="form-control mb-2" />
        <textarea placeholder="Add a comment" id="comment-${article.id}" class="form-control mb-2"></textarea>
        <button onclick="sendShare(${article.id})" class="btn btn-primary btn-sm">Send</button>
    </div>
`;

        container.innerHTML += `
            <div class="article-card mb-4 p-3 border rounded bg-white shadow-sm">
                ${image}
                <h5>${article.title}</h5>
                <p>${article.description || ""}</p>
                <a href="${article.sourceUrl}" target="_blank" class="btn btn-outline-primary btn-sm mb-2">Read Full Article</a><br/>
                ${saveButton}
                ${shareButton}
                ${shareForm}
            </div>`;
    }

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

// שאר הפונקציות: saveArticle, toggleShare וכו' – ללא שינוי

function openShareForm(articleId) {
    const form = document.getElementById(`shareForm-${articleId}`);
    if (form) {
        form.style.display = "block";
    }
}

function saveArticle(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    if (!user?.id || !articleId) {
        alert("Invalid data. Please log in again and try.");
        return;
    }

    fetch("https://localhost:7084/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                alert("Article saved to favorites.");
            } else {
                alert("Failed to save the article.");
            }
        })
        .catch(() => {
            alert("Network error occurred.");
        });
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
    const loggedUser = JSON.parse(sessionStorage.getItem("loggedUser"));
    const type = document.getElementById(`shareType-${articleId}`).value;
    const comment = document.getElementById(`comment-${articleId}`).value.trim();

    if (!loggedUser || !loggedUser.name || !loggedUser.id) {
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
            body: JSON.stringify({
                senderUsername: loggedUser.name,
                toUsername,
                articleId,
                comment
            })
        })
            .then(res => {
                if (res.ok) alert("Shared with user!");
                else throw new Error("Failed");
            })
            .catch(() => alert("Error sharing."));
    } else {
        const payload = {
            userId: loggedUser.id,
            articleId,
            comment
        };

        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(res => {
                if (res.ok) {
                    alert("Publicly shared!");
                } else {
                    return res.text().then(text => {
                        console.error("❌ SharePublic error response:", text);
                        throw new Error(text);
                    });
                }
            })
            .catch((err) => {
                alert("Error sharing publicly.");
                console.error(err);
            });
    }
}
