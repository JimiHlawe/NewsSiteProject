﻿// ✅ Global state and control variables
let currentPage = 1;
let filteredPage = 1;
const pageSize = 6;
let allArticles = [];
let carouselArticles = [];
let currentSlide = 0;
let slideInterval;
let isSearchActive = false;
let adLoaded = false;

/**
 * ✅ Returns the currently logged-in user from sessionStorage
 */
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

/**
 * ✅ Initializes the page when DOM is ready
 */
document.addEventListener("DOMContentLoaded", () => {
    loadAdBanner();
    loadAllArticlesAndSplit();
    loadSidebarSections();
    checkAdminAndShowAddForm();
});


/**
 * ✅ Loads all articles and splits them for carousel and grid display
 */
function loadAllArticlesAndSplit() {
    const user = getLoggedUser();
    const userId = user ? user.id : 3;

    fetch(`/api/Articles/AllFiltered?userId=${userId}`)
        .then(res => res.json())
        .then(data => {
            if (!data || data.length === 0) return;

            data = data.slice(8); // Skip the first 8 reserved for sidebar

            carouselArticles = data.slice(0, 5);
            allArticles = data.slice(5);

            initCarousel();
            renderVisibleArticles();

            const loadMoreBtn = document.getElementById("loadMoreBtn");
            loadMoreBtn.style.display = pageSize >= allArticles.length ? "none" : "block";
        });
}


/**
 * ✅ Renders visible articles into the main grid with pagination
 */
function renderVisibleArticles() {
    const user = getLoggedUser();
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    const sourceList = isSearchActive ? filteredArticles : allArticles;
    const currentPageUsed = isSearchActive ? filteredPage : currentPage;
    const visibleArticles = sourceList.slice(0, pageSize * currentPageUsed);

    visibleArticles.forEach(article => {
        const div = document.createElement("div");
        div.className = "article-card";
        div.style.cursor = "pointer";

        const tagsHtml = (article.tags || []).map(tag => `<span class="tag">${tag}</span>`).join(" ");
        const formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric'
        });

        let html = `
            <div class="article-image-container">
                <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}" class="article-image">
                <div class="article-tags">${tagsHtml}</div>
                <div class="article-overlay"></div>
            </div>
            <div class="article-content">
                <h3 class="article-title">${article.title}</h3>
                <p class="article-description">${article.description?.substring(0, 200) || ''}</p>
                <div class="article-author-date">
                    <span class="article-author">${article.author || 'Unknown Author'}</span>
                    <span>${formattedDate}</span>
                </div>`;

        if (user && user.id) {
            html += `
                <div class="article-actions">
                    <button class="like-btn" id="like-btn-${article.id}" onclick="toggleLike(${article.id}); event.stopPropagation();">
                        <img src="../pictures/like.png" alt="Like" title="Like">
                    </button>
                    <span id="like-count-${article.id}" class="like-count">0</span>

                    <button class="btn btn-sm btn-info" onclick="openCommentsModal(${article.id}); event.stopPropagation();">
                        <img src="../pictures/comment1.png" alt="Comment" title="Comment">
                    </button>
                    <button class="save-btn" onclick="saveArticle(${article.id}); event.stopPropagation();">
                        <img src="../pictures/save.png" alt="Save" title="Save">
                    </button>
                    <button class="btn btn-sm btn-success" onclick="toggleShare(${article.id}); event.stopPropagation();">
                        <img src="../pictures/send.png" alt="Share" title="Share">
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="reportArticle(${article.id}); event.stopPropagation();">
                        <img src="../pictures/report.png" alt="Report" title="Report">
                    </button>
                </div>`;

            updateLikeCount(article.id);
        }

        html += `</div>`; // Close article-content
        div.innerHTML = html;

        div.addEventListener('click', function () {
            if (article.sourceUrl && article.sourceUrl !== '#') {
                window.open(article.sourceUrl, '_blank');
            } else {
                alert('No article URL available');
            }
        });

        grid.appendChild(div);
    });

    const loadMoreBtn = document.getElementById("loadMoreBtn");
    loadMoreBtn.style.display = pageSize * currentPageUsed >= sourceList.length ? "none" : "block";
}


/**
 * ✅ Loads more articles (for pagination)
 */
function loadMoreArticles() {
    if (isSearchActive) {
        filteredPage++;
    } else {
        currentPage++;
    }
    renderVisibleArticles();
}

/**
 * ✅ Resets the search state and renders all articles
 */
function resetSearch() {
    isSearchActive = false;
    filteredArticles = [];
    filteredPage = 1;
    currentPage = 1;
    renderVisibleArticles();
}

/**
 * ✅ Saves an article for the logged-in user
 */
