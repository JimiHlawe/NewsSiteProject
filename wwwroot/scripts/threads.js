// ✅ Global Variables
let allThreads = [];
let currentThreadsPage = 1;
const threadsPageSize = 5;

// ✅ Run on page load
document.addEventListener("DOMContentLoaded", function () {
    loadThreadsArticles();
});

// ✅ Get currently logged user from session
function getLoggedUser() {
    const raw = sessionStorage.getItem("loggedUser");
    return raw ? JSON.parse(raw) : null;
}

// ✅ Load all public THREADS from server
function loadThreadsArticles() {
    const user = getLoggedUser();
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

// ✅ Render current visible threads based on page number
function renderVisibleThreads() {
    const container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    const visibleThreads = allThreads.slice(0, threadsPageSize * currentThreadsPage);

    visibleThreads.forEach(article => {
        const card = createThreadCard(article);
        container.appendChild(card);
        loadComments(article.publicArticleId);
    });

    const loadMoreBtn = document.getElementById("loadMoreThreadsBtn");
    if (threadsPageSize * currentThreadsPage >= allThreads.length) {
        loadMoreBtn.style.display = "none";
    } else {
        loadMoreBtn.style.display = "block";
    }
}

// ✅ Load next page of threads
function loadMoreThreads() {
    currentThreadsPage++;
    renderVisibleThreads();
}


// ✅ Create thread card element from article data
function createThreadCard(article) {
    const id = article.publicArticleId;
    const div = document.createElement("div");
    div.className = "thread-card p-3 mb-4 border rounded bg-light";
    div.style.cursor = "pointer";

    const formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    // ✅ Build tags section above image (if exists)
    const tagsHtml = article.tags?.length
        ? article.tags.map(tag => `<span class="tag">${tag}</span>`).join(" ")
        : "";

    // ✅ Build full HTML of thread
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
                <button class='btn btn-sm btn-outline-primary' id="like-thread-btn-${id}"></button>
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

            <h6>💬 Comments:</h6>
            <div id="comments-${id}" onclick="event.stopPropagation();" style="display: none;"></div>
            <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();" style="display: none;"></textarea>
            <button class='btn btn-sm btn-primary' onclick='sendComment(${id}); event.stopPropagation();' style="display: none;">Send</button>
        </div>
    `;

    div.innerHTML = html;

    // ✅ Load like count and setup like toggle
    loadThreadLikeCount(id);
    const likeBtn = div.querySelector(`#like-thread-btn-${id}`);
    if (likeBtn) {
        likeBtn.onclick = function (event) {
            event.stopPropagation();
            toggleThreadLike(article);
        };
    }

    // ✅ Open article in new tab when clicking the card
    div.addEventListener('click', function () {
        if (article.sourceUrl && article.sourceUrl !== '#') {
            window.open(article.sourceUrl, '_blank');
        } else {
            alert('No article URL available');
        }
    });

    return div;
}


// ✅ Show comments modal for a given thread (publicArticleId)
function showCommentsModal(publicArticleId) {
    const user = getLoggedUser();
    if (!user) {
        alert("Please log in to comment.");
        return;
    }

    const modal = document.getElementById("commentsModal");
    const commentList = document.getElementById("modalCommentList");
    const sendBtn = document.getElementById("sendCommentBtn");
    const inputBox = document.getElementById("modalCommentInput");

    commentList.innerHTML = "<div class='loading-placeholder'>Loading comments...</div>";
    modal.style.display = "block";
    sendBtn.onclick = function () {
        sendCommentFromModal(publicArticleId);
    };

    fetch(`/api/Articles/GetComments/${publicArticleId}`)
        .then(res => res.json())
        .then(comments => {
            commentList.innerHTML = "";

            if (comments.length === 0) {
                commentList.innerHTML = "<div class='empty-state'>No comments yet</div>";
                return;
            }

            comments.forEach(comment => {
                const div = document.createElement("div");
                div.className = "thread-comment";

                const content = `
                    <strong>${comment.senderName}:</strong>
                    <span>${comment.content}</span>
                    <button class="btn btn-sm btn-danger ms-2" onclick="reportComment(${comment.id}); event.stopPropagation();">🚩</button>
                `;

                div.innerHTML = content;
                commentList.appendChild(div);
            });
        })
        .catch(() => {
            commentList.innerHTML = "<div class='error-state'>Failed to load comments</div>";
        });
}


// ✅ Send comment from modal input
function sendCommentFromModal(publicArticleId) {
    const user = getLoggedUser();
    const content = document.getElementById("modalCommentInput").value.trim();
    if (!content) {
        alert("Please write something before sending");
        return;
    }

    fetch("/api/Articles/AddComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ publicArticleId, senderId: user.id, content })
    })
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(() => {
            document.getElementById("modalCommentInput").value = "";
            showCommentsModal(publicArticleId); // Reload comments
        })
        .catch(() => {
            alert("❌ Failed to send comment");
        });
}


