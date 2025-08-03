// ✅ Global variables
let allThreads = [];
let currentThreadsPage = 1;
const threadsPageSize = 5;

// ✅ Load threads when the page is ready
document.addEventListener("DOMContentLoaded", function () {
    loadThreadsArticles();
});

// ✅ Get the logged-in user from sessionStorage
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ Load all public thread articles from server
function loadThreadsArticles() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch("/api/Articles/Public/" + user.id)
        .then(res => {
            if (!res.ok) throw new Error("Failed to fetch threads");
            return res.json();
        })
        .then(data => {
            allThreads = data;
            currentThreadsPage = 1;
            renderVisibleThreads();
        })
        .catch(() => {
            showError("threadsContainer", "Failed to load threads");
        });
}

// ✅ Render visible thread articles based on current page
function renderVisibleThreads() {
    const container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    const visibleThreads = allThreads.slice(0, threadsPageSize * currentThreadsPage);

    visibleThreads.forEach(article => {
        const card = createThreadCard(article);
        container.appendChild(card);
        loadComments(article.publicArticleId);
    });

    toggleLoadMoreThreadsButton();
}

// ✅ Load more threads (increment current page)
function loadMoreThreads() {
    currentThreadsPage++;
    renderVisibleThreads();
}

// ✅ Show or hide the Load More button
function toggleLoadMoreThreadsButton() {
    const loadMoreBtn = document.getElementById("loadMoreThreadsBtn");
    if (threadsPageSize * currentThreadsPage >= allThreads.length) {
        loadMoreBtn.style.display = "none";
    } else {
        loadMoreBtn.style.display = "block";
    }
}

// ✅ Render given thread articles list
function renderThreadsArticles(articles) {
    const container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    for (let i = 0; i < articles.length; i++) {
        const article = articles[i];
        const card = createThreadCard(article);
        container.appendChild(card);
        loadComments(article.publicArticleId);
    }
}


// ✅ Create and return a thread card DOM element
function createThreadCard(article) {
    const id = article.publicArticleId;
    const div = document.createElement("div");
    div.className = "thread-card p-3 mb-4 border rounded bg-light";
    div.style.cursor = 'pointer';

    const formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    const tagsHtml = (article.tags && article.tags.length > 0)
        ? article.tags.map(tag => `<span class="tag">${tag}</span>`).join(" ")
        : "";

    const html = `
        <div class="initial-comment">
            <div class="author-wrote">${article.senderName} wrote</div>
            <div class="comment-text">${article.initialComment || ""}</div>
        </div>

        <div class="thread-content">
            <div class="thread-image mb-2" data-date="${formattedDate}" data-author="${article.author || 'Unknown'}">
                <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}" class="img-fluid rounded">
                ${tagsHtml ? `<div class="article-tags">${tagsHtml}</div>` : ''}
            </div>

            <h5>${article.title}</h5>
            <p>${article.description || ""}</p>

            <div class="thread-meta">
                <div class="meta-author">${article.author || 'Unknown'}</div>
                <div class="meta-date">${formattedDate}</div>
            </div>

            <div class="thread-actions">
                <button class='btn btn-sm btn-info comment-btn' id="like-thread-btn-${id}">
                <img src="../pictures/like.png" alt="Like" class="share-icon">
                </button>
                <span id="like-thread-count-${id}" class="ms-2">0</span>
                <button class='btn btn-sm btn-info comment-btn' onclick="showCommentsModal(${id}); event.stopPropagation();">
                    <img src="../pictures/comment1.png" alt="Comment" class="share-icon">
                </button>
                <div class="three-dots-menu" onclick="showThreadOptionsMenu(${id}, '${article.senderName}', ${id}); event.stopPropagation();">
                    ⋯
                    <div class="thread-options-menu" id="options-menu-${id}">
                        <div class="thread-options-content">
                            <button onclick="blockUser('${article.senderName}'); event.stopPropagation();">🚫 Block ${article.senderName}</button>
                            <button onclick="reportArticle(${id}); event.stopPropagation();">🚨 Report Article</button>
                        </div>
                    </div>
                </div>
            </div>

            <button class='btn btn-sm btn-danger mb-2' onclick="blockUser('${article.senderName}'); event.stopPropagation();" style="display: none;">Block ${article.senderName}</button>
            <button class='btn btn-sm btn-warning mb-2' onclick="reportArticle(${id}); event.stopPropagation();" style="display: none;">Report Article</button>

            <h6>💬 Comments:</h6>
            <div id="comments-${id}" onclick="event.stopPropagation();" style="display: none;"></div>
            <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();" style="display: none;"></textarea>
            <button class='btn btn-sm btn-primary' onclick='sendComment(${id}); event.stopPropagation();' style="display: none;">Send</button>
        </div>
    `;

    div.innerHTML = html;

    loadThreadLikeCount(id);

    const likeBtn = div.querySelector(`#like-thread-btn-${id}`);
    if (likeBtn) {
        likeBtn.onclick = function (event) {
            event.stopPropagation();
            toggleThreadLike(article);
        };
    }

    div.addEventListener('click', function () {
        if (article.sourceUrl && article.sourceUrl !== '#') {
            window.open(article.sourceUrl, '_blank');
        } else {
            alert('No article URL available');
        }
    });

    return div;
}

