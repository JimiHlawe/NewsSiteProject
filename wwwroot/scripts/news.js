// ✅ משתנים גלובליים
let currentPage = 1;
const pageSize = 10;
let allArticles = [];

// ✅ עוזר: קבלת משתמש מחובר
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ טופס שיתוף כאב טיפוס
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

// ✅ טעינה ראשונית

document.addEventListener("DOMContentLoaded", () => {
    // מייבא כתבות חיצוניות (רק בפעם הראשונה)
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(() => {
            // לאחר הייבוא או כשלון, טוענים את הנתונים
            loadCarouselArticles();  // אם יש לך קרוסלה
            loadArticlesGrid();      // כתבות במרכז
            loadSidebarSections();   // HOT NEWS וכולי
        });
});


function loadArticles() {
    fetch("/api/Articles/WithTags")
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(articles => {
            articles.reverse(); // מהעדכניות לישנות
            allArticles = articles;
            renderPage(currentPage);
        })
        .catch(() => {
            document.getElementById("articlesContainer").innerHTML = `
                <div class="alert alert-danger">An error occurred while loading the articles.</div>`;
        });
}

function renderPage(page) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    const pageArticles = allArticles.slice(start, end);

    const user = getLoggedUser();

    let html = "";
    for (const article of pageArticles) {
        const image = article.imageUrl
            ? `<img src="${article.imageUrl}" style="max-height:200px;" class="img-fluid mb-2">`
            : "";

        const actionsHtml = (user && article.id)
            ? `<button onclick="saveArticle(${article.id})" class="btn btn-success btn-sm me-2">💾 Save</button>
               <button onclick="toggleShare(${article.id})" class="btn btn-secondary btn-sm">🔗 Share</button>`
            : "";

        const tagHtml = article.tags && article.tags.length > 0
            ? article.tags.map(tag => `<span class="badge bg-secondary me-1">${tag.name || tag}</span>`).join("")
            : `<span class="text-muted">No tags</span>`;

        html += `
            <div id="article-card-${article.id}" class="article-card mb-4 p-3 border rounded bg-white shadow-sm">
                ${image}
                <h5>${article.title}</h5>
                <p>${article.description || ""}</p>
                <div class="mb-2 tags-container">${tagHtml}</div>
                <a href="${article.sourceUrl}" target="_blank" class="btn btn-outline-primary btn-sm mb-2">Read Full Article</a><br/>
                ${actionsHtml}
                ${getShareForm(article.id)}
            </div>`;
    }

    container.innerHTML = html;
    renderPagination(Math.ceil(allArticles.length / pageSize));
}

function renderPagination(totalPages) {
    const container = document.getElementById("articlesContainer");
    let html = `<div class="text-center mt-3">`;
    for (let i = 1; i <= totalPages; i++) {
        html += `<button onclick="goToPage(${i})" class="btn btn-sm ${i === currentPage ? 'btn-primary' : 'btn-outline-primary'} mx-1">${i}</button>`;
    }
    html += `</div>`;
    container.innerHTML += html;
}

function goToPage(page) {
    currentPage = page;
    renderPage(page);
}

function saveArticle(articleId) {
    const user = getLoggedUser();

    if (!user?.id || !articleId) {
        alert("Invalid data. Please log in again and try.");
        return;
    }

    fetch("https://localhost:7084/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => res.ok ? alert("Article saved to favorites.") : alert("Failed to save the article."))
        .catch(() => alert("Network error occurred."));
}

function toggleShare(articleId) {
    const form = document.getElementById(`shareForm-${articleId}`);
    if (form) {
        form.style.display = form.style.display === "none" ? "block" : "none";
    }
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
            .then(res => res.ok
                ? alert("Publicly shared!")
                : res.text().then(text => { console.error("❌ SharePublic error:", text); throw new Error(text); }))
            .catch(err => {
                alert("Error sharing publicly.");
                console.error(err);
            });
    }
}

function loadArticlesGrid() {
    fetch("/api/Articles/WithTags")
        .then(res => {
            if (!res.ok) throw new Error('Failed to fetch articles');
            return res.json();
        })
        .then(articles => {
            const grid = document.getElementById("articlesGrid");
            grid.innerHTML = "";

            articles.forEach(article => {
                const div = document.createElement("div");
                div.className = "article-card";
                div.innerHTML = `
                    <img src="${article.imageUrl || 'https://via.placeholder.com/800x600'}" class="article-image">
                    <div class="article-content">
                        <div class="article-category">${article.tags?.[0]?.name || "חדשות"}</div>
                        <h3 class="article-title">${article.title}</h3>
                        <p class="article-description">${article.description?.substring(0, 150) || "אין תיאור."}</p>
                        <div class="article-meta">
                            <span>${article.author || "מערכת"}</span>
                            <span>${new Date(article.publishedAt).toLocaleDateString()}</span>
                        </div>
                    </div>
                `;
                grid.appendChild(div);
            });
        })
        .catch(err => {
            console.error("שגיאה בטעינת הכתבות:", err);
        });
}


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

let carouselArticles = [];
let currentSlide = 0;
let slideInterval;

function loadCarouselArticles() {
    fetch("/api/Articles/WithTags")
        .then(res => res.json())
        .then(data => {
            carouselArticles = data.slice(0, 5); // עד 5 כתבות
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

        slide.innerHTML = `
            <div class="carousel-overlay">
                <div class="slide-content">
                    <div class="slide-main">
                        <div class="slide-category">${article.tags?.[0]?.name || "News"}</div>
                        <h1 class="slide-title">${article.title}</h1>
                        <p class="slide-description">${article.description?.substring(0, 150) || ""}</p>
                        <p class="slide-author">${article.author || "מערכת"}</p>
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

    slides[currentSlide].classList.remove("active");
    dots[currentSlide].classList.remove("active");

    currentSlide = index;

    slides[currentSlide].classList.add("active");
    dots[currentSlide].classList.add("active");

    resetAutoSlide();
}

function nextSlide() {
    goToSlide((currentSlide + 1) % carouselArticles.length);
}

function prevSlide() {
    goToSlide((currentSlide - 1 + carouselArticles.length) % carouselArticles.length);
}

function startAutoSlide() {
    slideInterval = setInterval(nextSlide, 5000);
}

function resetAutoSlide() {
    clearInterval(slideInterval);
    startAutoSlide();
}


function startAutoSlide() {
    stopAutoSlide(); // בטיחות
    slideInterval = setInterval(nextSlide, 5000); // כל 5 שניות
}

function stopAutoSlide() {
    if (slideInterval) clearInterval(slideInterval);
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
