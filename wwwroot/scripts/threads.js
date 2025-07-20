document.addEventListener("DOMContentLoaded", function () {
    loadThreadsArticles();
});

function loadThreadsArticles() {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch("/api/Articles/Public/" + user.id)
        .then(function (res) {
            if (!res.ok) throw new Error("Failed to fetch threads");
            return res.json();
        })
        .then(function (data) {
            console.log("📦 Threads from DB:", data);
            renderThreadsArticles(data);
        })
        .catch(function (err) {
            console.error("Failed to load threads:", err);
            showError("threadsContainer", "Failed to load threads");
        });
}

function renderThreadsArticles(articles) {
    var container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];
        var id = article.publicArticleId;

        var card = createThreadCard(article);
        container.appendChild(card);

        loadComments(id);
    }
}

function createThreadCard(article) {
    var id = article.publicArticleId;
    var div = document.createElement("div");
    div.className = "thread-card p-3 mb-4 border rounded bg-light";

    // הוספת cursor pointer וקליק לכרטיס כולו
    div.style.cursor = 'pointer';

    var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    var html = `
        <div class="initial-comment border-bottom pb-2 mb-3">
            <strong>${article.senderName}</strong> wrote:
            <p class="mb-0"><em>${article.initialComment || ""}</em></p>
        </div>

        <div class="thread-content">
            <div class="thread-image mb-2">
                <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}" class="img-fluid rounded">
            </div>
            <h5>${article.title}</h5>
            <p>${article.description || ""}</p>

            <div class="thread-meta mb-2">
                <strong>Author:</strong> ${article.author || 'Unknown'} |
                <strong>Date:</strong> ${formattedDate}
            </div>
            <div class="thread-actions mb-2">
            <button class='btn btn-sm btn-outline-primary' id="like-thread-btn-${id}" onclick="toggleThreadLike(${id}); event.stopPropagation();">
                ❤️ Like
            </button>
            <span id="like-thread-count-${id}" class="ms-2">0 ❤️</span>
            </div>

            <button class='btn btn-sm btn-danger mb-2' onclick="blockUser('${article.senderName}'); event.stopPropagation();">Block ${article.senderName}</button>
            <button class='btn btn-sm btn-warning mb-2' onclick="reportArticle(${id}); event.stopPropagation();">Report Article</button>

            <h6>💬 Comments:</h6>
            <div id="comments-${id}" onclick="event.stopPropagation();"></div>
            <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();"></textarea>
            <button class='btn btn-sm btn-primary' onclick='sendComment(${id}); event.stopPropagation();'>Send</button>
        </div>
    `;

    div.innerHTML = html;

    // הוספת event listener לכרטיס כולו
    div.addEventListener('click', function () {
        if (article.sourceUrl && article.sourceUrl !== '#') {
            window.open(article.sourceUrl, '_blank');
        } else {
            alert('No article URL available');
        }
    });

    return div;
}
function toggleThreadLike(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const btn = document.getElementById(`like-thread-btn-${articleId}`);
    const isLiked = btn.classList.contains("liked");

    const endpoint = isLiked ? "RemoveThreadLike" : "AddThreadLike";

    fetch(`/api/Articles/${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                btn.classList.toggle("liked");
                loadThreadLikeCount(articleId);
            }
        });
}

function loadThreadLikeCount(articleId) {
    fetch(`/api/Articles/GetThreadLikeCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`like-thread-count-${articleId}`).innerText = `${count} ❤️`;
        });
}

function blockUser(senderName) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    if (!confirm(`Are you sure you want to block ${senderName}?`)) return;

    fetch("/api/Users/BlockUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            blockerUserId: user.id,
            blockedUsername: senderName
        })
    })
        .then(res => res.ok ? alert(`✅ ${senderName} blocked!`) : alert("Error blocking user"))
        .then(() => loadThreadsArticles())
        .catch(() => alert("Error"));
}

function reportArticle(articleId) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    var reason = prompt("Why do you report this article?");
    if (!reason) return;

    var payload = {
        userId: user.id,
        referenceType: "Article",
        referenceId: articleId,
        reason: reason
    };

    fetch("/api/Articles/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => res.ok ? alert("✅ Report sent!") : alert("Error reporting"))
        .catch(() => alert("Error"));
}

function sendComment(articleId) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    var commentInput = document.getElementById("commentBox-" + articleId);
    var commentText = commentInput.value.trim();

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }

    var payload = {
        publicArticleId: articleId,
        userId: user.id,
        comment: commentText
    };

    console.log("📤 Sending comment:", payload);

    fetch("/api/Articles/AddPublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(function (res) {
            if (!res.ok) throw new Error("HTTP " + res.status);
            commentInput.value = "";
            loadComments(articleId);
        })
        .catch(function (err) {
            console.error("💥 Error posting comment", err);
            alert("Error posting comment");
        });
}

function loadComments(articleId) {
    fetch("/api/Articles/GetPublicComments/" + articleId)
        .then(function (res) {
            return res.json();
        })
        .then(function (comments) {
            var container = document.getElementById("comments-" + articleId);
            container.innerHTML = "";

            for (var i = 0; i < comments.length; i++) {
                var c = comments[i];
                var commentDiv = document.createElement('div');
                commentDiv.className = 'border rounded p-2 mb-1';
                commentDiv.innerHTML = "<strong>" +
                    c.username + "</strong>: " + c.comment +
                    ` <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id}); event.stopPropagation();'>Report</button>`;

                // מניעת קליק על התגובה עצמה
                commentDiv.addEventListener('click', function (event) {
                    event.stopPropagation();
                });

                container.appendChild(commentDiv);
            }
        })
        .catch(function () {
            console.error("Error loading comments");
        });
}

function reportComment(commentId) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    var reason = prompt("Why do you report this comment?");
    if (!reason) return;

    var payload = {
        userId: user.id,
        referenceType: "Comment",
        referenceId: commentId,
        reason: reason
    };

    fetch("/api/Articles/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => res.ok ? alert("Report sent!") : alert("Error reporting"))
        .catch(() => alert("Error"));
}

function showError(containerId, message) {
    var container = document.getElementById(containerId);
    container.innerHTML = "<div class='alert alert-danger'>" + message + "</div>";
}