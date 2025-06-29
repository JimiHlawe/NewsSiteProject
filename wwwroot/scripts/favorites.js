﻿// ✅ GLOBALS
var allSavedArticles = [];

// ✅ LOAD SAVED ARTICLES ON LOAD
document.addEventListener("DOMContentLoaded", function () {
    var userJson = sessionStorage.getItem("loggedUser");
    var user = userJson ? JSON.parse(userJson) : null;

    if (!user || !user.id) return;

    fetch("/api/Users/GetSavedArticles/" + user.id)
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

// ✅ FILTER BY TITLE + DATE
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

// ✅ RENDER ARTICLES
function renderSavedArticles(articles) {
    var container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No articles found.</div>';
        return;
    }

    articles.forEach(function (article) {
        var articleElement = document.createElement('div');
        articleElement.className = 'article-card';

        var tagsHtml = (article.tags || []).map(tag => `<span class="tag">${tag}</span>`).join(" ");

        var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });

        articleElement.innerHTML = `
            <div class="article-image-container">
                <img src="${article.imageUrl || 'https://via.placeholder.com/800x600'}" class="article-image" alt="Article Image">
                <div class="article-date-badge">${formattedDate}</div>
                <div class="article-overlay"></div>
            </div>
            <div class="article-content">
                <div class="article-tags">${tagsHtml}</div>
                <h3 class="article-title">${article.title}</h3>
                <p class="article-description">${article.description?.substring(0, 150) || 'No description available.'}</p>
                <div class="article-meta">
                    <span class="article-author">${article.author || 'Unknown Author'}</span>
                </div>
                <div class="article-actions">
                    <a href="${article.sourceUrl || '#'}" target="_blank" class="btn btn-primary">Read Article</a>
                    <button onclick="removeFavorite(${article.id})" class="btn btn-danger">Remove</button>
                </div>
            </div>
        `;

        container.appendChild(articleElement);
    });
}

// ✅ REMOVE FAVORITE
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
                allSavedArticles = allSavedArticles.filter(a => a.id !== articleId);
                renderSavedArticles(allSavedArticles);
            } else {
                alert("Failed to remove.");
            }
        })
        .catch(function () {
            alert("Network error.");
        });
}
