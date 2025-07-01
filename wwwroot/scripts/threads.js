document.addEventListener("DOMContentLoaded", function () {
    loadPublicArticles();
});

function loadPublicArticles() {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch("/api/Articles/Public/" + user.id)
        .then(function (res) {
            if (!res.ok) throw new Error("Failed to fetch articles");
            return res.json();
        })
        .then(function (data) {
            console.log("📦 Articles from DB:", data);
            renderPublicArticles(data);
        })
        .catch(function (err) {
            console.error("Failed to load public articles:", err);
            showError("threadsContainer", "Failed to load articles");
        });
}

function renderPublicArticles(articles) {
    var container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];
        var id = article.publicArticleId;

        var card = createArticleCard(article);
        container.appendChild(card);

        loadComments(id);
    }
}

function createArticleCard(article) {
    var id = article.publicArticleId;
    var div = document.createElement("div");
    div.className = "article-card p-3 mb-3 border rounded bg-light";

    var html = "";
    html += "<h5>" + article.title + "</h5>";
    html += "<p>" + (article.description || "") + "</p>";
    html += "<p><em>" + (article.initialComment || "") + "</em></p>";
    html += "<div class='mb-2'><strong>Shared by:</strong> " + article.senderName + "</div>";
    html += `<button class='btn btn-sm btn-danger mb-2' onclick="blockUser('${article.senderName}')">Block ${article.senderName}</button> `;
    html += `<button class='btn btn-sm btn-warning mb-2' onclick="reportArticle(${id})">Report Article</button>`;
    html += "<h6>💬 Comments:</h6>";
    html += "<div id='comments-" + id + "'></div>";
    html += "<textarea id='commentBox-" + id + "' class='form-control mb-2' placeholder='Write a comment...'></textarea>";
    html += "<button class='btn btn-sm btn-primary' onclick='sendComment(" + id + ")'>Send</button>";

    div.innerHTML = html;
    return div;
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
        .then(() => loadPublicArticles())
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
                var html = "<div class='border rounded p-2 mb-1'><strong>" +
                    c.username + "</strong>: " + c.comment +
                    ` <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id})'>Report</button>` +
                    "</div>";
                container.innerHTML += html;
            }
        })
        .catch(function () {
            console.error("💥 Error loading comments");
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
        .then(res => res.ok ? alert("✅ Report sent!") : alert("Error reporting"))
        .catch(() => alert("Error"));
}

function showError(containerId, message) {
    var container = document.getElementById(containerId);
    container.innerHTML = "<div class='alert alert-danger'>" + message + "</div>";
}
