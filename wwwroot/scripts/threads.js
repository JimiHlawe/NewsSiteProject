document.addEventListener("DOMContentLoaded", function () {
    loadThreadsArticles();
});

function loadThreadsArticles() {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch("/api/Articles/Public/" + user.id)
        .then(function (res) {
            if (!res.ok) throw new Error("Failed to fetch threads");
            return res.json();
        })
        .then(function (data) {
            console.log("📦 Threads from DB:", data);
            renderThreadsArticles(data);
        })
        .catch(function (err) {
            console.error("Failed to load threads:", err);
            showError("threadsContainer", "Failed to load threads");
        });
}

function renderThreadsArticles(articles) {
    var container = document.getElementById("threadsContainer");
    container.innerHTML = "";

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];
        var id = article.publicArticleId; 
        var card = createThreadCard(article);
        container.appendChild(card);

        loadComments(id);
    }
}

function createThreadCard(article) {
    var id = article.publicArticleId;
    var div = document.createElement("div");
    div.className = "thread-card p-3 mb-4 border rounded bg-light";
    div.style.cursor = 'pointer';

    var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    // ✅ יצירת HTML לתגיות על התמונה
    var tagsHtml = (article.tags && article.tags.length > 0)
        ? article.tags.map(tag => `<span class="tag">${tag}</span>`).join(" ")
        : "";

    var html = `
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
            <button class='btn btn-sm btn-outline-primary' id="like-thread-btn-${article.publicArticleId}"></button>
            <span id="like-thread-count-${article.publicArticleId}" class="ms-2">0</span>
            <button class='btn btn-sm btn-success share-btn' onclick="showThreadShareModal(${article.publicArticleId}); event.stopPropagation();">
                <img src="../pictures/send.png" alt="Share" class="share-icon">
            </button>
            <button class='btn btn-sm btn-info comment-btn' onclick="showCommentsModal(${id}); event.stopPropagation();">
                <img src="../pictures/comment1.png" alt="Comment" class="share-icon">
            </button>
            <div class="three-dots-menu" onclick="showThreadOptionsMenu(${article.publicArticleId}, '${article.senderName}', ${id}); event.stopPropagation();">
                ⋯
                <div class="thread-options-menu" id="options-menu-${article.publicArticleId}">
                    <div class="thread-options-content">
                        <button onclick="blockUser('${article.senderName}'); event.stopPropagation();">
                            🚫 Block ${article.senderName}
                        </button>
                        <button onclick="reportArticle(${id}); event.stopPropagation();">
                            🚨 Report Article
                        </button>
                        <button onclick="showThreadShareModal(${article.publicArticleId}); event.stopPropagation();">
                            📤 Share
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <button class='btn btn-sm btn-danger mb-2' onclick="blockUser('${article.senderName}'); event.stopPropagation();" style="display: none;">Block ${article.senderName}</button>
        <button class='btn btn-sm btn-warning mb-2' onclick="reportArticle(${id}); event.stopPropagation();" style="display: none;">Report Article</button>
        <button class='btn btn-sm btn-success mb-2' onclick="showThreadShareModal(${article.publicArticleId}); event.stopPropagation();" style="display: none;">Share</button>

        <h6>💬 Comments:</h6>
        <div id="comments-${id}" onclick="event.stopPropagation();" style="display: none;"></div>
        <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();" style="display: none;"></textarea>
        <button class='btn btn-sm btn-primary' onclick='sendComment(${id}); event.stopPropagation();' style="display: none;">Send</button>
    </div>
    `;

    div.innerHTML = html;

    loadThreadLikeCount(article.publicArticleId);

    var likeBtn = div.querySelector(`#like-thread-btn-${article.publicArticleId}`);
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

// פונקציה לפתיחת תפריט אופציות
function showThreadOptionsMenu(articleId, senderName, id) {
    // סגירת כל התפריטים הפתוחים
    document.querySelectorAll('.thread-options-menu').forEach(menu => {
        menu.classList.remove('show');
    });

    // פתיחת התפריט הנוכחי
    const menu = document.getElementById(`options-menu-${articleId}`);
    if (menu) {
        menu.classList.add('show');

        // סגירה בלחיצה מחוץ לתפריט
        document.addEventListener('click', function closeMenu(e) {
            if (!menu.contains(e.target)) {
                menu.classList.remove('show');
                document.removeEventListener('click', closeMenu);
            }
        });
    }
}


// פונקציה חדשה להצגת תגובות במודאל
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

    // טען תגובות למודאל
    loadCommentsForModal(articleId);

    // הצג את המודאל
    setTimeout(() => {
        document.getElementById("commentsModal").classList.add("show");
    }, 10);

    // סגירה בלחיצה מחוץ למודאל
    setTimeout(() => {
        document.addEventListener('click', closeCommentsModalOnOutsideClick);
    }, 100);
}
function loadCommentsForModal(articleId) {
    fetch("/api/Articles/GetPublicComments/" + articleId)
        .then(res => res.json())
        .then(comments => {
            var container = document.getElementById("modal-comments-" + articleId);
            container.innerHTML = "";

            for (var i = 0; i < comments.length; i++) {
                var c = comments[i];

                var commentDiv = document.createElement('div');
                commentDiv.className = 'border rounded p-2 mb-1';
                commentDiv.innerHTML = `
                    <strong>${c.username}</strong>: ${c.comment}
                    <button onclick="togglePublicCommentLike(${c.id})">👍 Like</button>
                    <span id="public-like-count-${c.id}">0</span>
                    <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id}); event.stopPropagation();'>Report</button>
                `;

                container.appendChild(commentDiv);
                updatePublicLikeCount(c.id);
            }
        })
        .catch(() => {
            console.error("Error loading comments for modal");
        });
}


