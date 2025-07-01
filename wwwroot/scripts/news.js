// ✅ משתנים גלובליים
let currentPage = 1;
const pageSize = 6;
let allArticles = [];
let carouselArticles = [];
let currentSlide = 0;
let slideInterval;

// ✅ קבלת משתמש מחובר
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ התחלה בעת טעינת הדף
document.addEventListener("DOMContentLoaded", () => {
    loadAllArticlesAndSplit();
    loadSidebarSections();
});

// ✅ טען את כל הכתבות ואז פצל לקרוסלה וגריד
function loadAllArticlesAndSplit() {
    fetch(`/api/Articles/WithTags?page=${currentPage}&pageSize=${pageSize * 5}`)
        .then(res => {
            if (!res.ok) throw new Error("Failed to load articles");
            return res.json();
        })
        .then(data => {
            if (!data || data.length === 0) {
                console.warn("No articles found");
                return;
            }

            carouselArticles = data.slice(0, 5);
            allArticles = data.slice(5); // כל השאר

            initCarousel();
            renderVisibleArticles();

            if (pageSize >= allArticles.length) {
                document.getElementById("loadMoreBtn").style.display = "none";
            } else {
                document.getElementById("loadMoreBtn").style.display = "block";
            }
        })
        .catch(err => console.error("❌ Error loading articles:", err));
}

// ✅ גריד - עם תיקון מיקום התגיות בלבד
function renderVisibleArticles() {
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    const visibleArticles = allArticles.slice(0, pageSize * currentPage);

    visibleArticles.forEach(article => {
        const div = document.createElement("div");
        div.className = "article-card";

        const tagsHtml = (article.tags || []).map(tag => `<span class="tag">${tag}</span>`).join(" ");
        const formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric'
        });

        div.innerHTML = `
        <div class="article-image-container">
            <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}" class="article-image">
            <div class="article-tags">${tagsHtml}</div>
            <div class="article-overlay"></div>
        </div>
        <div class="article-content">
            <h3 class="article-title">${article.title}</h3>
            <p class="article-description">${article.description?.substring(0, 150) || ''}</p>
            <div class="article-meta">
                <span>${article.author || 'Unknown Author'}</span>
                <span>${formattedDate}</span>
            </div>
            <div class="article-actions">
                <button class="save-btn" onclick="saveArticle(${article.id})">Save</button>
                <button class="btn btn-sm btn-success" onclick="toggleShare(${article.id})">Share</button>
                <button class="btn btn-sm btn-danger" onclick="reportArticle(${article.id})">🚩 Report</button>
            </div>
            ${getShareForm(article.id)}
            <div class="article-comments mt-3">
                <h6>💬 Comments:</h6>
                <div id="comments-${article.id}"></div>
                <textarea id="commentBox-${article.id}" class="form-control mb-2" placeholder="Write a comment..."></textarea>
                <button onclick="sendComment(${article.id})" class="btn btn-sm btn-primary">Send</button>
            </div>
        </div>
    `;

        grid.appendChild(div);

        loadComments(article.id);
    });

    if (pageSize * currentPage >= allArticles.length) {
        document.getElementById("loadMoreBtn").style.display = "none";
    } else {
        document.getElementById("loadMoreBtn").style.display = "block";
    }
}

function loadMoreArticles() {
    currentPage++;
    renderVisibleArticles();
}

// ✅ שמירת כתבה
function saveArticle(articleId) {
    const user = getLoggedUser();
    if (!user?.id || !articleId) {
        alert("Invalid data. Please log in again and try.");
        return;
    }

    fetch("/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => res.ok ? alert("✅ Article saved.") : alert("❌ Failed to save."))
        .catch(() => alert("❌ Network error."));
}

// ✅ הוספת תגובה
function sendComment(articleId) {
    const user = getLoggedUser();
    const comment = document.getElementById(`commentBox-${articleId}`).value.trim();

    if (!comment) {
        alert("Please write a comment!");
        return;
    }

    fetch("/api/Articles/AddComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            ArticleId: articleId,
            UserId: user.id,
            Comment: comment
        })
    })
        .then(res => {
            if (res.ok) {
                document.getElementById(`commentBox-${articleId}`).value = "";
                loadComments(articleId);
            } else {
                alert("❌ Error adding comment");
            }
        })
        .catch(() => alert("❌ Network error"));
}

