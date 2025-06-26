var allSavedArticles = [];

document.addEventListener("DOMContentLoaded", function () {
    var userJson = sessionStorage.getItem("loggedUser");
    var user = userJson ? JSON.parse(userJson) : null;

    if (!user || !user.id) return;

    fetch("https://localhost:7084/api/Users/GetSavedArticles/" + user.id)
        .then(function (res) { return res.json(); })
        .then(function (data) {
            allSavedArticles = data;
            renderSavedArticles(data);
        })
        .catch(function () {
            document.getElementById("savedArticlesContainer").innerHTML =
                '<div class="alert alert-danger">Error loading saved articles</div>';
        });

    var form = document.getElementById("searchForm");
    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            filterArticles();
        });
    }
});

function filterArticles() {
    var title = document.getElementById("searchTitle").value.toLowerCase();
    var from = document.getElementById("searchFrom").value;
    var to = document.getElementById("searchTo").value;

    var filtered = [];

    for (var i = 0; i < allSavedArticles.length; i++) {
        var article = allSavedArticles[i];
        var articleTitle = article.title ? article.title.toLowerCase() : "";
        var matchTitle = articleTitle.indexOf(title) !== -1;

        var published = new Date(article.publishedAt);
        var matchFrom = !from || published >= new Date(from);
        var matchTo = !to || published <= new Date(to);

        if (matchTitle && matchFrom && matchTo) {
            filtered.push(article);
        }
    }

    renderSavedArticles(filtered);
}

function renderSavedArticles(articles) {
    var container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No articles found.</div>';
        return;
    }

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];

        container.innerHTML +=
            '<div class="col-md-4">' +
            '<div class="card">' +
            '<img src="' + article.imageUrl + '" class="card-img-top" alt="Article Image">' +
            '<div class="card-body">' +
            '<h5 class="card-title">' + article.title + '</h5>' +
            '<p class="card-text">' + article.description + '</p>' +
            '<a href="' + article.sourceUrl + '" target="_blank" class="btn btn-primary">Read</a> ' +
            '<button onclick="removeFavorite(' + article.id + ')" class="btn btn-danger btn-sm">Remove</button>' +
            '</div>' +
            '</div>' +
            '</div>';
    }
}

function removeFavorite(articleId) {
    var userJson = sessionStorage.getItem("loggedUser");
    var user = userJson ? JSON.parse(userJson) : null;

    if (!user || !user.id) return;

    fetch("/api/Users/RemoveSavedArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId: articleId })
    })
        .then(function (res) {
            if (res.ok) {
                alert("Removed from favorites.");

                // מחיקה ידנית מהמערך
                var updatedList = [];
                for (var i = 0; i < allSavedArticles.length; i++) {
                    if (allSavedArticles[i].id !== articleId) {
                        updatedList.push(allSavedArticles[i]);
                    }
                }

                allSavedArticles = updatedList;
                renderSavedArticles(allSavedArticles);
            } else {
                alert("Failed to remove.");
            }
        })
        .catch(function () {
            alert("Network error.");
        });
}
