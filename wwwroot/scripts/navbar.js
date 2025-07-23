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

        // Always show Home
        html += "      <li><a class='nav-link' href='/html/index.html'>Home</a></li>";

        if (user) {
            html += "      <li><a class='nav-link' href='/html/favorites.html'>My Favorites</a></li>";
            html += "      <li><a class='nav-link' href='/html/shared.html'>Articles Inbox</a></li>";
            html += "      <li><a class='nav-link' href='/html/threads.html'>Threads</a></li>";
            html += "      <li><a class='nav-link' href='/html/profile.html'>Profile</a></li>";
            if (user.isAdmin) {
                html += "      <li><a class='nav-link' href='/html/admin.html'>Admin</a></li>";
            }
            html += "      <li><a id='logoutBtn' href='#' class='nav-link logout-link' onclick='logout()'>Logout</a></li>";
        } else {
            html += "      <li><a class='nav-link' href='/html/login.html'>Sign In</a></li>";
        }

        html += "    </ul>";

        // Hamburger Menu
        html += "    <div class='hamburger' onclick='toggleMobileMenu()'>";
        html += "      <span></span><span></span><span></span>";
        html += "    </div>";

        html += "  </div>";
        html += "</nav>";

        // Mobile Menu
        html += "<div class='mobile-menu-overlay' onclick='closeMobileMenu()'></div>";
        html += "<div class='mobile-menu'>";
        html += "  <ul class='mobile-nav'>";
        html += "    <li><a class='nav-link' href='/html/index.html' onclick='closeMobileMenu()'>Home</a></li>";

        if (user) {
            html += "    <li><a class='nav-link' href='/html/favorites.html' onclick='closeMobileMenu()'>My Favorites</a></li>";
            html += "    <li><a class='nav-link' href='/html/shared.html' onclick='closeMobileMenu()'>Articles Inbox</a></li>";
            html += "    <li><a class='nav-link' href='/html/threads.html' onclick='closeMobileMenu()'>Threads</a></li>";
            html += "    <li><a class='nav-link' href='/html/profile.html' onclick='closeMobileMenu()'>Profile</a></li>";
            if (user.isAdmin) {
                html += "    <li><a class='nav-link' href='/html/admin.html' onclick='closeMobileMenu()'>Admin</a></li>";
            }
            html += "    <li><a class='nav-link logout-link' href='#' onclick='logout(); closeMobileMenu()'>Logout</a></li>";
        } else {
            html += "    <li><a class='nav-link' href='/html/login.html' onclick='closeMobileMenu()'>Sign In</a></li>";
        }

        html += "  </ul>";
        html += "</div>";

        container.innerHTML = html;
    }

    bindNavbarEvents();
});

// Mobile Menu Functions
function toggleMobileMenu() {
    const hamburger = document.querySelector('.hamburger');
    const mobileMenu = document.querySelector('.mobile-menu');
    const overlay = document.querySelector('.mobile-menu-overlay');

    hamburger.classList.toggle('active');
    mobileMenu.classList.toggle('active');
    overlay.classList.toggle('active');
    document.body.style.overflow = mobileMenu.classList.contains('active') ? 'hidden' : '';
}

function closeMobileMenu() {
    const hamburger = document.querySelector('.hamburger');
    const mobileMenu = document.querySelector('.mobile-menu');
    const overlay = document.querySelector('.mobile-menu-overlay');

    hamburger.classList.remove('active');
    mobileMenu.classList.remove('active');
    overlay.classList.remove('active');
    document.body.style.overflow = '';
}

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