function saveArticle(articleId) {
    if (!requireLogin()) return;
    const user = getLoggedUser();

    fetch("/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                showSaveModal();
            } else {
                alert("❌ Failed to save.");
            }
        })
        .catch(() => alert("❌ Network error."));
}

/**
 * ✅ Displays the "Article Saved" modal
 */
function showSaveModal() {
    const modalHTML = `
        <div class="save-modal-overlay" id="saveModalOverlay">
            <div class="save-modal">
                <div class="save-modal-particles"></div>
                <div class="save-modal-icon"></div>
                <h2 class="save-modal-title">Article Saved!</h2>
                <p class="save-modal-subtitle">The article has been successfully added to your favorites</p>
                <button class="save-modal-close" onclick="closeSaveModal()">Got it</button>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    setTimeout(() => {
        document.getElementById('saveModalOverlay').classList.add('show');
    }, 100);

    setTimeout(() => {
        closeSaveModal();
    }, 4000);
}

/**
 * ✅ Closes the "Article Saved" modal
 */
function closeSaveModal() {
    const overlay = document.getElementById('saveModalOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}


// Submit the share request based on selected type
function submitShare(articleId) {
    const user = getLoggedUser();
    const shareType = document.getElementById('shareType').value;
    const comment = document.getElementById('shareComment').value.trim();

    if (!user?.name || !user?.id) {
        alert("Please log in.");
        return;
    }

    const canShare = sessionStorage.getItem("canShare") === "true";
    if (!canShare) {
        alert("🚫 Your sharing ability is blocked!");
        return;
    }

    if (shareType === "public" && comment === "") {
        alert("🚫 You must enter a comment when sharing to Threads!");
        return;
    }

    if (shareType === "private") {
        const toUsername = document.getElementById('targetUser').value.trim();
        if (!toUsername) {
            alert("Please enter a username.");
            return;
        }
        if (toUsername.toLowerCase() === user.name.toLowerCase()) {
            alert("🚫 You cannot share an article with yourself.");
            return;
        }

        fetch("/api/Articles/Share", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                senderUsername: user.name,
                toUsername,
                articleId,
                comment
            })
        })
            .then(res => {
                if (res.ok) {
                    closeShareModal();
                    showShareSuccessModal();
                } else {
                    alert("❌ Error sharing.");
                }
            })
            .catch(() => alert("❌ Error."));
    } else {
        fetch("/api/Articles/SharePublic", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                userId: user.id,
                articleId,
                comment
            })
        })
            .then(res => {
                if (res.ok) {
                    closeShareModal();
                    showShareSuccessModal();
                } else {
                    alert("❌ Error.");
                }
            })
            .catch(() => alert("❌ Error."));
    }
}

// Close the share modal
function closeShareModal() {
    const overlay = document.getElementById('shareModalOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}

// Show success modal after sharing
function showShareSuccessModal() {
    const modalHTML = `
        <div class="save-modal-overlay" id="shareSuccessOverlay">
            <div class="save-modal">
                <div class="save-modal-particles"></div>
                <div class="save-modal-icon"></div>
                <h2 class="save-modal-title">Article Shared!</h2>
                <p class="save-modal-subtitle">The article has been successfully shared</p>
                <button class="save-modal-close" onclick="closeShareSuccessModal()">Great!</button>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    setTimeout(() => {
        document.getElementById('shareSuccessOverlay').classList.add('show');
    }, 100);

    setTimeout(() => {
        closeShareSuccessModal();
    }, 3000);
}

// Close the share success modal
function closeShareSuccessModal() {
    const overlay = document.getElementById('shareSuccessOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}


// Send a comment for an article (in modal or regular view)
function sendComment(articleId, isModal = false) {
    const user = getLoggedUser();
    const commentBoxId = isModal ? `modal-commentBox-${articleId}` : `commentBox-${articleId}`;
    const comment = document.getElementById(commentBoxId).value.trim();

    if (!comment) {
        alert("Please write a comment!");
        return;
    }

    const canComment = sessionStorage.getItem("canComment") === "true";
    if (!canComment) {
        alert("🚫 Your commenting ability is blocked!");
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
                document.getElementById(commentBoxId).value = "";
                loadComments(articleId, isModal);
            } else {
                alert("❌ You may not be allowed to comment.");
            }
        })
        .catch(() => alert("❌ Network error"));
}

