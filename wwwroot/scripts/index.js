// ✅ Global Variables
let currentPage = 1;
let filteredPage = 1;
const pageSize = 6;
let allArticles = [];
let carouselArticles = [];
let currentSlide = 0;
let slideInterval;

// ✅ Get logged user
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ Initialize on page load
document.addEventListener("DOMContentLoaded", () => {
    loadAllArticlesAndSplit();
    loadSidebarSections();
    checkAdminAndShowAddForm();
});

// ✅ Load all articles and split for carousel and grid
function loadAllArticlesAndSplit() {
    const user = getLoggedUser();
    const userId = user ? user.id : 3;

    fetch(`/api/Articles/AllFiltered?userId=${userId}`)
        .then(res => res.json())
        .then(data => {
            if (!data || data.length === 0) {
                console.warn("No articles found");
                return;
            }

            // תמיד לדלג על 8 הראשונות (מוקצות ל־Side Articles)
            data = data.slice(8);

            carouselArticles = data.slice(0, 5);
            allArticles = data.slice(5);

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



// ✅ Grid - with tag positioning fix only
function renderVisibleArticles() {
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    const visibleArticles = allArticles.slice(0, pageSize * currentPage);

    visibleArticles.forEach(article => {
        const div = document.createElement("div");
        div.className = "article-card";

        // הוספת cursor pointer וקליק לכרטיס כולו
        div.style.cursor = 'pointer';

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
                <span>${article.author}</span>
                <span>${formattedDate}</span>
            </div>
            <div class="article-actions">
                <button class="like-btn" id="like-btn-${article.id}" onclick="toggleLike(${article.id}); event.stopPropagation();">
                    <img src="../pictures/like.png" alt="Like" title="Like">
                </button>
                <span id="like-count-${article.id}" class="like-count">❤️ 0</span>

                <button class="btn btn-sm btn-info" onclick="toggleComments(${article.id}); event.stopPropagation();">
                    <img src="../pictures/comment.png" alt="Comment" title="Comment">
                </button>
                <button class="save-btn" onclick="saveArticle(${article.id}); event.stopPropagation();">
                    <img src="../pictures/save.png" alt="Save" title="Save">
                </button>
                <button class="btn btn-sm btn-success" onclick="toggleShare(${article.id}); event.stopPropagation();">
                    <img src="../pictures/share.png" alt="Share" title="Share">
                </button>
                <button class="btn btn-sm btn-danger" onclick="reportArticle(${article.id}); event.stopPropagation();">
                    <img src="../pictures/report.png" alt="Report" title="Report">
                </button>
            </div>

            <div class="article-comments mt-3" id="commentsSection-${article.id}" style="display:none;">
                <h6>Comments:</h6>
                <div id="comments-${article.id}"></div>
                <textarea id="commentBox-${article.id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();"></textarea>
                <button onclick="sendComment(${article.id}); event.stopPropagation();" class="btn btn-sm btn-primary">Send</button>
            </div>
        </div>
    `;
        updateLikeCount(article.id);

        // הוספת event listener לכרטיס כולו
        div.addEventListener('click', function () {
            if (article.sourceUrl && article.sourceUrl !== '#') {
                window.open(article.sourceUrl, '_blank');
            } else {
                alert('No article URL available');
            }
        });

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

// ✅ Toggle comments display
function toggleComments(articleId) {
    const commentsSection = document.getElementById(`commentsSection-${articleId}`);
    if (commentsSection) {
        commentsSection.style.display = commentsSection.style.display === "none" ? "block" : "none";
    }
}

// ✅ Updated function for saving article with special modal
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
        .then(res => {
            if (res.ok) {
                showSaveModal();
            } else {
                alert("❌ Failed to save.");
            }
        })
        .catch(() => alert("❌ Network error."));
}

// Function to display the special save modal
function showSaveModal() {
    // Create the modal HTML
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

    // Add the modal to the page body
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal with animation
    setTimeout(() => {
        document.getElementById('saveModalOverlay').classList.add('show');
    }, 100);

    // Auto close after 4 seconds
    setTimeout(() => {
        closeSaveModal();
    }, 4000);
}

// Function to close the save modal
function closeSaveModal() {
    const overlay = document.getElementById('saveModalOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}

// ✅ Updated function for sharing article with special modal
function toggleShare(articleId) {
    showShareModal(articleId);
}

// Function to display the special share modal
function showShareModal(articleId) {
    // Create the modal HTML
    const modalHTML = `
        <div class="share-modal-overlay" id="shareModalOverlay">
            <div class="share-modal">
                <div class="share-modal-particles"></div>
                <div class="share-modal-icon"></div>
                <h2 class="share-modal-title">Share Article</h2>
                <p class="share-modal-subtitle">Choose how you want to share this article</p>
                
                <form class="share-modal-form" id="shareModalForm">
                    <div class="form-group">
                        <label for="shareType">Share Type</label>
                        <select id="shareType" onchange="toggleShareModalType()">
                            <option value="private">Share with specific user</option>
                            <option value="public">Share with everyone</option>
                        </select>
                    </div>
                    
                    <div class="form-group" id="targetUserGroup">
                        <label for="targetUser">Username</label>
                        <input type="text" id="targetUser" placeholder="Enter username to share with" />
                    </div>
                    
                    <div class="form-group">
                        <label for="shareComment">Comment (Optional)</label>
                        <textarea id="shareComment" placeholder="Add a comment about this article..."></textarea>
                    </div>
                </form>
                
                <div class="share-modal-buttons">
                    <button class="share-modal-button secondary" onclick="closeShareModal()">Cancel</button>
                    <button class="share-modal-button primary" onclick="submitShare(${articleId})">Share Article</button>
                </div>
            </div>
        </div>
    `;

    // Add the modal to the page body
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal with animation
    setTimeout(() => {
        document.getElementById('shareModalOverlay').classList.add('show');
    }, 100);
}

// Function to toggle share type in modal
function toggleShareModalType() {
    const shareType = document.getElementById('shareType').value;
    const targetUserGroup = document.getElementById('targetUserGroup');

    if (shareType === 'public') {
        targetUserGroup.style.display = 'none';
    } else {
        targetUserGroup.style.display = 'block';
    }
}

function toggleLike(articleId) {
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
                updateLikeCount(articleId);
                document.getElementById(`like-btn-${articleId}`).classList.toggle("liked");
            }
        });
}

function updateLikeCount(articleId) {
    fetch(`/api/Articles/LikesCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`like-count-${articleId}`).innerText = `${count} ❤️`;
        });
}


// Function to submit share from modal
function submitShare(articleId) {
    const user = getLoggedUser();
    const shareType = document.getElementById('shareType').value;
    const comment = document.getElementById('shareComment').value.trim();

    if (!user?.name || !user?.id) {
        alert("Please log in.");
        return;
    }

    // Check if user is blocked from sharing
    const canShare = sessionStorage.getItem("canShare") === "true";
    if (!canShare) {
        alert("🚫 Your sharing ability is blocked!");
        return;
    }

    if (shareType === "private") {
        const toUsername = document.getElementById('targetUser').value.trim();
        if (!toUsername) {
            alert("Please enter a username.");
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

// Function to close the share modal
function closeShareModal() {
    const overlay = document.getElementById('shareModalOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}

// Function to show success message after sharing
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

// Function to close share success modal
function closeShareSuccessModal() {
    const overlay = document.getElementById('shareSuccessOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => {
            overlay.remove();
        }, 600);
    }
}

// ✅ Add comment
function sendComment(articleId) {
    const user = getLoggedUser();
    const comment = document.getElementById(`commentBox-${articleId}`).value.trim();

    if (!comment) {
        alert("Please write a comment!");
        return;
    }

    // Check if user is blocked from commenting
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
                document.getElementById(`commentBox-${articleId}`).value = "";
                loadComments(articleId);
            } else {
                alert("❌ You may not be allowed to comment.");
            }
        })
        .catch(() => alert("❌ Network error"));
}

// ✅ Load comments
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

function showReportModal(referenceType, referenceId) {
    // הסר מודל קודם אם קיים
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
function toggleOtherReason() {
    const select = document.getElementById("reportReasonSelect");
    const otherInput = document.getElementById("reportOtherReason");
    otherInput.style.display = select.value === "other" ? "block" : "none";
}

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

function closeReportModal() {
    const overlay = document.getElementById("reportModalOverlay");
    if (overlay) {
        overlay.classList.add("hide");
        setTimeout(() => overlay.remove(), 500);
    }
}
function reportArticle(articleId) {
    showReportModal("Article", articleId);
}

function reportComment(commentId) {
    showReportModal("Comment", commentId);
}

// ✅ Report comment
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

// ✅ Like article function
function likeArticle(articleId) {
    const user = getLoggedUser();
    if (!user?.id || !articleId) {
        alert("Please log in to like articles.");
        return;
    }

    fetch("/api/Articles/Like", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => res.ok ? alert("✅ Article liked!") : alert("❌ Failed to like."))
        .catch(() => alert("❌ Network error."));
}

// ✅ Carousel
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

function prevSlide() {
    goToSlide((currentSlide - 1 + carouselArticles.length) % carouselArticles.length);
}

function startAutoSlide() {
    stopAutoSlide();
    slideInterval = setInterval(nextSlide, 5000);
}

function stopAutoSlide() {
    if (slideInterval) clearInterval(slideInterval);
}

// ✅ Regular Sidebar
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

                    // ✅ פתיחת הכתבה בלחיצה
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

// Close modals on background click
document.addEventListener('click', function (e) {
    if (e.target && e.target.classList.contains('save-modal-overlay')) {
        closeSaveModal();
    }
    if (e.target && e.target.classList.contains('share-modal-overlay')) {
        closeShareModal();
    }
    if (e.target && e.target.classList.contains('save-modal-overlay') && e.target.id === 'shareSuccessOverlay') {
        closeShareSuccessModal();
    }
});

function checkAdminAndShowAddForm() {
    const user = getLoggedUser();
    const addSection = document.querySelector(".add-article-section");

    if (user?.isAdmin) {
        addSection.style.display = "block";
    } else {
        addSection.style.display = "none";
    }
}

function filterArticles() {
    const titleInput = document.getElementById("searchTitle").value.toLowerCase();
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;

    const filtered = allArticles.filter(article => {
        const titleMatch = article.title?.toLowerCase().includes(titleInput);

        const articleDate = new Date(article.publishedAt);
        const afterStart = !startDate || articleDate >= new Date(startDate);
        const beforeEnd = !endDate || articleDate <= new Date(endDate);

        return titleMatch && afterStart && beforeEnd;
    });

    renderFilteredArticles(filtered);
}


function renderFilteredArticles(filteredArticles) {
    const grid = document.getElementById("articlesGrid");
    grid.innerHTML = "";

    if (filteredArticles.length === 0) {
        grid.innerHTML = `<div class="alert alert-warning">No articles found for your search</div>`;
        return;
    }

    filteredArticles.forEach(article => {
        const div = document.createElement("div");
        div.className = "article-card";
        div.style.cursor = "pointer";

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
                    <span>${article.author}</span>
                    <span>${formattedDate}</span>
                </div>
                <div class="article-actions">
                    <button class="like-btn" id="like-btn-${article.id}" onclick="toggleLike(${article.id}); event.stopPropagation();">
                        <img src="../pictures/like.png" alt="Like" title="Like">
                    </button>
                    <span id="like-count-${article.id}" class="like-count">❤️ 0</span>

                    <button class="btn btn-sm btn-info" onclick="toggleComments(${article.id}); event.stopPropagation();">
                        <img src="../pictures/comment.png" alt="Comment" title="Comment">
                    </button>
                    <button class="save-btn" onclick="saveArticle(${article.id}); event.stopPropagation();">
                        <img src="../pictures/save.png" alt="Save" title="Save">
                    </button>
                    <button class="btn btn-sm btn-success" onclick="toggleShare(${article.id}); event.stopPropagation();">
                        <img src="../pictures/share.png" alt="Share" title="Share">
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="reportArticle(${article.id}); event.stopPropagation();">
                        <img src="../pictures/report.png" alt="Report" title="Report">
                    </button>
                </div>

                <div class="article-comments mt-3" id="commentsSection-${article.id}" style="display:none;">
                    <h6>Comments:</h6>
                    <div id="comments-${article.id}"></div>
                    <textarea id="commentBox-${article.id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();"></textarea>
                    <button onclick="sendComment(${article.id}); event.stopPropagation();" class="btn btn-sm btn-primary">Send</button>
                </div>
            </div>`;

        div.addEventListener("click", () => {
            if (article.sourceUrl && article.sourceUrl !== "#") {
                window.open(article.sourceUrl, "_blank");
            } else {
                alert("No article URL available");
            }
        });

        grid.appendChild(div);

        updateLikeCount(article.id);
        loadComments(article.id);
    });

    document.getElementById("loadMoreBtn").style.display = "none";
}
