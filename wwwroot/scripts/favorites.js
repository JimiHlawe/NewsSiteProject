let allSavedArticles = [];

document.addEventListener("DOMContentLoaded", () => {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user?.id) return;

    fetch(`https://localhost:7084/api/Users/GetSavedArticles/${user.id}`)
        .then(res => res.json())
        .then(data => {
            allSavedArticles = data;
            renderSavedArticles(data);
        })
        .catch(() => {
            document.getElementById("savedArticlesContainer").innerHTML =
                `<div class="alert alert-danger">Error loading saved articles</div>`;
        });

    document.getElementById("searchForm").addEventListener("submit", (e) => {
        e.preventDefault();
        filterArticles();
    });
});

function filterArticles() {
    const title = document.getElementById("searchTitle").value.toLowerCase();
    const from = document.getElementById("searchFrom").value;
    const to = document.getElementById("searchTo").value;

    const filtered = allSavedArticles.filter(article => {
        const matchTitle = article.title?.toLowerCase().includes(title);
        const published = new Date(article.publishedAt);
        const matchFrom = !from || published >= new Date(from);
        const matchTo = !to || published <= new Date(to);
        return matchTitle && matchFrom && matchTo;
    });

    renderSavedArticles(filtered);
}

function renderSavedArticles(articles) {
    const container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = `<div class="alert alert-warning">No articles found.</div>`;
        return;
    }

    for (const article of articles) {
        container.innerHTML += `
            <div class="col-md-4">
                <div class="card">
                    <img src="${article.imageUrl}" class="card-img-top" alt="Article Image">
                    <div class="card-body">
                        <h5 class="card-title">${article.title}</h5>
                        <p class="card-text">${article.description}</p>
                        <a href="${article.sourceUrl}" target="_blank" class="btn btn-primary">Read</a>
                    </div>
                </div>
            </div>
        `;
    }
}
