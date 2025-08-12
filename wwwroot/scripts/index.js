// ✅ Global state and control variables
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
    visibleArticles.forEach(article => {
        updateArticleLikeCount(article.id);
    });
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

    fetch("/api/Article/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                showSaveModal();
            } else {
                alert("Failed to save.");
            }
        })
        .catch(() => alert("Network error."));
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


// ✅ Submits the share request to the server
// ✅ Sends private/public article share using usernames (only public doesn't need username)
function submitShare(articleId) {
    const shareType = document.getElementById("shareType").value;
    const comment = document.getElementById("shareComment").value || "";
    const loggedUser = JSON.parse(sessionStorage.getItem("loggedUser"));

    if (loggedUser.canShare === false) {
        alert("You are blocked from sharing articles.");
        return;
    }

    if (shareType === "public") {
        if (comment.trim().length === 0) {
            alert("Please enter a comment before sharing publicly.");
            return;
        }

        // ✅ Public Share
        fetch("/api/Articles/ShareToThreads", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                userId: loggedUser.id,
                articleId: articleId,
                comment: comment
            })
        })
            .then(res => {
                if (!res.ok) throw new Error("Failed to share publicly");
                alert("Article shared publicly to threads!");
                closeShareModal();
            })
            .catch(err => {
                console.error(err);
                alert("Error sharing publicly: " + err.message);
            });

    } else {
        // ✅ Private Share
        const targetUsername = document.getElementById("targetUser").value.trim();
        if (!targetUsername) {
            alert("Please enter a target username.");
            return;
        }

        fetch("/api/Articles/ShareByUsernames", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                senderUsername: loggedUser.name,
                toUsername: targetUsername,
                articleId: articleId,
                comment: comment
            })
        })
            .then(res => {
                if (!res.ok) throw new Error("Failed to share privately");
                alert(`Article shared privately with ${targetUsername}!`);
                closeShareModal();
            })
            .catch(err => {
                console.error(err);
                alert("Error sharing privately: " + err.message);
            });
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

    const canComment = user.canComment === true;
    if (!canComment) {
        alert("Your commenting ability is blocked!");
        return;
    }

    fetch("/api/Comments/AddToArticle", {
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
                alert("You may not be allowed to comment.");
            }
        })
        .catch(() => alert("Network error"));
}

// Load comments for an article
function loadComments(articleId, isModal = false) {
    const targetId = isModal ? `modal-comments-${articleId}` : `comments-${articleId}`;
    const container = document.getElementById(targetId);
    if (!container) return;

    const user = getLoggedUser();

    fetch(`/api/Comments/GetForArticle/${articleId}`)
        .then(res => res.json())
        .then(comments => {
            container.innerHTML = "";

            comments.forEach(c => {
                const isOwner = user && user.id === c.userId;

                container.innerHTML += `
                    <div class="border rounded p-2 mb-1">
                        <strong>${c.username}</strong>: ${c.commentText}
                        <button class='btn btn-sm btn-outline-danger ms-2' onclick='toggleCommentLike(${c.id})'>
                            ❤️
                        </button>
                        <span id="like-count-${c.id}" class="ms-1">0</span>
                        ${!isOwner
                        ? `<button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id})'>🚩</button>`
                        : ''}
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
    fetch('/api/Comments/ToggleLikeForArticles', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, commentId })
    }).then(() => updateLikeCount(commentId));
}

// Update like count display for a comment
function updateLikeCount(commentId) {
    fetch(`/api/Comments/ArticleCommentLikeCount/${commentId}`)
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
                <h2 class="save-modal-title">Report Content</h2>
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
        .then(res => res.ok ? alert("Reported!") : alert("❌ Error reporting."))
        .catch(() => alert("Network error"))
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

    fetch("/api/Likes/ToggleArticleLike", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => res.ok ? res.json() : Promise.reject())
        .then(data => {
            // Update UI according to 'liked' response from server
            const btn = document.getElementById(`like-btn-${articleId}`);
            if (btn) {
                btn.classList.toggle("liked", data.liked); // הוספה או הסרה לפי מה שהשרת החזיר
            }
            updateArticleLikeCount(articleId); // רענון מונה הלייקים
        })
        .catch(() => {
            alert("Failed to toggle like.");
        });
}


// ✅ Listen to real-time like count for an article
function updateArticleLikeCount(articleId) {
    const likeRef = firebase.database().ref(`likes/article_${articleId}`);
    likeRef.on("value", (snapshot) => {
        const count = snapshot.val();
        const element = document.getElementById(`like-count-${articleId}`);
        if (element && count !== null) {
            element.innerText = count;
        }
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
    fetch("/api/Articles/Sidebar?page=1&pageSize=9")
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

    const title = document.getElementById("newTitle").value.trim();
    const description = document.getElementById("newDescription").value.trim();
    const content = document.getElementById("newContent").value.trim();
    const author = document.getElementById("newAuthor").value.trim();
    const sourceUrl = document.getElementById("newSourceUrl").value.trim();
    const imageUrl = document.getElementById("newImageUrl").value.trim();
    const publishedAtRaw = document.getElementById("newPublishedAt").value.trim();
    const tagsRaw = document.getElementById("newTags").value.trim();

    if (!title || !description || !content || !author || !sourceUrl || !imageUrl || !publishedAtRaw || !tagsRaw) {
        alert("Please fill in all fields before submitting.");
        return;
    }

    const publishedAt = new Date(publishedAtRaw);
    if (isNaN(publishedAt)) {
        alert("Please enter a valid published date.");
        return;
    }

    const tags = tagsRaw.split(",").map(s => s.trim()).filter(s => s !== "");

    const newArticle = {
        title,
        description,
        content,
        author,
        sourceUrl,
        imageUrl,
        publishedAt,
        tags
    };

    fetch("/api/Admin/AddUserArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newArticle)
    })
        .then(res => res.ok ? res.json() : Promise.reject(res))
        .then(() => {
            alert("✅ Article added successfully.");
            toggleAddArticleModal();
        })
        .catch(async res => {
            const errorText = await res.text();
            if (errorText.includes("already exists")) {
                alert("❌ Article already exists.");
            } else {
                alert("❌ Failed to submit article: " + errorText);
            }
        });
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

function toggleShare(articleId) {
    const modalHtml = `
        <div class="save-modal-overlay" id="shareModalOverlay">
            <div class="save-modal">
                <h2 class="save-modal-title">Share Article</h2>
                <select id="shareType" class="form-control mb-2" onchange="toggleShareType()">
                    <option value="private">Private Share</option>
                    <option value="public">Public Thread</option>
                </select>
                <input type="text" id="targetUser" placeholder="Target username (for private share)" class="form-control mb-2">
                <textarea id="shareComment" placeholder="Add a comment..." class="form-control mb-2"></textarea>
                <div class="modal-buttons">
                    <button class="btn btn-secondary" onclick="closeShareModal()">Cancel</button>
                    <button class="btn btn-primary" onclick="submitShare(${articleId})">Share</button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML("beforeend", modalHtml);
    setTimeout(() => {
        document.getElementById("shareModalOverlay").classList.add("show");
        toggleShareType(); 
    }, 50);
}

// ✅ Shows/hides the target user field based on selected share type
function toggleShareType() {
    const shareType = document.getElementById("shareType").value;
    const targetUser = document.getElementById("targetUser");
    targetUser.style.display = shareType === "private" ? "block" : "none";
}


// Load a tech ad banner only once
function loadAdBanner() {
    if (adLoaded) return;
    adLoaded = true;

    fetch("/api/Ads/Generate?category=breaking news")
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
