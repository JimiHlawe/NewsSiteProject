// ✅ GLOBALS
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

// ✅ RENDER ARTICLES עם MIN READ
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

        // הוספת cursor pointer וקליק לכרטיס כולו
        articleElement.style.cursor = 'pointer';

        // יצירת תגיות
        var tagsHtml = (article.tags || []).map(tag => `<span class="tag">${tag}</span>`).join("");

        // חישוב זמן קריאה
        var readingTime = Math.ceil((article.description?.length || 50) / 50) + ' min read';

        // פורמט תאריך
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

        // הוספת event listener לכרטיס כולו
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