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
        container.innerHTML = `
            <div class="no-articles-message fade-in">
                <h3>📭 No shared articles yet</h3>
                <p>Articles that friends and colleagues share with you will appear here.</p>
            </div>
        `;
        return;
    }

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];

        // תמונה ברירת מחדל אם אין תמונה
        var imageUrl = article.imageUrl || 'https://via.placeholder.com/400x200?text=Article+Image';

        // עיצוב תאריך
        var formattedDate = "";
        if (article.publishedAt) {
            formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            });
        }

        var html = `
            <div class='shared-article-card fade-in' style='animation-delay: ${i * 0.1}s'>
                <div class='shared-image-container'>
                    <img src='${imageUrl}' class='shared-image' alt='${article.title}'>
                </div>
                
                <div class='shared-content'>
                    <div class='shared-info'>
                        <div class='shared-by'>
                            <span class='shared-label'>👤 Shared by:</span>
                            <span class='shared-name'>${article.senderName}</span>
                        </div>
                        
                        ${article.comment && article.comment !== 'No comment' ? `
                            <div class='shared-comment'>
                                <span class='comment-label'>💬 Comment:</span>
                                <div class='comment-text'>${article.comment}</div>
                            </div>
                        ` : ''}
                    </div>
                    
                    <h3 class='shared-title'>${article.title}</h3>
                    
                    ${article.description ? `
                        <p class='shared-description'>${article.description.substring(0, 150)}${article.description.length > 150 ? '...' : ''}</p>
                    ` : ''}

                    <div class='shared-meta mb-2'>
                        <span class='shared-author'><strong>Author:</strong> ${article.author || 'Unknown'}</span>
                        ${formattedDate ? ` | <span class='shared-date'><strong>Date:</strong> ${formattedDate}</span>` : ''}
                    </div>
                    
                    <a href='${article.sourceUrl}' target='_blank' class='read-article-btn'>
                        📖 Read Full Article
                    </a>
                </div>
            </div>
        `;

        container.innerHTML += html;
    }
}