// ✅ טעינת תגובות
function loadComments(articleId) {
    fetch(`/api/Articles/GetComments/${articleId}`)
        .then(res => res.json())
        .then(comments => {
            const container = document.getElementById(`comments-${articleId}`);
            container.innerHTML = "";
            comments.forEach(c => {
                container.innerHTML += `<div class="border rounded p-2 mb-1">
                    <strong>${c.username}</strong>: ${c.commentText}
                    <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id})'>🚩</button>
                </div>`;
            });
        })
        .catch(err => console.error(err));
}

// ✅ דיווח כתבה
function reportArticle(articleId) {
    const user = getLoggedUser();
    const reason = prompt("Why do you want to report this article?");
    if (!reason) return;

    fetch("/api/Articles/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            userId: user.id,
            referenceType: "Article",
            referenceId: articleId,
            reason: reason
        })
    })
        .then(res => res.ok ? alert("✅ Reported!") : alert("❌ Error reporting."))
        .catch(() => alert("❌ Error reporting."));
}

// ✅ דיווח תגובה
function reportComment(commentId) {
    const user = getLoggedUser();
    const reason = prompt("Why do you want to report this comment?");
    if (!reason) return;

    fetch("/api/Articles/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            userId: user.id,
            referenceType: "Comment",
            referenceId: commentId,
            reason: reason
        })
    })
        .then(res => res.ok ? alert("✅ Reported!") : alert("❌ Error reporting."))
        .catch(() => alert("❌ Error reporting."));
}

// ✅ שיתוף
function getShareForm(articleId) {
    return `
        <div id="shareForm-${articleId}" class="share-form mt-2" style="display:none;">
            <select class="form-select mb-2" id="shareType-${articleId}" onchange="toggleShareType(${articleId})">
                <option value="private">Share with user</option>
                <option value="public">Share with everyone</option>
            </select>
            <input type="text" placeholder="Target username" id="targetUser-${articleId}" class="form-control mb-2" />
            <textarea placeholder="Add a comment" id="comment-${articleId}" class="form-control mb-2"></textarea>
            <button onclick="sendShare(${articleId})" class="btn btn-primary btn-sm">Send</button>
        </div>`;
}

function toggleShare(articleId) {
    const form = document.getElementById(`shareForm-${articleId}`);
    if (form) form.style.display = form.style.display === "none" ? "block" : "none";
}

function toggleShareType(articleId) {
    const type = document.getElementById(`shareType-${articleId}`).value;
    const targetInput = document.getElementById(`targetUser-${articleId}`);
    targetInput.style.display = type === "public" ? "none" : "block";
}

function sendShare(articleId) {
    const user = getLoggedUser();
    const type = document.getElementById(`shareType-${articleId}`).value;
    const comment = document.getElementById(`comment-${articleId}`).value.trim();

    if (!user?.name || !user?.id) {
        alert("Please log in.");
        return;
    }

    if (type === "private") {
        const toUsername = document.getElementById(`targetUser-${articleId}`).value.trim();
        if (!toUsername) {
            alert("Please enter a username.");
            return;
        }

        fetch("/api/Articles/Share", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ senderUsername: user.name, toUsername, articleId, comment })
        })
            .then(res => res.ok ? alert("Shared!") : alert("Error sharing."))
            .catch(() => alert("Error."));
    } else {
        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, articleId, comment })
        })
            .then(res => res.ok ? alert("Publicly shared!") : alert("Error."))
            .catch(() => alert("Error."));
    }
}

