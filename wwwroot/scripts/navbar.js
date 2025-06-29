﻿document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("navbar");

    if (container) {
        let html = "";
        html += "<nav class='navbar'>";
        html += "  <div class='container'>";
        html += "    <a class='navbar-brand' href='/html/index.html'>NEWSPAPER</a>";
        html += "    <ul class='navbar-nav'>";
        html += "      <li><a class='nav-link' href='/html/index.html'>Home</a></li>";
        html += "      <li><a class='nav-link' href='/html/favorites.html'>Saved Articles</a></li>";
        html += "      <li><a class='nav-link' href='/html/shared.html'>Shared Articles</a></li>";
        html += "      <li><a class='nav-link' href='/html/public.html'>Public Shared</a></li>";
        html += "      <li><a class='nav-link' href='/html/profile.html'>Profile</a></li>";
        html += "    </ul>";
        html += "    <select class='language-selector'>";
        html += "      <option>English</option>";
        html += "      <option>עברית</option>";
        html += "    </select>";
        html += "    <a id='logoutBtn' href='#' class='nav-link logout-link' onclick='logout()'>Logout</a>";
        html += "  </div>";
        html += "</nav>";

        container.innerHTML = html;
    }

    const userJson = sessionStorage.getItem("loggedUser");
    const user = userJson ? JSON.parse(userJson) : null;

    const path = window.location.pathname;
    const isLoginPage = path.includes("login.html") || path.includes("register.html");

    if (!user && !isLoginPage) {
        window.location.href = "/html/login.html";
    }
});

function logout() {
    sessionStorage.clear();
    window.location.href = "/html/login.html";
}
