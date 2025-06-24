document.addEventListener("DOMContentLoaded", () => {
    const userJson = sessionStorage.getItem("loggedUser");
    if (!userJson) {
        window.location.href = "/html/login.html";
        return;
    }

    const user = JSON.parse(userJson);
    fetch(`https://localhost:7084/api/Articles/SharedWithMe/${user.id}`)
        .then(res => {
            if (!res.ok) throw new Error("Failed to fetch shared articles");
            return res.json();
        })
        .then(renderSharedArticles)
        .catch(() => {
            document.getElementById("sharedContainer").innerHTML = `
                <div class="alert alert-danger">⚠️ Error loading shared articles</div>`;
        });
});

function renderSharedArticles(articles) {
    const container = document.getElementById("sharedContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = `<p>No articles shared with you yet.</p>`;
        return;
    }

    for (const article of articles) {
        const image = article.imageUrl
            ? `<img src="${article.imageUrl}" style="max-height:200px;" class="mb-2">`
            : "";

        container.innerHTML += `
            <div class="card mb-3 p-3">
                ${image}
                <h4>${article.title}</h4>
                <p>${article.description || ""}</p>
                <p><strong>Shared by:</strong> ${article.senderName}</p>
                <p><strong>Comment:</strong> ${article.comment || "No comment"}</p>
                <a href="${article.sourceUrl}" target="_blank" class="btn btn-primary btn-sm mt-2">Read Full Article</a>
            </div>`;
    }
}