// ✅ Show the options menu (⋯) for a thread
function showThreadOptionsMenu(articleId, senderName, id) {
    document.querySelectorAll('.thread-options-menu').forEach(menu => {
        menu.classList.remove('show');
    });

    const menu = document.getElementById(`options-menu-${articleId}`);
    if (menu) {
        menu.classList.add('show');

        document.addEventListener('click', function closeMenu(e) {
            if (!menu.contains(e.target)) {
                menu.classList.remove('show');
                document.removeEventListener('click', closeMenu);
            }
        });
    }
}


// ✅ Show the comments modal for a given article
function showCommentsModal(articleId) {
    const existingModal = document.getElementById('commentsModal');
    if (existingModal) existingModal.remove();

    const modalHTML = `
        <div class="comments-modal-overlay" id="commentsModal">
            <div class="comments-modal-content">
                <div class="comments-modal-header">
                    <h3>Comments</h3>
                    <button class="close-btn" onclick="closeCommentsModal()">×</button>
                </div>
                <div class="comments-modal-body">
                    <div id="modal-comments-${articleId}" class="comments-list"></div>
                    <div class="comment-input-section">
                        <h4>Write a Comment</h4>
                        <textarea id="modal-commentBox-${articleId}" placeholder="Share your thoughts..."></textarea>
                        <button onclick='sendCommentFromModal(${articleId});'>Send Comment</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    document.body.insertAdjacentHTML("beforeend", modalHTML);
    loadCommentsForModal(articleId);

    setTimeout(() => {
        document.getElementById("commentsModal").classList.add("show");
    }, 10);

    setTimeout(() => {
        document.addEventListener('click', closeCommentsModalOnOutsideClick);
    }, 100);
}

// ✅ Load comments for modal view
function loadCommentsForModal(articleId) {
    fetch("/api/Comments/Public/" + articleId)
        .then(res => res.json())
        .then(comments => {
            const container = document.getElementById("modal-comments-" + articleId);
            container.innerHTML = "";

            comments.forEach(c => {
                const commentDiv = document.createElement('div');
                commentDiv.className = 'border rounded p-2 mb-1';
                commentDiv.innerHTML = `
                    <strong>${c.username}</strong>: ${c.comment}
                    <button onclick="togglePublicCommentLike(${c.id})">❤️</button>
                    <span id="public-like-count-${c.id}">0</span>
                    <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id}); event.stopPropagation();'>Report</button>
                `;
                container.appendChild(commentDiv);
                updatePublicLikeCount(c.id);
            });
        });
}

// ✅ Send a comment from modal
function sendCommentFromModal(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const commentInput = document.getElementById("modal-commentBox-" + articleId);
    const commentText = commentInput.value.trim();

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }

    const payload = {
        publicArticleId: articleId,
        userId: user.id,
        comment: commentText
    };

    fetch("/api/Comments/AddPublic", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error("HTTP " + res.status);
            commentInput.value = "";
            loadCommentsForModal(articleId);
            loadComments(articleId);
        })
        .catch(() => {
            alert("Error posting comment");
        });
}

// ✅ Close the comments modal
function closeCommentsModal() {
    const modal = document.getElementById('commentsModal');
    if (modal) {
        modal.classList.remove('show');
        setTimeout(() => modal.remove(), 300);
    }
    document.removeEventListener('click', closeCommentsModalOnOutsideClick);
}

// ✅ Close modal when clicking outside of it
function closeCommentsModalOnOutsideClick(event) {
    const modal = document.getElementById('commentsModal');
    const modalContent = modal?.querySelector('.comments-modal-content');
    if (modal && !modalContent?.contains(event.target)) {
        closeCommentsModal();
    }
}

// ✅ Update like count for public comment
function updatePublicLikeCount(publicCommentId) {
    fetch(`/api/Likes/PublicCommentLikeCount/${publicCommentId}`)
        .then(res => res.json())
        .then(count => {
            const countSpan = document.getElementById(`public-like-count-${publicCommentId}`);
            if (countSpan) countSpan.innerText = count;
        });
}

// ✅ Toggle like for a public comment
function togglePublicCommentLike(publicCommentId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch('/api/Likes/TogglePublicCommentLike', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, publicCommentId })
    }).then(() => updatePublicLikeCount(publicCommentId));
}


// ✅ Send a comment directly from article card
function sendComment(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const commentInput = document.getElementById("commentBox-" + articleId);
    const commentText = commentInput.value.trim();

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }

    const payload = {
        publicArticleId: articleId,
        userId: user.id,
        comment: commentText
    };

    fetch("/api/Comments/AddPublic", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error("HTTP " + res.status);
            commentInput.value = "";
            loadComments(articleId);
        })
        .catch(() => {
            alert("Error posting comment");
        });
}

// ✅ Load comments into the article card
function loadComments(articleId) {
    fetch("/api/Comments/Public/" + articleId)
        .then(res => res.json())
        .then(comments => {
            const container = document.getElementById("comments-" + articleId);
            container.innerHTML = "";

            comments.forEach(c => {
                const commentDiv = document.createElement('div');
                commentDiv.className = 'border rounded p-2 mb-1';
                commentDiv.innerHTML = `
                    <strong>${c.username}</strong>: ${c.comment}
                    <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id}); event.stopPropagation();'>Report</button>
                `;
                commentDiv.addEventListener('click', function (event) {
                    event.stopPropagation();
                });
                container.appendChild(commentDiv);
            });
        });
}

// ✅ Block a user by their name
function blockUser(senderName) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    if (!confirm(`Are you sure you want to block ${senderName}?`)) return;

    fetch("/api/Users/BlockUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            blockerUserId: user.id,
            blockedUsername: senderName
        })
    })
        .then(res => res.ok ? alert(`✅ ${senderName} blocked!`) : alert("Error blocking user"))
        .then(() => loadThreadsArticles())
        .catch(() => alert("Error"));
}

// ✅ Load and update the like count and button status
function loadThreadLikeCount(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    fetch(`/api/Likes/ThreadLikeCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            const likeCountSpan = document.getElementById(`like-thread-count-${articleId}`);
            if (likeCountSpan) {
                likeCountSpan.textContent = `${count}`;
            }
        });

    if (user?.id) {
        fetch(`/api/Likes/Check/${articleId}/${user.id}`)
            .then(res => res.json())
            .then(hasLiked => {
                const likeBtn = document.getElementById(`like-thread-btn-${articleId}`);
                if (likeBtn) {
                    likeBtn.classList.toggle('liked', hasLiked);
                }
            });
    }
}