// Load comments for an article
function loadComments(articleId, isModal = false) {
    const targetId = isModal ? `modal-comments-${articleId}` : `comments-${articleId}`;
    const container = document.getElementById(targetId);
    if (!container) return;

    fetch(`/api/Articles/GetComments/${articleId}`)
        .then(res => res.json())
        .then(comments => {
            container.innerHTML = "";
            comments.forEach(c => {
                container.innerHTML += `
                    <div class="border rounded p-2 mb-1">
                        <strong>${c.username}</strong>: ${c.commentText}
                        <button class='btn btn-sm btn-outline-danger ms-2' onclick='toggleCommentLike(${c.id})'>
                            ❤️
                        </button>
                        <span id="like-count-${c.id}" class="ms-1">0</span>
                        <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id})'>🚩</button>
                    </div>`;
                updateLikeCount(c.id);
            });
        });
}

// Open the comments modal for an article
function openCommentsModal(articleId) {
    if (!requireLogin()) return;

    const modalHTML = `
        <div class="comments-modal-overlay" id="commentsModalOverlay-${articleId}">
            <div class="comments-modal">
                <h3>Comments</h3>
                <div id="modal-comments-${articleId}" class="modal-comments-container"></div>
                <textarea id="modal-commentBox-${articleId}" class="form-control mt-2" placeholder="Write a comment..."></textarea>
                <div class="modal-buttons">
                    <button class="btn btn-sm btn-secondary" onclick="closeCommentsModal(${articleId})">Close</button>
                    <button class="btn btn-sm btn-primary" onclick="sendComment(${articleId}, true)">Send</button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML("beforeend", modalHTML);

    setTimeout(() => {
        document.getElementById(`commentsModalOverlay-${articleId}`).classList.add("show");
        loadComments(articleId, true);
    }, 50);
}

// Close the comments modal
function closeCommentsModal(articleId) {
    const modal = document.getElementById(`commentsModalOverlay-${articleId}`);
    if (modal) {
        modal.classList.remove("show");
        setTimeout(() => modal.remove(), 300);
    }
}

// Toggle like for a comment
function toggleCommentLike(commentId) {
    const user = getLoggedUser();
    fetch('/api/Articles/ToggleCommentLike', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, commentId })
    }).then(() => updateLikeCount(commentId));
}

// Update like count display for a comment
function updateLikeCount(commentId) {
    fetch(`/api/Articles/CommentLikeCount/${commentId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`like-count-${commentId}`).innerText = count;
        });
}


// Show the report modal for article or comment
function showReportModal(referenceType, referenceId) {
    if (!requireLogin()) return;

    const existing = document.getElementById('reportModalOverlay');
    if (existing) existing.remove();

    const modalHTML = `
        <div class="save-modal-overlay" id="reportModalOverlay">
            <div class="save-modal">
                <h2 class="save-modal-title">🚩 Report Content</h2>
                <p class="save-modal-subtitle">Please choose the reason for your report:</p>

                <select id="reportReasonSelect" onchange="toggleOtherReason()" class="form-control mb-2">
                    <option value="harassment">Harassment</option>
                    <option value="hate">Hate Speech</option>
                    <option value="false_info">False Information</option>
                    <option value="explicit">Explicit Content</option>
                    <option value="other">Other</option>
                </select>

                <textarea id="reportOtherReason" class="form-control mb-2" placeholder="Enter reason..." style="display:none;"></textarea>

                <div class="share-modal-buttons">
                    <button class="share-modal-button secondary" onclick="closeReportModal()">Cancel</button>
                    <button class="share-modal-button primary" onclick="submitReport('${referenceType}', ${referenceId})">Submit Report</button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML("beforeend", modalHTML);

    setTimeout(() => {
        document.getElementById("reportModalOverlay").classList.add("show");
    }, 100);
}

// Toggle display of custom reason input
function toggleOtherReason() {
    const select = document.getElementById("reportReasonSelect");
    const otherInput = document.getElementById("reportOtherReason");
    otherInput.style.display = select.value === "other" ? "block" : "none";
}

// Submit the report to the server
function submitReport(referenceType, referenceId) {
    const user = getLoggedUser();
    if (!user || !user.id) {
        alert("You must be logged in to report.");
        return;
    }

    const reasonSelect = document.getElementById("reportReasonSelect").value;
    const otherText = document.getElementById("reportOtherReason").value.trim();
    const reason = reasonSelect === "other" ? otherText : reasonSelect;

    if (!reason) {
        alert("Please provide a reason.");
        return;
    }

    fetch("/api/Articles/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            userId: user.id,
            referenceType,
            referenceId,
            reason
        })
    })
        .then(res => res.ok ? alert("✅ Reported!") : alert("❌ Error reporting."))
        .catch(() => alert("❌ Network error"))
        .finally(() => closeReportModal());
}

// Close the report modal
function closeReportModal() {
    const overlay = document.getElementById("reportModalOverlay");
    if (overlay) {
        overlay.classList.add("hide");
        setTimeout(() => overlay.remove(), 500);
    }
}

// Shortcut functions to report article or comment
function reportArticle(articleId) {
    showReportModal("Article", articleId);
}

function reportComment(commentId) {
    showReportModal("Comment", commentId);
}


// Toggle like for an article
function toggleLike(articleId) {
    if (!requireLogin()) return;
    const user = getLoggedUser();
    const isLiked = document.getElementById(`like-btn-${articleId}`).classList.contains("liked");
    const endpoint = isLiked ? "Unlike" : "Like";

    fetch(`/api/Articles/${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                updateArticleLikeCount(articleId);
                document.getElementById(`like-btn-${articleId}`).classList.toggle("liked");
            }
        });
}

