// Holds all saved articles for filtering/rendering
var allSavedArticles = [];

// --- API BASE ---
const API_BASE = location.hostname.includes("localhost")
    ? "https://localhost:7084/api"
    : "https://proj.ruppin.ac.il/igroup113_test2/tar1/api";


// On page load: validate session, fetch saved articles, wire search form
document.addEventListener("DOMContentLoaded", function () {
    // Check login
    var userJson = sessionStorage.getItem("loggedUser");
    if (!userJson) {
        window.location.href = "/html/index.html";
        return;
    }

    var user = JSON.parse(userJson);
    if (!user || !user.id) {
        alert("Invalid session, please log in again");
        window.location.href = "/html/login.html";
        return;
    }

    // Fetch saved articles (simple handling)
    fetch(`${API_BASE}/Articles/GetSavedArticles/${user.id}`)
        .then(function (res) {
            if (!res.ok) return [];            
            return res.json().catch(function () {  
                return [];
            });
        })
        .then(function (data) {
            allSavedArticles = data || [];
            renderSavedArticles(allSavedArticles);
        })
        .catch(function () {
            document.getElementById("savedArticlesContainer").innerHTML =
                '<div class="alert alert-danger">Error loading saved articles</div>';
        });

    // Search form submit
    var form = document.getElementById("searchForm");
    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            filterArticles();
        });
    }
});

// Filters by title text and optional date range
function filterArticles() {
    var title = (document.getElementById("searchTitle").value || "").toLowerCase();
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

// Renders a simple card grid for saved articles
function renderSavedArticles(articles) {
    var container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (!articles || articles.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No articles found.</div>';
        return;
    }

    articles.forEach(function (article, index) {
        var articleElement = document.createElement('div');
        articleElement.className = 'article-card';
        articleElement.style.animationDelay = (index * 0.1) + 's';
        articleElement.style.cursor = 'pointer';

        // Unified article id (some payloads use articleId, some use id)
        var aid = (typeof article.articleId === "number" ? article.articleId : article.id);

        var tagsHtml = (article.tags || []).map(function (tag) {
            return '<span class="tag">' + tag + '</span>';
        }).join("");

        // Rough reading time (toy calc)
        var readingTime = Math.ceil(((article.description && article.description.length) || 50) / 50) + ' min read';

        var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric'
        });

        articleElement.innerHTML = ''
            + '<div class="article-image-container">'
            + '  <img src="' + (article.imageUrl || 'https://via.placeholder.com/400x200') + '" class="article-image" alt="Article Image">'
            + '  <div class="article-tags">' + tagsHtml + '</div>'
            + '  <div class="article-date-badge">' + formattedDate + '</div>'
            + '  <div class="article-overlay"></div>'
            + '</div>'
            + '<div class="article-content">'
            + '  <h3 class="article-title">' + (article.title || '') + '</h3>'
            + '  <p class="article-description">' + ((article.description && article.description.substring(0, 150)) || 'No description available.') + '</p>'
            + '  <div class="article-meta">'
            + '    <span class="article-author">' + (article.author || 'Unknown Author') + '</span>'
            + '    <span class="article-reading-time">' + readingTime + '</span>'
            + '  </div>'
            + '  <div class="article-actions">'
            + '    <button onclick="removeFavorite(' + aid + '); event.stopPropagation();" class="btn btn-danger">Remove</button>'
            + '  </div>'
            + '</div>';

        // Open original article on click
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

// Removes a saved article (simple UX + local re-render)
function removeFavorite(articleId) {
    var userJson = sessionStorage.getItem("loggedUser");
    var user = userJson ? JSON.parse(userJson) : null;
    if (!user || !user.id) return;

    fetch(`${API_BASE}/Articles/RemoveSavedArticle`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId: articleId })
    })
        .then(function (res) {
            if (!res.ok) {
                alert("Failed to remove."); // simple student-friendly message
                return;
            }
            // Update local list and re-render
            allSavedArticles = allSavedArticles.filter(function (a) {
                var aid = (typeof a.articleId === "number" ? a.articleId : a.id);
                return aid !== articleId;
            });
            renderSavedArticles(allSavedArticles);
        })
        .catch(function () {
            alert("Network error.");
        });
}
