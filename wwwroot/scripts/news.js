document.addEventListener("DOMContentLoaded", () => {
    // Try importing external articles (non-blocking)
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(loadArticles); // Always load articles from DB
});

function loadArticles() {
    fetch("/api/Articles/All")
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(showArticles)
        .catch(() => {
            document.getElementById("articlesContainer").innerHTML = `
                <div class="alert alert-danger">An error occurred while loading the articles.</div>`;
        });
}

function showArticles(articles) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    for (const article of articles) {
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
}

// מציג את טופס השיתוף לכתבה מסוימת
function openShareForm(articleId) {
    const form = document.getElementById(`shareForm-${articleId}`);
    if (form) {
        form.style.display = "block";
    }
}


// שולח את הנתונים לשיתוף כתבה לשרת
function sendShare(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const toUsername = document.getElementById(`targetUser-${articleId}`).value.trim();
    const comment = document.getElementById(`comment-${articleId}`).value.trim();

    if (!user || !user.name || !toUsername) {
        alert("Missing sender or recipient username.");
        return;
    }

    const payload = {
        senderUsername: user.name,  // ✅ כאן התיקון
        toUsername: toUsername,
        articleId: articleId,
        comment: comment
    };

    console.log("✅ Payload:", payload);

    fetch("https://localhost:7084/api/Articles/Share", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (res.ok) {
                alert("Article shared successfully.");
                document.getElementById(`shareForm-${articleId}`).style.display = "none";
            } else {
                return res.text().then(text => { throw new Error(text); });
            }
        })
        .catch(err => {
            console.error("Share error:", err);
            alert("Failed to share the article.");
        });
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

// פונקציה לפתיחת וסגירת טופס שיתוף
function toggleShareType(articleId) {
    const type = document.getElementById(`shareType-${articleId}`).value;
    const targetInput = document.getElementById(`targetUser-${articleId}`);
    targetInput.style.display = type === "public" ? "none" : "block";
}


// פונקציית שליחת השיתוף
function sendShare(articleId) {
    const type = document.getElementById(`shareType-${articleId}`).value;
    const comment = document.getElementById(`comment-${articleId}`).value.trim();
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    if (!user || !user.name) {
        alert("Please log in.");
        return;
    }

    if (type === "private") {
        const toUsername = document.getElementById(`targetUser-${articleId}`).value.trim();
        if (!toUsername) {
            alert("Please enter a username.");
            return;
        }

        // שיתוף פרטי
        fetch("/api/Articles/Share", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                senderUsername: user.name,
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
        // שיתוף ציבורי
        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                username: user.name,
                articleId,
                comment
            })
        })
            .then(res => {
                if (res.ok) alert("Publicly shared!");
                else throw new Error("Failed");
            })
            .catch(() => alert("Error sharing publicly."));
    }
}
