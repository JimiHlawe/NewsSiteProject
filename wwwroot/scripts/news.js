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
    const user = getLoggedUser();
    if (!user?.id) {
        console.error("No logged user found");
        return;
    }

    console.log("🔑 Loading articles for user:", user.id);

    fetch(`/api/Users/All?userId=${user.id}`)
        .then(res => {
            if (!res.ok) throw new Error("Failed to load articles");
            return res.json();
        })
        .then(data => {
            console.log("✅ Articles fetched:", data);

            // ✨ הקרוסלה: 5 הכתבות הכי חדשות
            carouselArticles = data.slice(0, 5);
            initCarousel();

            // ✨ הגריד: כל שאר הכתבות אחרי 5 הראשונות
            allArticles = data.slice(5, 5 + pageSize * currentPage);
            renderVisibleArticles();

            if (5 + allArticles.length >= data.length) {
                document.getElementById("loadMoreBtn").style.display = "none";
            } else {
                document.getElementById("loadMoreBtn").style.display = "block";
            }
        })
        .catch(err => {
            console.error("❌ Error loading articles:", err);
        });
}

// ✅ גריד
function renderVisibleArticles() {
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    allArticles.forEach(article => {
        const div = document.createElement("div");
        div.className = "article-card";

        const tagsHtml = (article.tags || []).map(tag => `<span class="tag">${tag}</span>`).join(" ");

        div.innerHTML = `
            <img src="${article.imageUrl || 'https://via.placeholder.com/800x600'}" class="article-image">
            <div class="article-content">
                <div class="article-tags">${tagsHtml}</div>
                <h3 class="article-title">${article.title}</h3>
                <p class="article-description">${article.description?.substring(0, 150)}</p>
                <div class="article-meta">
                    <span>${article.author}</span>
                    <span>${new Date(article.publishedAt).toLocaleDateString()}</span>
                </div>
                <div class="article-actions">
                    <button class="save-btn" onclick="saveArticle(${article.id})">Save</button>
                    <button class="share-btn" onclick="toggleShare(${article.id})">Share</button>
                </div>
                ${getShareForm(article.id)}
            </div>
        `;
        grid.appendChild(div);
    });
}

// ✅ כפתור Load More
function loadMoreArticles() {
    currentPage++;
    const user = getLoggedUser();

    fetch(`/api/Users/All?userId=${user.id}`)
        .then(res => res.json())
        .then(data => {
            allArticles = data.slice(5, 5 + pageSize * currentPage);
            renderVisibleArticles();

            if (5 + allArticles.length >= data.length) {
                document.getElementById("loadMoreBtn").style.display = "none";
            }
        });
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
        .then(res => res.ok ? alert("Article saved.") : alert("Failed to save."))
        .catch(() => alert("Network error."));
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
            </div>
        `;
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
    const next = (currentSlide + 1) % carouselArticles.length;
    goToSlide(next);
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
    fetch("/api/Articles/Paginated?page=1&pageSize=5")
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
                    const div = document.createElement("div");
                    div.className = "sidebar-item";
                    div.innerHTML = `
                        <img src="${article.imageUrl || 'https://via.placeholder.com/60'}" />
                        <div class="sidebar-item-content">
                            <h6>${article.title?.substring(0, 40)}...</h6>
                            <div class="date">${new Date(article.publishedAt).toLocaleDateString()}</div>
                        </div>
                    `;
                    section.appendChild(div);
                });
            });
        });
}