function sendCommentFromModal(articleId) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    var commentInput = document.getElementById("modal-commentBox-" + articleId);
    var commentText = commentInput.value.trim();

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }

    var payload = {
        publicArticleId: articleId,
        userId: user.id,
        comment: commentText
    };

    console.log("📤 Sending comment from modal:", payload);

    fetch("/api/Articles/AddPublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(function (res) {
            if (!res.ok) throw new Error("HTTP " + res.status);
            commentInput.value = "";
            loadCommentsForModal(articleId);
            loadComments(articleId); // עדכן גם את התגובות בכרטיס
        })
        .catch(function (err) {
            console.error("💥 Error posting comment from modal", err);
            alert("Error posting comment");
        });
}

function closeCommentsModal() {
    const modal = document.getElementById('commentsModal');
    if (modal) {
        modal.classList.remove('show');
        setTimeout(() => modal.remove(), 300);
    }
    document.removeEventListener('click', closeCommentsModalOnOutsideClick);
}

function closeCommentsModalOnOutsideClick(event) {
    const modal = document.getElementById('commentsModal');
    const modalContent = modal?.querySelector('.comments-modal-content');
    if (modal && !modalContent?.contains(event.target)) {
        closeCommentsModal();
    }
}

// פונקציה חדשה להצגת תפריט האפשרויות (רק report ו-block עכשיו)
function showThreadOptionsMenu(publicArticleId, senderName, articleId) {
    const existingMenu = document.getElementById('threadOptionsMenu');
    if (existingMenu) existingMenu.remove();

    const menuHTML = `
        <div class="thread-options-menu" id="threadOptionsMenu">
            <div class="thread-options-content">
                <button onclick="reportArticle(${articleId}); closeThreadOptionsMenu();">
                     Report Article
                </button>
                <button onclick="blockUser('${senderName}'); closeThreadOptionsMenu();">
                    Block ${senderName}
                </button>
            </div>
        </div>
    `;

    document.body.insertAdjacentHTML("beforeend", menuHTML);

    setTimeout(() => {
        document.getElementById("threadOptionsMenu").classList.add("show");
    }, 10);

    // סגירה בלחיצה מחוץ לתפריט
    setTimeout(() => {
        document.addEventListener('click', closeThreadOptionsMenuOnOutsideClick);
    }, 100);
}

function closeThreadOptionsMenu() {
    const menu = document.getElementById('threadOptionsMenu');
    if (menu) {
        menu.classList.remove('show');
        setTimeout(() => menu.remove(), 300);
    }
    document.removeEventListener('click', closeThreadOptionsMenuOnOutsideClick);
}

function closeThreadOptionsMenuOnOutsideClick(event) {
    const menu = document.getElementById('threadOptionsMenu');
    if (menu && !menu.contains(event.target) && !event.target.classList.contains('three-dots-menu')) {
        closeThreadOptionsMenu();
    }
}

