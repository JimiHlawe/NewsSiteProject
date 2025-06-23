// navbar.js
document.addEventListener("DOMContentLoaded", () => {
    const container = document.getElementById("navbar");

    if (container) {
        container.innerHTML = `
            <nav class="navbar navbar-expand-lg navbar-light bg-light px-3">
                <a class="navbar-brand" href="/html/index.html">News Portal</a>
                <div class="collapse navbar-collapse">
                    <ul class="navbar-nav me-auto">
                        <li class="nav-item"><a class="nav-link" href="/html/index.html">Home</a></li>
                        <li class="nav-item"><a class="nav-link" href="/html/favorites.html">Saved Articles</a></li>
                        <li class="nav-item"><a class="nav-link" href="/html/shared.html">Shared Articles</a></li>
                        <li class="nav-item"><a class="nav-link" href="/html/profile.html">Profile</a></li>
                    </ul>
                    <button class="btn btn-outline-danger" onclick="logout()">Logout</button>
                </div>
            </nav>
        `;
    }

    const userJson = sessionStorage.getItem("loggedUser"); 
    const user = userJson ? JSON.parse(userJson) : null;

    const path = window.location.pathname;
    const isLoginPage = path.endsWith("login.html") || path.endsWith("register.html");

    if (!user && !isLoginPage) {
        window.location.href = "/html/login.html";
    }
});

function logout() {
    sessionStorage.clear();
    window.location.href = "/html/login.html";
}
