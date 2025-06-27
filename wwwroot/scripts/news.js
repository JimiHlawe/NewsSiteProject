
// ✅ משתנים גלובליים
let currentPage = 1;
const pageSize = 6;
let lastPageReached = false;

let allArticles = [];
let currentVisibleCount = 10;
const loadStep = 5;


// ✅ קבלת משתמש מחובר
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ טופס שיתוף מוכן לשימוש
function getShareForm(articleId) {
    return `
        <div id="shareForm-${articleId}" class="share-form mt-2" style="display:none;">
            <select class="form-select mb-2" id="shareType-${articleId}" onchange="toggleShareType(${articleId})">
                <option value="private">📤 Share with user</option>
                <option value="public">🌍 Share with everyone</option>
            </select>
            <input type="text" placeholder="Target username" id="targetUser-${articleId}" class="form-control mb-2" />
            <textarea placeholder="Add a comment" id="comment-${articleId}" class="form-control mb-2"></textarea>
            <button onclick="sendShare(${articleId})" class="btn btn-primary btn-sm">Send</button>
        </div>`;
}

// ✅ התחלה בעת טעינת הדף
document.addEventListener("DOMContentLoaded", () => {
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(() => {
            loadCarouselArticles();
            loadArticlesGrid();
            loadSidebarSections();
        });
});

// ✅ טוען כתבות לפי עמוד מהשרת (Load More)
function loadArticlesGrid() {
    fetch("/api/Articles/WithTags")
        .then(res => res.json())
        .then(data => {
            allArticles = data;
            currentVisibleCount = 10;
            renderVisibleArticles();
        })
        .catch(err => {
            console.error("שגיאה בטעינת הכתבות:", err);
        });
}


function renderVisibleArticles() {
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    const articlesToShow = allArticles.slice(0, currentVisibleCount);

    articlesToShow.forEach(article => {
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

    // הצגת / הסתרת כפתור Load More
    const btn = document.getElementById("loadMoreBtn");
    if (currentVisibleCount >= allArticles.length) {
        btn.style.display = "none";
    } else {
        btn.style.display = "block";
    }
}


// ✅ כפתור Load More
function loadMoreArticles() {
    currentVisibleCount += loadStep;
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
        .then(res => res.ok ? alert("Article saved to favorites.") : alert("Failed to save the article."))
        .catch(() => alert("Network error occurred."));
}

// ✅ שיתוף
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
            .then(res => res.ok ? alert("Shared with user!") : alert("Error sharing."))
            .catch(() => alert("Error sharing."));
    } else {
        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, articleId, comment })
        })
            .then(res => res.ok ? alert("Publicly shared!") : res.text().then(text => { throw new Error(text); }))
            .catch(err => {
                alert("Error sharing publicly.");
                console.error(err);
            });
    }
}

// ✅ Sidebar
function loadSidebarSections() {
    fetch("/api/Articles/WithTags")
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

// ✅ Carousel
let carouselArticles = [];
let currentSlide = 0;
let slideInterval;

function loadCarouselArticles() {
    fetch("/api/Articles/WithTags")
        .then(res => res.json())
        .then(data => {
            carouselArticles = data.slice(0, 5);
            initCarousel();
        })
        .catch(err => console.error("שגיאה בטעינת קרוסלה:", err));
}

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
                    <div class="slide-sidebar">
                        ${generateCarouselSidebarArticles(index)}
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

function generateCarouselSidebarArticles(excludeIndex) {
    let html = "";
    let count = 0;

    for (let i = 0; i < carouselArticles.length; i++) {
        if (i !== excludeIndex && count < 3) {
            const art = carouselArticles[i];
            html += `
                <div class="sidebar-article">
                    <div class="sidebar-article-content">
                        <img src="${art.imageUrl || 'https://via.placeholder.com/60'}" alt="">
                        <div class="sidebar-article-text">
                            <h6>${art.title.substring(0, 50)}...</h6>
                            <div class="date">${new Date(art.publishedAt).toLocaleDateString()}</div>
                        </div>
                    </div>
                </div>
            `;
            count++;
        }
    }

    return html;
}

function goToSlide(index) {
    const slides = document.querySelectorAll(".carousel-slide");
    const dots = document.querySelectorAll(".carousel-dot");

    if (index < 0 || index >= slides.length) return;

    slides.forEach((slide, i) => {
        slide.classList.toggle("active", i === index);
    });

    dots.forEach((dot, i) => {
        dot.classList.toggle("active", i === index);
    });

    currentSlide = index;
}

function nextSlide() {
    const next = (currentSlide + 1) % carouselArticles.length;
    goToSlide(next);
}

function prevSlide() {
    const prev = (currentSlide - 1 + carouselArticles.length) % carouselArticles.length;
    goToSlide(prev);
}

function startAutoSlide() {
    stopAutoSlide();
    slideInterval = setInterval(nextSlide, 5000);
}

function stopAutoSlide() {
    if (slideInterval) clearInterval(slideInterval);
}