// ✅ Toggle like/unlike for a thread
function toggleThreadLike(article) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    const likeBtn = document.getElementById(`like-thread-btn-${article.publicArticleId}`);

    const payload = {
        userId: user.id,
        publicArticleId: article.publicArticleId
    };

    fetch("/api/Likes/ToggleThreadLike", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error("Failed to toggle like");
            if (likeBtn) {
                likeBtn.classList.toggle('liked');
            }
            loadThreadLikeCount(article.publicArticleId);
        })
        .catch(() => {
            alert("Failed to toggle like");
        });
}


// ✅ Show the report modal for a comment or article
function showReportModal(referenceType, referenceId) {
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

// ✅ Show/hide the "Other" textarea if "Other" selected
function toggleOtherReason() {
    const select = document.getElementById("reportReasonSelect");
    const otherInput = document.getElementById("reportOtherReason");
    otherInput.style.display = select.value === "other" ? "block" : "none";
}

// ✅ Submit report to server
function submitReport(referenceType, referenceId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    const reasonSelect = document.getElementById("reportReasonSelect").value;
    const otherText = document.getElementById("reportOtherReason").value.trim();
    const reason = reasonSelect === "other" ? otherText : reasonSelect;

    if (!reason) {
        alert("Please provide a reason.");
        return;
    }

    fetch("/api/Reports/Report", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            userId: user.id,
            referenceType,
            referenceId,
            reason
        })
    })
        .then(res => {
            if (res.ok) {
                alert("✅ Reported!");
            } else {
                alert("❌ Error reporting.");
            }
        })
        .catch(() => alert("❌ Network error"))
        .finally(() => closeReportModal());
}

// ✅ Close the report modal
function closeReportModal() {
    const overlay = document.getElementById("reportModalOverlay");
    if (overlay) {
        overlay.classList.add("hide");
        setTimeout(() => overlay.remove(), 500);
    }
}

// ✅ Report an article
function reportArticle(articleId) {
    showReportModal("Article", articleId);
}

// ✅ Report a comment
function reportComment(commentId) {
    showReportModal("Comment", commentId);
}

// ✅ Show error message inside container
function showError(containerId, message) {
    const container = document.getElementById(containerId);
    container.innerHTML = `<div class='alert alert-danger'>${message}</div>`;
}