function loadThreadLikeCount(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    // טען את מספר הלייקים
    fetch(`/api/Articles/GetThreadLikeCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            const likeCountSpan = document.getElementById(`like-thread-count-${articleId}`);
            if (likeCountSpan) {
                likeCountSpan.textContent = `${count} ❤️`;
            }
        })
        .catch(err => {
            console.error("Failed to fetch like count:", err);
        });

    // בדוק אם המשתמש כבר עשה לייק
    if (user?.id) {
        fetch(`/api/Articles/CheckUserLike/${articleId}/${user.id}`)
            .then(res => res.json())
            .then(hasLiked => {
                const likeBtn = document.getElementById(`like-thread-btn-${articleId}`);
                if (likeBtn) {
                    if (hasLiked) {
                        likeBtn.classList.add('liked');
                    } else {
                        likeBtn.classList.remove('liked');
                    }
                }
            })
            .catch(err => {
                console.error("Failed to check user like status:", err);
            });
    }
}

function blockUser(senderName) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
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

function sendComment(articleId) {
    var user = JSON.parse(sessionStorage.getItem("loggedUser"));
    var commentInput = document.getElementById("commentBox-" + articleId);
    var commentText = commentInput.value.trim();

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }

    var payload = {
        publicArticleId: articleId,
        userId: user.id,
        comment: commentText
    };

    console.log("📤 Sending comment:", payload);

    fetch("/api/Articles/AddPublicComment", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(function (res) {
            if (!res.ok) throw new Error("HTTP " + res.status);
            commentInput.value = "";
            loadComments(articleId);
        })
        .catch(function (err) {
            console.error("💥 Error posting comment", err);
            alert("Error posting comment");
        });
}

function loadComments(articleId) {
    fetch("/api/Articles/GetPublicComments/" + articleId)
        .then(function (res) {
            return res.json();
        })
        .then(function (comments) {
            var container = document.getElementById("comments-" + articleId);
            container.innerHTML = "";

            for (var i = 0; i < comments.length; i++) {
                var c = comments[i];
                var commentDiv = document.createElement('div');
                commentDiv.className = 'border rounded p-2 mb-1';
                commentDiv.innerHTML = `
    <strong>${c.username}</strong>: ${c.comment}
    <button class='btn btn-sm btn-warning ms-2' onclick='reportComment(${c.id}); event.stopPropagation();'>Report</button>
`;

                // מניעת קליק על התגובה עצמה
                commentDiv.addEventListener('click', function (event) {
                    event.stopPropagation();
                });

                container.appendChild(commentDiv);
            }
        })
        .catch(function () {
            console.error("Error loading comments");
        });
}

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

function toggleOtherReason() {
    const select = document.getElementById("reportReasonSelect");
    const otherInput = document.getElementById("reportOtherReason");
    otherInput.style.display = select.value === "other" ? "block" : "none";
}

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

function showError(containerId, message) {
    var container = document.getElementById(containerId);
    container.innerHTML = "<div class='alert alert-danger'>" + message + "</div>";
}

function showThreadShareModal(threadId) {
    const modalHTML = `
        <div class="share-modal-overlay" id="shareModalOverlay">
            <div class="share-modal">
                <div class="share-modal-icon"></div>
                <h2 class="share-modal-title">Share Thread</h2>
                <p class="share-modal-subtitle">Send this thread to a specific user</p>

                <form class="share-modal-form" id="threadShareForm">
                    <div class="form-group">
                        <label for="targetUser">Username</label>
                        <input type="text" id="targetUser" placeholder="Enter username" required />
                    </div>

                    <div class="form-group">
                        <label for="shareComment">Comment (optional)</label>
                        <textarea id="shareComment" placeholder="Add a message..."></textarea>
                    </div>
                </form>

                <div class="share-modal-buttons">
                    <button class="share-modal-button secondary" onclick="closeShareModal()">Cancel</button>
                    <button class="share-modal-button primary" onclick="submitThreadShare(${threadId})">Share</button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML("beforeend", modalHTML);
    setTimeout(() => {
        document.getElementById("shareModalOverlay").classList.add("show");
    }, 100);
}

function submitThreadShare(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const toUsername = document.getElementById('targetUser').value.trim();
    const comment = document.getElementById('shareComment').value.trim();

    fetch("/api/Articles/ShareThreadToUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            senderUsername: user.name,
            toUsername: toUsername,
            publicArticleId: articleId,
            comment: comment
        })
    })
        .then(res => {
            if (res.ok) {
                closeShareModal();
                showShareSuccessModal();
            } else {
                alert("❌ Failed to share thread.");
            }
        })
}

function closeShareModal() {
    const overlay = document.getElementById("shareModalOverlay");
    if (overlay) {
        overlay.classList.add("hide");
        setTimeout(() => overlay.remove(), 500);
    }
}

function showShareSuccessModal() {
    const modalHTML = `
        <div class="save-modal-overlay" id="shareSuccessOverlay">
            <div class="save-modal">
                <div class="save-modal-icon"></div>
                <h2 class="save-modal-title">Thread Shared!</h2>
                <p class="save-modal-subtitle">The thread was successfully sent</p>
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

function closeShareSuccessModal() {
    const overlay = document.getElementById('shareSuccessOverlay');
    if (overlay) {
        overlay.classList.add('hide');
        setTimeout(() => overlay.remove(), 600);
    }
}

document.addEventListener('click', function (e) {
    if (e.target && e.target.classList.contains('share-modal-overlay')) {
        closeShareModal();
    }
});

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

    fetch("/api/Articles/ToggleThreadLike", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    })
        .then(res => {
            if (!res.ok) throw new Error("Failed to toggle like");

            // שינוי מצב הכפתור ויזואלית
            if (likeBtn) {
                likeBtn.classList.toggle('liked');
            }

            // לאחר שינוי הלייק נרענן את מספר הלייקים
            loadThreadLikeCount(article.publicArticleId);
        })
        .catch(err => {
            console.error("❌ Failed to toggle like:", err);
        });
}

function togglePublicCommentLike(publicCommentId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    fetch('/api/Articles/TogglePublicCommentLike', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.id, publicCommentId })
    }).then(() => updatePublicLikeCount(publicCommentId));
}

function updatePublicLikeCount(publicCommentId) {
    fetch(`/api/Articles/PublicCommentLikeCount/${publicCommentId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`public-like-count-${publicCommentId}`).innerText = count;
        });
}
