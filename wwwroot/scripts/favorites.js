document.addEventListener("DOMContentLoaded", () => {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user) {
        document.getElementById("savedArticlesContainer").innerHTML = `
            <div class="alert alert-warning text-center">
                עליך להתחבר כדי לצפות במועדפים שלך
            </div>`;
        return;
    }

    fetch(`https://localhost:7084/api/Users/GetSavedArticles/${user.id}`)
        .then(response => response.json())
        .then(articles => renderSavedArticles(articles))
        .catch(err => {
            console.error("שגיאה בטעינת מועדפים:", err);
            document.getElementById("savedArticlesContainer").innerHTML = `
                <div class="alert alert-danger text-center">אירעה שגיאה בטעינת הכתבות</div>`;
        });
});

function renderSavedArticles(articles) {
    const container = document.getElementById("savedArticlesContainer");
    container.innerHTML = "";

    if (articles.length === 0) {
        container.innerHTML = `
            <div class="alert alert-info text-center">לא שמרת עדיין כתבות</div>`;
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
                        <a href="${article.sourceUrl}" target="_blank" class="btn btn-primary mt-auto">לכתבה המלאה</a>
                    </div>
                </div>
            </div>`;
    });
}
