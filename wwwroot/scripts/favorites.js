document.addEventListener("DOMContentLoaded", () => {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user) {
        document.getElementById("savedArticlesContainer").innerHTML = `
            <div class="alert alert-warning text-center">
                You must be logged in to view your saved articles.
            </div>`;
        return;
    }

    fetch(`https://localhost:7084/api/Users/GetSavedArticles/${user.id}`)
        .then(response => response.json())
        .then(articles => renderSavedArticles(articles))
        .catch(() => {
            document.getElementById("savedArticlesContainer").innerHTML = `
                <div class="alert alert-danger text-center">An error occurred while loading saved articles.</div>`;
        });
});

function renderSavedArticles(articles) {
    const container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = `
            <div class="alert alert-info text-center">You haven't saved any articles yet.</div>`;
        return;
    }

    articles.forEach(article => {
        container.innerHTML += `
            <div class="col-md-4">
                <div class="card h-100">
                    ${article.imageUrl ? `<img src="${article.imageUrl}" class="card-img-top" style="max-height:200px; object-fit:cover;">` : ""}
                    <div class="card-body d-flex flex-column">
                        <h5 class="card-title">${article.title}</h5>
                        <p class="card-text">${article.description}</p>
                        <a href="${article.sourceUrl}" target="_blank" class="btn btn-primary mt-auto">Read Full Article</a>
                    </div>
                </div>
            </div>`;
    });
}
