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
        const articleCard = document.createElement("div");
        articleCard.className = "article-card p-3 mb-3 border rounded bg-light";

        const commentsSection = (article.publicComments || []).map(c => `
            <div class="mb-2"><strong>${c.username}:</strong> ${c.comment}</div>
        `).join("");

        articleCard.innerHTML = `
            <h5>${article.title}</h5>
            <p>${article.description || ""}</p>
            <p><em>${article.initialComment || ""}</em></p>
            <div class="mb-2"><strong>Shared by:</strong> ${article.senderName}</div>

            <h6>💬 Comments:</h6>
            <div id="comments-${article.articleId}">${commentsSection}</div>

            <textarea id="comment-input-${article.articleId}" class="form-control mb-2" placeholder="Write a comment..."></textarea>
            <button class="btn btn-sm btn-primary" onclick="sendComment(${article.articleId})">Send</button>
        `;

        container.appendChild(articleCard);
    });
}

function sendComment(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const commentText = document.getElementById(`comment-input-${articleId}`).value.trim();

    if (!user || !user.name || !commentText) {
        alert("Please log in and enter a comment.");
        return;
    }

    fetch("/api/Articles/AddPublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            articleId: articleId,
            username: user.name,
            comment: commentText
        })
    })
        .then(res => {
            if (!res.ok) throw new Error("Failed to post comment");
            return res.text();
        })
        .then(() => location.reload())
        .catch(err => {
            console.error("Error posting comment:", err);
            alert("Failed to send comment.");
        });
}