// ✅ Toggle like on a thread (public article)
function toggleThreadLike(article) {
    const user = getLoggedUser();
    if (!user) {
        alert("Please login first.");
        return;
    }

    const payload = {
        userId: user.id,
        publicArticleId: article.publicArticleId
    };

    fetch("/api/Articles/ToggleThreadLike", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error("Failed to toggle like");
            loadThreadLikeCount(article.publicArticleId); // עדכון הספירה
        })
        .catch(err => {
            console.error("❌ Failed to toggle like:", err);
        });
}

// ✅ Load current like count for a thread
function loadThreadLikeCount(articleId) {
    const countEl = document.getElementById(`like-thread-count-${articleId}`);
    const likeBtn = document.getElementById(`like-thread-btn-${articleId}`);
    const user = getLoggedUser();

    fetch(`/api/Articles/GetThreadLikeCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            if (countEl) countEl.innerText = count;
        });

    if (user) {
        fetch(`/api/Articles/CheckUserLike/${articleId}/${user.id}`)
            .then(res => res.json())
            .then(hasLiked => {
                if (likeBtn) {
                    likeBtn.classList.toggle("liked", hasLiked);
                }
            });
    }
}


// ✅ Toggle like on a public comment
function togglePublicCommentLike(publicCommentId) {
    const user = getLoggedUser();
    if (!user) {
        alert("Login required");
        return;
    }

    fetch('/api/Articles/TogglePublicCommentLike', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, publicCommentId })
    })
        .then(() => updatePublicLikeCount(publicCommentId));
}

// ✅ Refresh public comment like count
function updatePublicLikeCount(publicCommentId) {
    fetch(`/api/Articles/PublicCommentLikeCount/${publicCommentId}`)
        .then(res => res.json())
        .then(count => {
            const el = document.getElementById(`public-like-count-${publicCommentId}`);
            if (el) el.innerText = count;
        });
}


// ✅ Block another user (prevents seeing their threads)
function blockUser(senderName) {
    const user = getLoggedUser();
    if (!user) {
        alert("Login required");
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


// ✅ Show report modal with type (article/comment) and IDs
function showReportModal(type, id, extraInfo = "") {
    const modal = document.getElementById("reportModal");
    const form = document.getElementById("reportForm");

    form.dataset.type = type;
    form.dataset.id = id;
    form.dataset.extra = extraInfo;

    document.getElementById("reportReason").value = "";
    modal.style.display = "block";
}


// ✅ Submit report (called from modal)
function submitReport() {
    const form = document.getElementById("reportForm");
    const reason = document.getElementById("reportReason").value.trim();
    const type = form.dataset.type;
    const id = form.dataset.id;
    const extra = form.dataset.extra;

    const user = getLoggedUser();
    if (!user) {
        alert("Login required");
        return;
    }

    if (!reason) {
        alert("Please provide a reason for the report");
        return;
    }

    const payload = {
        reporterId: user.id,
        reportType: type,
        contentId: parseInt(id),
        reason,
        extraInfo: extra
    };

    fetch("/api/Reports/SubmitReport", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error();
            alert("✅ Report submitted successfully");
            closeReportModal();
        })
        .catch(() => {
            alert("❌ Failed to submit report");
        });
}


// ✅ Close report modal
function closeReportModal() {
    const modal = document.getElementById("reportModal");
    modal.style.display = "none";
}


// ✅ Load all public threads for the logged-in user
function loadThreadsArticles() {
    const user = getLoggedUser();
    if (!user) return;

    fetch(`/api/Articles/Public/${user.id}`)
        .then(res => res.json())
        .then(data => {
            allThreads = data;
            currentThreadsPage = 1;
            renderVisibleThreads();
        })
        .catch(err => {
            console.error("❌ Failed to load threads:", err);
            showError("threadsContainer", "Failed to load threads");
        });
}


// ✅ Render threads for the current page
function renderVisibleThreads() {
    const container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    const visibleThreads = allThreads.slice(0, threadsPageSize * currentThreadsPage);

    visibleThreads.forEach(article => {
        const card = createThreadCard(article);
        container.appendChild(card);
        loadComments(article.publicArticleId);
    });

    const loadMoreBtn = document.getElementById("loadMoreThreadsBtn");
    loadMoreBtn.style.display = (threadsPageSize * currentThreadsPage < allThreads.length) ? "block" : "none";
}

// ✅ Load more threads on button click
function loadMoreThreads() {
    currentThreadsPage++;
    renderVisibleThreads();
}


// ✅ Build thread card UI
function createThreadCard(article) {
    const id = article.publicArticleId;
    const div = document.createElement("div");
    div.className = "thread-card";

    const date = new Date(article.publishedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    const tagsHtml = (article.tags && article.tags.length > 0)
        ? article.tags.map(tag => `<span class="tag">${tag}</span>`).join(" ")
        : "";

    div.innerHTML = `
        <div class="initial-comment">
            <strong>${article.senderName}:</strong> ${article.initialComment || ""}
        </div>
        <div class="thread-image">
            <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}">
            ${tagsHtml ? `<div class="article-tags">${tagsHtml}</div>` : ""}
        </div>
        <h5>${article.title}</h5>
        <p>${article.description || ""}</p>
        <div class="thread-meta">
            <small>By ${article.author || "Unknown"} | ${date}</small>
        </div>
        <div class="thread-actions">
            <button id="like-thread-btn-${id}" class="btn btn-outline-primary btn-sm">Like</button>
            <span id="like-thread-count-${id}">0</span>
            <button class="btn btn-info btn-sm" onclick="showCommentsModal(${id})">💬</button>
            <div class="three-dots-menu" onclick="showThreadOptionsMenu(${id}, '${article.senderName}', ${id})">⋯</div>
        </div>
        <div id="comments-${id}" style="display:none;"></div>
        <textarea id="commentBox-${id}" style="display:none;" class="form-control mt-1" placeholder="Write a comment..."></textarea>
        <button class="btn btn-primary btn-sm" style="display:none;" onclick="sendComment(${id})">Send</button>
    `;

    const likeBtn = div.querySelector(`#like-thread-btn-${id}`);
    likeBtn.onclick = (e) => {
        e.stopPropagation();
        toggleThreadLike(article);
    };

    div.addEventListener('click', () => {
        if (article.sourceUrl) window.open(article.sourceUrl, "_blank");
    });

    loadThreadLikeCount(id);

    return div;
}


