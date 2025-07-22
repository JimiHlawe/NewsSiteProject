document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("navbar");

    const userJson = sessionStorage.getItem("loggedUser");
    const user = userJson ? JSON.parse(userJson) : null;

    if (container) {
        let html = "";
        html += "<nav class='navbar'>";
        html += "  <div class='container'>";
        html += "    <a class='navbar-brand' href='/html/index.html'>NEWSPAPER</a>";
        html += "    <ul class='navbar-nav'>";
        html += "      <li><a class='nav-link' href='/html/index.html'>Home</a></li>";
        html += "      <li><a class='nav-link' href='/html/favorites.html'>My Favorites</a></li>";
        html += "      <li><a class='nav-link' href='/html/shared.html'>Articles Inbox</a></li>";
        html += "      <li><a class='nav-link' href='/html/threads.html'>Threads</a></li>";
        html += "      <li><a class='nav-link' href='/html/profile.html'>Profile</a></li>";

        if (user && user.isAdmin) {
            html += "      <li><a class='nav-link' href='/html/admin.html'>Admin</a></li>";
        }

        html += "    </ul>";
        html += "    <a id='logoutBtn' href='#' class='nav-link logout-link' onclick='logout()'>Logout</a>";
        html += "  </div>";
        html += "</nav>";

        container.innerHTML = html;
    }

    const path = window.location.pathname;
    const isLoginPage = path.includes("login.html") || path.includes("register.html");

    if (!user && !isLoginPage) {
        window.location.href = "/html/login.html";
    }
});

function bindNavbarEvents() {
    const logoutBtn = document.getElementById("logoutBtn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", logout);
    }
}

function logout() {
    sessionStorage.clear();
    window.location.href = "/html/login.html";
}