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
            console.log("Shared articles from API:", data);
            renderSharedArticles(data);
        })
        .catch(function () {
            document.getElementById("sharedContainer").innerHTML =
                "<div class='alert alert-danger'>Error loading shared articles</div>";
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

    for (let i = 0; i < articles.length; i++) {
        const article = articles[i];
        const imageUrl = article.imageUrl || 'https://via.placeholder.com/400x200?text=Article+Image';

        const formattedDate = article.publishedAt
            ? new Date(article.publishedAt).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            })
            : "";

        const articleCard = document.createElement('div');
        articleCard.className = 'shared-article-card fade-in';
        articleCard.style.animationDelay = (i * 0.1) + 's';
        articleCard.style.cursor = 'pointer';

        articleCard.innerHTML = `
    <div class='shared-image-container'>
        <img src='${imageUrl}' class='shared-image' alt='${article.title}'>
    </div>

    <div class='shared-content'>
        <div class='shared-info' onclick='event.stopPropagation();'>
            <div class='shared-by'>
                <span class='shared-label'>Shared by:</span>
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
            <span class='shared-author'> ${article.author || 'Unknown'}</span>
            ${formattedDate ? `<span class='shared-date'>${formattedDate}</span>` : ''}
        </div>

        ${article.tags && article.tags.length > 0 ? `
            <div class='shared-tags'>
                ${article.tags.map(tag => `<span class='tag-badge'>${tag}</span>`).join(' ')}
            </div>
        ` : ''}

        <button class='btn btn-danger btn-sm remove-btn'> Remove</button>
    </div>
`;

        articleCard.querySelector(".remove-btn").addEventListener("click", function (event) {
            event.stopPropagation();
            removeSharedArticle(article.sharedId, articleCard);
        });

        articleCard.addEventListener('click', function () {
            if (article.sourceUrl && article.sourceUrl !== '#') {
                window.open(article.sourceUrl, '_blank');
            } else {
                alert('No article URL available');
            }
        });

        container.appendChild(articleCard);
    }

    // ✅ סימון כל השיתופים כנקראו
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    markAllSharedAsRead(user.id);
}

function removeSharedArticle(sharedId, cardElement) {
    if (!confirm("Are you sure you want to remove this shared article?")) return;

    fetch("/api/Articles/RemoveShared/" + sharedId, {
        method: "DELETE"
    })
        .then(res => {
            if (!res.ok) throw new Error("Failed to remove");
            cardElement.remove();
        })
        .catch(err => {
            console.error("Error:", err);
            alert("❌ Failed to remove shared article");
        });
}

// ✅ פונקציה לסימון כל השיתופים כנקראו
function markAllSharedAsRead(userId) {
    fetch(`/api/Articles/MarkSharedAsRead/${userId}`, {
        method: "POST"
    });
}
