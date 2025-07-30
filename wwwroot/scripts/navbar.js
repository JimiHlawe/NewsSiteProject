document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("navbar");
    const userJson = sessionStorage.getItem("loggedUser");
    const user = userJson ? JSON.parse(userJson) : null;

    if (!container) return;

    if (user) {
        // נטען את המשתמש העדכני מהשרת ונעדכן את sessionStorage
        fetch(`/api/Users/GetUserById/${user.id}`)
            .then(res => res.json())
            .then(updatedUser => {
                sessionStorage.setItem("loggedUser", JSON.stringify(updatedUser));
                renderNavbarWithUser(container, updatedUser);
                updateInboxNotification(); // ✅ עדכון ההתראה של ה-Inbox
            })
            .catch(err => {
                console.error("Failed to fetch updated user:", err);
                renderNavbarWithUser(container, user); // אם נכשלה הקריאה – נטען מה-session
                updateInboxNotification();
            });
    } else {
        renderNavbarWithUser(container, null);
    }

    bindNavbarEvents();
});

function renderNavbarWithUser(container, user) {
    let html = "";
    html += "<nav class='navbar'>";
    html += "  <div class='container'>";
    html += "    <a class='navbar-brand' href='/html/index.html'>NEWSPAPER</a>";
    html += "    <ul class='navbar-nav'>";

    if (user) {
        const profileImage = user.profileImagePath || "../pictures/default-avatar.jpg";
        const avatarIcons = {
            "BRONZE": "../pictures/avatar_bronze.png",
            "SILVER": "../pictures/avatar_silver.png",
            "GOLD": "../pictures/avatar_gold.png"
        };
        const avatarIcon = avatarIcons[user.avatarLevel || "BRONZE"];

        html += "      <li><a class='nav-link' href='/html/favorites.html'>My Favorites</a></li>";
        html += "      <li><a class='nav-link' href='/html/shared.html' id='inboxNavItem'>Inbox</a></li>";
        html += "      <li><a class='nav-link' href='/html/threads.html'>Threads</a></li>";

        if (user.isAdmin) {
            html += "      <li><a class='nav-link' href='/html/admin.html'>Admin</a></li>";
        }

        html += `
            <li class='nav-profile-image'>
                <a href="/html/profile.html">
                    <img src="${profileImage}" alt="Profile" class="profile-img-nav">
                    <img src="${avatarIcon}" alt="Rank" class="avatar-rank-icon">
                </a>
            </li>
        `;

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
        const profileImage = user.profileImagePath || "../pictures/default-avatar.png";

        html += "    <li><a class='nav-link' href='/html/favorites.html' onclick='closeMobileMenu()'>My Favorites</a></li>";
        html += "    <li><a class='nav-link' href='/html/shared.html' onclick='closeMobileMenu()'>Inbox</a></li>";
        html += "    <li><a class='nav-link' href='/html/threads.html' onclick='closeMobileMenu()'>Threads</a></li>";
        html += "    <li><a class='nav-link' href='/html/profile.html' onclick='closeMobileMenu()'>Profile</a></li>";

        html += `    <li class='nav-profile-image'>
                        <a href="/html/profile.html" onclick='closeMobileMenu()'>
                            <img src="${profileImage}" alt="Profile" class="profile-img-nav">
                        </a>
                     </li>`;

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

function bindNavbarEvents() {
    const logoutBtn = document.getElementById("logoutBtn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", logout);
    }
}

function logout() {
    sessionStorage.clear();
    window.location.href = "/html/index.html";
}

function updateInboxNotification() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user) return;

    fetch(`/api/Articles/UnreadCount/${user.id}`)
        .then(res => res.json())
        .then(count => {
            const inboxItem = document.getElementById("inboxNavItem");
            if (inboxItem) {
                inboxItem.innerHTML = `Inbox ${count > 0 ? `<span class="inbox-badge">${count}</span>` : ''}`;
            }
        });
}
