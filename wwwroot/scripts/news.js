document.addEventListener("DOMContentLoaded", () => {
    fetch("/api/News")


        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP Error: ${response.status}`);
            }
            return response.json();
        })
        .then(data => renderNews(data))
        .catch(err => {
            console.error("שגיאה בטעינת חדשות:", err);
            const container = document.getElementById("articlesContainer");
            container.innerHTML = `<div class="alert alert-danger">אירעה שגיאה בטעינת חדשות</div>`;
        });
});

function renderNews(articles) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    articles.forEach(article => {
        container.innerHTML += `
        <div class="col-md-4">
            <div class="card h-100">
                ${article.imageUrl ? `<img src="${article.imageUrl}" class="card-img-top" style="max-height: 200px; object-fit: cover;">` : ""}
                <div class="card-body d-flex flex-column">
                    <h5 class="card-title">${article.title}</h5>
                    <p class="card-text">${article.description}</p>
                    <a href="${article.sourceUrl}" target="_blank" class="mt-auto btn btn-primary">לכתבה המלאה</a>
                </div>
            </div>
        </div>`;
    });
}