// ✅ Load like count for a thread
function loadThreadLikeCount(threadId) {
    fetch(`/api/Articles/PublicLikeCount/${threadId}`)
        .then(res => res.json())
        .then(count => {
            const span = document.getElementById(`like-thread-count-${threadId}`);
            if (span) span.innerText = count;
        })
        .catch(() => console.warn("❌ Failed to load like count"));
}

// ✅ Like/unlike a thread
function toggleThreadLike(article) {
    const user = getLoggedUser();
    if (!user) return;

    const payload = {
        userId: user.id,
        publicArticleId: article.publicArticleId
    };

    fetch("/api/Articles/LikePublicArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error();
            loadThreadLikeCount(article.publicArticleId);
        })
        .catch(() => alert("Failed to like article"));
}


// ✅ Send a public comment
function sendComment(publicArticleId) {
    const user = getLoggedUser();
    const text = document.getElementById(`commentBox-${publicArticleId}`).value.trim();
    if (!text) return;

    const payload = {
        userId: user.id,
        publicArticleId,
        text
    };

    fetch("/api/Articles/CommentOnPublicArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error();
            document.getElementById(`commentBox-${publicArticleId}`).value = "";
            loadComments(publicArticleId);
        })
        .catch(() => alert("Failed to send comment"));
}


// ✅ Load comments for a given thread
function loadComments(publicArticleId) {
    const container = document.getElementById(`comments-${publicArticleId}`);
    if (!container) return;

    fetch(`/api/Articles/GetCommentsForPublicArticle/${publicArticleId}`)
        .then(res => res.json())
        .then(comments => {
            container.innerHTML = "";

            if (comments.length === 0) {
                container.innerHTML = "<div class='empty-state'>No comments yet</div>";
                return;
            }

            comments.forEach(comment => {
                const div = document.createElement("div");
                div.className = "comment";
                div.innerHTML = `
                    <strong>${comment.userName}</strong>: ${comment.text}
                    <div class="comment-actions">
                        <button class="btn btn-like" onclick="likeComment(${comment.id})">❤️</button>
                        <span id="comment-like-count-${comment.id}">0</span>
                        <button class="btn btn-warning" onclick="showReportModal('comment', ${comment.id})">⚠️</button>
                    </div>
                `;
                container.appendChild(div);
                loadCommentLikeCount(comment.id);
            });
        })
        .catch(() => {
            container.innerHTML = "<div class='error-state'>Failed to load comments</div>";
        });
}


// ✅ Like a public comment
function likeComment(commentId) {
    const user = getLoggedUser();
    if (!user) return;

    const payload = {
        userId: user.id,
        commentId
    };

    fetch("/api/Articles/LikePublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error();
            loadCommentLikeCount(commentId);
        })
        .catch(() => alert("Failed to like comment"));
}

// ✅ Load like count for a comment
function loadCommentLikeCount(commentId) {
    fetch(`/api/Articles/PublicCommentLikeCount/${commentId}`)
        .then(res => res.json())
        .then(count => {
            const span = document.getElementById(`comment-like-count-${commentId}`);
            if (span) span.innerText = count;
        });
}
