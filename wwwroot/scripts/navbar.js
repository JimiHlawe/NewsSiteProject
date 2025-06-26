document.addEventListener("DOMContentLoaded", function () {
    var container = document.getElementById("navbar");

    if (container) {
        var html = "";
        html += "<nav class='navbar navbar-expand-lg navbar-light bg-light px-3'>";
        html += "  <a class='navbar-brand' href='/html/index.html'>News Portal</a>";
        html += "  <div class='collapse navbar-collapse'>";
        html += "    <ul class='navbar-nav me-auto'>";
        html += "      <li class='nav-item'><a class='nav-link' href='/html/index.html'>Home</a></li>";
        html += "      <li class='nav-item'><a class='nav-link' href='/html/favorites.html'>Saved Articles</a></li>";
        html += "      <li class='nav-item'><a class='nav-link' href='/html/shared.html'>Shared Articles</a></li>";
        html += "      <li class='nav-item'><a class='nav-link' href='/html/public.html'>Public Shared</a></li>";
        html += "      <li class='nav-item'><a class='nav-link' href='/html/profile.html'>Profile</a></li>";
        html += "    </ul>";
        html += "    <button class='btn btn-outline-danger' onclick='logout()'>Logout</button>";
        html += "  </div>";
        html += "</nav>";

        container.innerHTML = html;
    }

    var userJson = sessionStorage.getItem("loggedUser");
    var user = userJson ? JSON.parse(userJson) : null;

    var path = window.location.pathname;
    var isLoginPage = path.indexOf("login.html") !== -1 || path.indexOf("register.html") !== -1;

    if (!user && !isLoginPage) {
        window.location.href = "/html/login.html";
    }
});

function logout() {
    sessionStorage.clear();
    window.location.href = "/html/login.html";
}