// ✅ קרוסלה
function initCarousel() {
    const container = document.getElementById("carouselContainer");
    const indicators = document.getElementById("carouselIndicators");
    container.innerHTML = "";
    indicators.innerHTML = "";

    carouselArticles.forEach((article, index) => {
        const slide = document.createElement("div");
        slide.className = `carousel-slide ${index === 0 ? "active" : ""}`;
        slide.style.backgroundImage = `url(${article.imageUrl || 'https://via.placeholder.com/800x400'})`;

        const tagsHtml = (article.tags || []).map(tag => `<span class="slide-category">${tag}</span>`).join(" ");

        slide.innerHTML = `
            <div class="carousel-overlay">
                <div class="slide-content">
                    <div class="slide-main">
                        <div class="slide-tags">${tagsHtml}</div>
                        <h1 class="slide-title">${article.title}</h1>
                        <p class="slide-description">${article.description?.substring(0, 150) || ""}</p>
                        <p class="slide-author">${article.author}</p>
                    </div>
                </div>
            </div>`;
        container.appendChild(slide);

        const dot = document.createElement("div");
        dot.className = `carousel-dot ${index === 0 ? "active" : ""}`;
        dot.onclick = () => goToSlide(index);
        indicators.appendChild(dot);
    });

    startAutoSlide();
}
function goToSlide(index) {
    const slides = document.querySelectorAll(".carousel-slide");
    const dots = document.querySelectorAll(".carousel-dot");
    slides.forEach((slide, i) => slide.classList.toggle("active", i === index));
    dots.forEach((dot, i) => dot.classList.toggle("active", i === index));
    currentSlide = index;
}

function nextSlide() {
    goToSlide((currentSlide + 1) % carouselArticles.length);
}

function startAutoSlide() {
    stopAutoSlide();
    slideInterval = setInterval(nextSlide, 5000);
}

function stopAutoSlide() {
    if (slideInterval) clearInterval(slideInterval);
}

// ✅ Sidebar רגיל שלך
function loadSidebarSections() {
    fetch("/api/Articles/Paginated?page=1&pageSize=8")
        .then(res => res.json())
        .then(articles => {
            const hot = document.getElementById("hotNews");
            const editor = document.getElementById("editorPick");
            const must = document.getElementById("mustSee");

            const sections = [hot, editor, must];
            sections.forEach((section, i) => {
                section.innerHTML = "";
                const chunk = articles.slice(i * 3, i * 3 + 3);
                chunk.forEach(article => {
                    const formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
                        year: 'numeric', month: 'short', day: 'numeric'
                    });
                    const div = document.createElement("div");
                    div.className = "sidebar-item";
                    div.innerHTML = `
                        <img src="${article.imageUrl || 'https://via.placeholder.com/60'}" />
                        <div class="sidebar-item-content">
                            <h6>${article.title?.substring(0, 40)}...</h6>
                            <div class="date">${formattedDate}</div>
                        </div>`;
                    section.appendChild(div);
                });
            });
        });
}

function toggleAddArticleForm() {
    const form = document.getElementById("addArticleForm");
    form.style.display = form.style.display === "none" ? "block" : "none";
}

function submitNewArticle(event) {
    if (event) event.preventDefault();

    const tagsRaw = document.getElementById("newTags").value;
    const tags = tagsRaw.split(",").map(s => s.trim()).filter(s => s !== "");

    const newArticle = {
        title: document.getElementById("newTitle").value,
        description: document.getElementById("newDescription").value,
        content: document.getElementById("newContent").value,
        author: document.getElementById("newAuthor").value,
        sourceUrl: document.getElementById("newSourceUrl").value,
        imageUrl: document.getElementById("newImageUrl").value,
        publishedAt: document.getElementById("newPublishedAt").value,
        tags: tags
    };

    console.log(newArticle);

    fetch("/api/Articles/AddUserArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newArticle)
    })
        .then(res => res.ok ? res.json() : res.text())
        .then(data => console.log(data))
        .catch(err => console.error(err));
}