// Update the like count display for an article
function updateArticleLikeCount(articleId) {
    fetch(`/api/Articles/LikesCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`like-count-${articleId}`).innerText = `${count}`;
        });
}


// Initialize the carousel slides and indicators
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

// Move to a specific slide
function goToSlide(index) {
    const slides = document.querySelectorAll(".carousel-slide");
    const dots = document.querySelectorAll(".carousel-dot");
    slides.forEach((slide, i) => slide.classList.toggle("active", i === index));
    dots.forEach((dot, i) => dot.classList.toggle("active", i === index));
    currentSlide = index;
}

// Move to next slide
function nextSlide() {
    goToSlide((currentSlide + 1) % carouselArticles.length);
}

// Move to previous slide
function prevSlide() {
    goToSlide((currentSlide - 1 + carouselArticles.length) % carouselArticles.length);
}

// Start auto slide interval
function startAutoSlide() {
    stopAutoSlide();
    slideInterval = setInterval(nextSlide, 5000);
}

// Stop auto slide interval
function stopAutoSlide() {
    if (slideInterval) clearInterval(slideInterval);
}


// Load sidebar sections: hot news, editor's pick, must see
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
                    div.style.cursor = "pointer";

                    div.innerHTML = `
                        <img src="${article.imageUrl || 'https://via.placeholder.com/60'}" />
                        <div class="sidebar-item-content">
                            <h6>${article.title?.substring(0, 40)}...</h6>
                            <div class="date">${formattedDate}</div>
                        </div>`;

                    div.addEventListener("click", () => {
                        if (article.sourceUrl && article.sourceUrl !== "#") {
                            window.open(article.sourceUrl, "_blank");
                        } else {
                            alert("No article URL available");
                        }
                    });

                    section.appendChild(div);
                });
            });
        });
}


// Toggle visibility of add-article modal
function toggleAddArticleModal() {
    const overlay = document.getElementById("addArticleModalOverlay");
    overlay.classList.toggle("show");
}

// Close modal on click outside
document.addEventListener('click', function (e) {
    if (e.target && e.target.id === 'addArticleModalOverlay') {
        toggleAddArticleModal();
    }
});

// Close modal with Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const overlay = document.getElementById("addArticleModalOverlay");
        if (overlay.classList.contains('show')) {
            toggleAddArticleModal();
        }
    }
});

// Submit new article (admin only)
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

    fetch("/api/Articles/AddUserArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newArticle)
    })
        .then(res => res.ok ? res.json() : res.text())
        .then(() => toggleAddArticleModal())
        .catch(() => alert("❌ Failed to submit article."));
}


// Show add-article form only for admin users
function checkAdminAndShowAddForm() {
    const user = getLoggedUser();
    const addSection = document.querySelector(".add-article-section");

    if (user?.isAdmin) {
        addSection.style.display = "block";
    } else {
        addSection.style.display = "none";
    }
}


// Filter articles by title and date range
function filterArticles() {
    const titleInput = document.getElementById("searchTitle").value.toLowerCase();
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;

    filteredArticles = allArticles.filter(article => {
        const titleMatch = article.title?.toLowerCase().includes(titleInput);
        const articleDate = new Date(article.publishedAt);
        const afterStart = !startDate || articleDate >= new Date(startDate);
        const beforeEnd = !endDate || articleDate <= new Date(endDate);
        return titleMatch && afterStart && beforeEnd;
    });

    isSearchActive = true;
    filteredPage = 1;
    renderVisibleArticles();
}


// Load a tech ad banner only once
function loadAdBanner() {
    if (adLoaded) return;
    adLoaded = true;

    fetch("/api/Ads/Generate?category=technology")
        .then(res => res.json())
        .then(ad => {
            document.getElementById("adImage").src = ad.imageUrl;
            document.getElementById("adText").innerText = ad.text;
        })
        .catch(() => {
            document.getElementById("adText").innerText = "Ad failed to load.";
        });
}


// Require login before performing certain actions
function requireLogin() {
    const user = getLoggedUser();
    if (!user) {
        alert("You must be logged in to perform this action.");
        window.location.href = "login.html";
        return false;
    }
    return true;
}
