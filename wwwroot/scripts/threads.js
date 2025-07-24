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
        var id = article.articleId;
        var card = createThreadCard(article);
        container.appendChild(card);

        loadComments(id);
    }
}
function createThreadCard(article) {
    var id = article.articleId;
    var div = document.createElement("div");
    div.className = "thread-card p-3 mb-4 border rounded bg-light";
    div.style.cursor = 'pointer';

    var formattedDate = new Date(article.publishedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    var html = `
    <div class="initial-comment border-bottom pb-2 mb-3">
        <strong>${article.senderName}</strong> wrote:
        <p class="mb-0"><em>${article.initialComment || ""}</em></p>
    </div>

    <div class="thread-content">
        <div class="thread-image mb-2">
            <img src="${article.imageUrl || 'https://via.placeholder.com/800x400'}" class="img-fluid rounded">
        </div>
        <h5>${article.title}</h5>
        <p>${article.description || ""}</p>

        <div class="thread-meta mb-2">
            <strong>Author:</strong> ${article.author || 'Unknown'} |
            <strong>Date:</strong> ${formattedDate}
        </div>
        <div class="thread-actions mb-2">
            <button class='btn btn-sm btn-outline-primary' id="like-thread-btn-${article.publicArticleId}">
                ❤️ Like
            </button>
            <span id="like-thread-count-${article.publicArticleId}" class="ms-2">0 ❤️</span>
        </div>

        <button class='btn btn-sm btn-danger mb-2' onclick="blockUser('${article.senderName}'); event.stopPropagation();">Block ${article.senderName}</button>
        <button class='btn btn-sm btn-warning mb-2' onclick="reportArticle(${id}); event.stopPropagation();">Report Article</button>
        <button class='btn btn-sm btn-success mb-2' onclick="showThreadShareModal(${article.publicArticleId}); event.stopPropagation();">Share</button>

        <h6>💬 Comments:</h6>
        <div id="comments-${id}" onclick="event.stopPropagation();"></div>
        <textarea id="commentBox-${id}" class="form-control mb-2" placeholder="Write a comment..." onclick="event.stopPropagation();"></textarea>
        <button class='btn btn-sm btn-primary' onclick='sendComment(${id}); event.stopPropagation();'>Send</button>
    </div>
`;


    div.innerHTML = html;

    // הפעלת toggleThreadLike עם article המלא
    var likeBtn = div.querySelector(`#like-thread-btn-${article.publicArticleId}`);
    if (likeBtn) {
        likeBtn.onclick = function (event) {
            event.stopPropagation();
            toggleThreadLike(article);
        };
    }

    // פתיחת מקור הכתבה בלחיצה על הכרטיס
    div.addEventListener('click', function () {
        if (article.sourceUrl && article.sourceUrl !== '#') {
            window.open(article.sourceUrl, '_blank');
        } else {
            alert('No article URL available');
        }
    });

    return div;
}

function toggleThreadLike(article) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) {
        alert("Please login first.");
        return;
    }

    const btn = document.getElementById(`like-thread-btn-${article.publicArticleId}`);
    if (!btn) {
        console.error("❌ Like button not found for article:", article);
        return;
    }

    const isLiked = btn.classList.contains("liked");
    const endpoint = isLiked ? "RemoveThreadLike" : "AddThreadLike";

    fetch(`/api/Articles/${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            userId: user.id,
            publicArticleId: article.publicArticleId // ✅ תיקון כאן
        })
    })
        .then(res => {
            if (res.ok) {
                btn.classList.toggle("liked");
                loadThreadLikeCount(article.publicArticleId);
            } else {
                alert("❌ Failed to toggle like");
            }
        })
        .catch(err => {
            console.error("Error toggling thread like:", err);
            alert("❌ Network error");
        });
}







function loadThreadLikeCount(articleId) {
    fetch(`/api/Articles/GetThreadLikeCount/${articleId}`)
        .then(res => res.json())
        .then(count => {
            document.getElementById(`like-thread-count-${articleId}`).innerText = `${count} ❤️`;
        })
        .catch(err => {
            console.error("Error loading like count:", err);
        });
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