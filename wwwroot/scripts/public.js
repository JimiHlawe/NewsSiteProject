document.addEventListener("DOMContentLoaded", function () {
    fetch("/api/Articles/Public")
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
            showError("publicContainer", "Failed to load articles");
        });
});

function renderPublicArticles(articles) {
    var container = document.getElementById("publicContainer");
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
    html += "<h6>💬 Comments:</h6>";
    html += "<div id='comments-" + id + "'></div>";
    html += "<textarea id='commentBox-" + id + "' class='form-control mb-2' placeholder='Write a comment...'></textarea>";
    html += "<button class='btn btn-sm btn-primary' onclick='sendComment(" + id + ")'>Send</button>";

    div.innerHTML = html;
    return div;
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
                    c.username + "</strong>: " + c.comment + "</div>";
                container.innerHTML += html;
            }
        })
        .catch(function () {
            console.error("💥 Error loading comments");
        });
}

function showError(containerId, message) {
    var container = document.getElementById(containerId);
    container.innerHTML = "<div class='alert alert-danger'>" + message + "</div>";
}
