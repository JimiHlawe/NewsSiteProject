document.addEventListener("DOMContentLoaded", function () {
    var userJson = sessionStorage.getItem("loggedUser");
    if (!userJson) {
        window.location.href = "/html/login.html";
        return;
    }

    var user = JSON.parse(userJson);
    var url = "https://localhost:7084/api/Articles/SharedWithMe/" + user.id;

    fetch(url)
        .then(function (res) {
            if (!res.ok) throw new Error("Failed to fetch shared articles");
            return res.json();
        })
        .then(function (data) {
            renderSharedArticles(data);
        })
        .catch(function () {
            document.getElementById("sharedContainer").innerHTML =
                "<div class='alert alert-danger'>⚠️ Error loading shared articles</div>";
        });
});

function renderSharedArticles(articles) {
    var container = document.getElementById("sharedContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = "<p>No articles shared with you yet.</p>";
        return;
    }

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];

        var imageHtml = "";
        if (article.imageUrl) {
            imageHtml = "<img src='" + article.imageUrl + "' style='max-height:200px;' class='mb-2'>";
        }

        var html = "";
        html += "<div class='card mb-3 p-3'>";
        html += imageHtml;
        html += "<h4>" + article.title + "</h4>";
        html += "<p>" + (article.description || "") + "</p>";
        html += "<p><strong>Shared by:</strong> " + article.senderName + "</p>";
        html += "<p><strong>Comment:</strong> " + (article.comment || "No comment") + "</p>";
        html += "<a href='" + article.sourceUrl + "' target='_blank' class='btn btn-primary btn-sm mt-2'>Read Full Article</a>";
        html += "</div>";

        container.innerHTML += html;
    }
}
