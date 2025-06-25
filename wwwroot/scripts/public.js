document.addEventListener("DOMContentLoaded", () => {
    fetch("/api/Articles/Public")
        .then(res => {
            if (!res.ok) throw new Error("Failed to fetch articles");
            return res.json();
        })
        .then(data => {
            console.log("📦 Articles from DB:", data);
            renderPublicArticles(data);
        })
        .catch(err => {
            console.error("Failed to load public articles:", err);
            document.getElementById("publicContainer").innerHTML =
                `<div class="alert alert-danger">Failed to load articles</div>`;
        });
});

function renderPublicArticles(articles) {
    const container = document.getElementById("publicContainer");
    container.innerHTML = "";

    articles.forEach(article => {
        const id = article.publicArticleId; // ← שימוש נכון במזהה
        const articleCard = document.createElement("div");
        articleCard.className = "article-card p-3 mb-3 border rounded bg-light";

        articleCard.innerHTML = `
            <h5>${article.title}</h5>
            <p>${article.description || ""}</p>
            <p><em>${article.initialComment || ""}</em></p>
            <div class="mb-2"><strong>Shared by:</strong> ${article.senderName}</div>

            <h6>💬 Comments:</h6>
            <div id="comments-${id}"></div>

            <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..."></textarea>
            <button class="btn btn-sm btn-primary" onclick="sendComment(${id})">Send</button>
        `;

        container.appendChild(articleCard);

        loadComments(id); // ← שימוש נכון
    });
}

function sendComment(articleId) {
    const loggedUser = JSON.parse(sessionStorage.getItem("loggedUser"));
    const commentText = document.getElementById(`commentBox-${articleId}`).value.trim();

    if (!commentText) return alert("Please enter a comment.");

    console.log("📤 Sending comment:", {
        publicArticleId: articleId,
        userId: loggedUser.id,
        comment: commentText
    });

    fetch("/api/Articles/AddPublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            publicArticleId: articleId,
            userId: loggedUser.id,
            comment: commentText
        })
    })
        .then(res => {
            if (res.ok) {
                console.log("✅ Comment posted successfully");
                document.getElementById(`commentBox-${articleId}`).value = "";
                loadComments(articleId);
            } else {
                console.error("❌ Failed to post comment: HTTP Error", res.status);
                throw new Error();
            }
        })
        .catch(err => {
            console.error("💥 Error posting comment", err);
            alert("Error posting comment");
        });
}

function loadComments(articleId) {
    console.log("🔄 Loading comments for article:", articleId);
    fetch(`/api/Articles/GetPublicComments/${articleId}`)
        .then(res => res.json())
        .then(comments => {
            const container = document.getElementById(`comments-${articleId}`);
            container.innerHTML = "";

            for (const c of comments) {
                container.innerHTML += `
                    <div class="border rounded p-2 mb-1">
                        <strong>${c.username}</strong>: ${c.comment}
                    </div>`;
            }
        })
        .catch(() => console.error("💥 Error loading comments"));
}
