document.addEventListener("DOMContentLoaded", () => {
    // נסה לייבא כתבות חיצוניות, אבל אל תעצור אם נכשל
    fetch("/api/News/ImportExternal", {
        method: "POST"
    })
        .then(res => {
            if (!res.ok) throw new Error(`Import failed: ${res.status}`);
            return res.json();
        })
        .then(data => {
            console.log("✅ כתבות חיצוניות יובאו:", data);
        })
        .catch(err => {
            console.warn("⚠️ לא ניתן לייבא כתבות חיצוניות (לא נורא):", err);
        })
        .finally(() => {
            // תמיד נמשיך לטעון מה-DB
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
});



function renderNews(articles) {
    const container = document.getElementById("articlesContainer");
    container.innerHTML = "";

    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    articles.forEach(article => {
        console.log("🔍 כתבה:", article);

        // בדיקה שהכתבה כוללת ID תקני
        if (!article.id || article.id === 0) {
            console.warn("⚠️ כתבה עם ID חסר או שגוי:", article);
        }

        container.innerHTML += `
        <div>
            ${article.imageUrl ? `<img src="${article.imageUrl}" style="max-height:200px;">` : ""}
            <h3>${article.title}</h3>
            <p>${article.description}</p>
            <a href="${article.sourceUrl}" target="_blank">לכתבה המלאה</a>
            ${user && article.id
                ? `<button onclick="saveArticle(${article.id})">💾 שמור</button>`
                : ""
            }
            <hr/>
        </div>`;
    });
}

function saveArticle(articleId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    // בדיקת תקינות נתונים לפני שליחה
    if (!user || !user.id || !articleId) {
        console.error("❌ נתונים חסרים:", { user, articleId });
        alert("⚠️ נתונים לא תקינים. התחבר מחדש ונסה שוב.");
        return;
    }

    const data = {
        userId: user.id,
        articleId: articleId
    };

    console.log("📤 שולח שמירת כתבה:", data);

    fetch("https://localhost:7084/api/Users/SaveArticle", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(data)
    })
        .then(response => {
            if (response.ok) {
                alert("✅ הכתבה נשמרה למועדפים");
            } else {
                console.error("❌ שגיאה בשמירה:", response.status);
                alert("⚠️ לא ניתן לשמור את הכתבה");
            }
        })
        .catch(err => {
            console.error("⚠️ שגיאה ברשת:", err);
            alert("⚠️ שגיאה כללית בשליחה");
        });
}
