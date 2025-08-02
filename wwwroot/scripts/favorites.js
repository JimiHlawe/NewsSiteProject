// ✅ Global variable to store all saved articles
var allSavedArticles = [];

// ✅ On page load – fetch saved articles and set up search form
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

// ✅ Filter saved articles by title and date range
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

// ✅ Render saved articles as cards with tags and metadata
function renderSavedArticles(articles) {
    var container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No articles found.</div>';
        return;
    }

    articles.forEach(function (article, index) {
        var articleElement = document.createElement('div');
        articleElement.className = 'article-card';
        articleElement.style.animationDelay = (index * 0.1) + 's';
        articleElement.style.cursor = 'pointer';

        var tagsHtml = (article.tags || []).map(function (tag) {
            return `<span class="tag">${tag}</span>`;
        }).join("");

        var readingTime = Math.ceil((article.description?.length || 50) / 50) + ' min read';

        var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });

        articleElement.innerHTML = `
            <div class="article-image-container">
                <img src="${article.imageUrl || 'https://via.placeholder.com/400x200'}" class="article-image" alt="Article Image">
                <div class="article-tags">${tagsHtml}</div>
                <div class="article-date-badge">${formattedDate}</div>
                <div class="article-overlay"></div>
            </div>
            <div class="article-content">
                <h3 class="article-title">${article.title}</h3>
                <p class="article-description">${article.description?.substring(0, 150) || 'No description available.'}</p>
                <div class="article-meta">
                    <span class="article-author">${article.author || 'Unknown Author'}</span>
                    <span class="article-reading-time">${readingTime}</span>
                </div>
                <div class="article-actions">
                    <button onclick="removeFavorite(${article.id}); event.stopPropagation();" class="btn btn-danger">Remove</button>
                </div>
            </div>
        `;

        articleElement.addEventListener('click', function () {
            if (article.sourceUrl && article.sourceUrl !== '#') {
                window.open(article.sourceUrl, '_blank');
            } else {
                alert('No article URL available');
            }
        });

        container.appendChild(articleElement);
    });
}

// ✅ Remove an article from saved list and re-render
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
                allSavedArticles = allSavedArticles.filter(function (a) {
                    return a.id !== articleId;
                });
                renderSavedArticles(allSavedArticles);
            } else {
                alert("Failed to remove.");
            }
        })
        .catch(function () {
            alert("Network error.");
        });
}
