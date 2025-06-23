document.addEventListener("DOMContentLoaded", () => {
    // Try importing external articles (non-blocking)
    fetch("/api/Articles/ImportExternal", { method: "POST" })
        .finally(loadArticles); // Always load articles from DB
});

function loadArticles() {
    fetch("/api/Articles/All")
        .then(res => {
            if (!res.ok) throw new Error();
            return res.json();
        })
        .then(showArticles)
        .catch(() => {
            document.getElementById("articlesContainer").innerHTML = `
                <div class="alert alert-danger">An error occurred while loading the articles.</div>`;
        });
}

function showArticles(articles) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    articles.reverse();
    for (const article of articles) {
        const image = article.imageUrl
            ? `<img src="${article.imageUrl}" style="max-height:200px;">`
            : "";

        const saveButton = (user && article.id)
            ? `<button onclick="saveArticle(${article.id})">Save</button>`
            : "";

        container.innerHTML += `
            <div>
                ${image}
                <h3>${article.title}</h3>
                <p>${article.description}</p>
                <a href="${article.sourceUrl}" target="_blank">Read Full Article</a>
                ${saveButton}
                <hr/>
            </div>`;
    }
}

function saveArticle(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    if (!user?.id || !articleId) {
        alert("Invalid data. Please log in again and try.");
        return;
    }

    fetch("https://localhost:7084/api/Users/SaveArticle", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, articleId })
    })
        .then(res => {
            if (res.ok) {
                alert("Article saved to favorites.");
            } else {
                alert("Failed to save the article.");
            }
        })
        .catch(() => {
            alert("Network error occurred.");
        });
}
