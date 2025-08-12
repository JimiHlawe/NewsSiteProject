// ✅ Firebase Imports
import { db } from './firebase-config.js';
import { ref, onValue } from "https://www.gstatic.com/firebasejs/9.0.0/firebase-database.js";

// --- API BASE ---
const API_BASE = location.hostname.includes("localhost")
    ? "https://localhost:7084/api"
    : "https://proj.ruppin.ac.il/cgroup13/test2/tar1/api";

// ✅ On page load – build navbar and load user data
document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("navbar");
    const userJson = sessionStorage.getItem("loggedUser");
    const user = userJson ? JSON.parse(userJson) : null;

    if (!container) return;

    if (user) {
        fetch(`${API_BASE}/Users/GetUserById/${user.id}`)
            .then(res => res.json())
            .then(updatedUser => {
                sessionStorage.setItem("loggedUser", JSON.stringify(updatedUser));
                renderNavbarWithUser(container, updatedUser);
                listenToInboxCount(updatedUser.id);
            })
            .catch(() => {
                renderNavbarWithUser(container, user);
                listenToInboxCount(user.id);
            });
    } else {
        renderNavbarWithUser(container, null);
    }

    bindNavbarEvents();
});

// ✅ Render navbar content based on user state (logged in or not)
function renderNavbarWithUser(container, user) {
    let html = "";

    html += `
    <nav class="navbar">
        <div class="container">
            <a class="navbar-brand" href="/html/index.html">NEWSPAPER</a>
            <ul class="navbar-nav">
    `;

    if (user) {
        const profileImage = user.profileImagePath || "../pictures/default-avatar.jpg";
        const avatarIcons = {
            "BRONZE": "../pictures/avatar_bronze.png",
            "SILVER": "../pictures/avatar_silver.png",
            "GOLD": "../pictures/avatar_gold.png"
        };
        const avatarIcon = avatarIcons[user.avatarLevel || "BRONZE"];

        html += `
            <li><a class="nav-link" href="/html/favorites.html">My Favorites</a></li>
            <li><a class="nav-link" id="inboxNavItem" href="/html/inbox.html">Inbox</a></li>
            <li><a class="nav-link" href="/html/threads.html">Threads</a></li>
        `;

        if (user.isAdmin) {
            html += `<li><a class="nav-link" href="/html/admin.html">Admin</a></li>`;
        }

        html += `
            <li class="nav-profile-image">
                <a href="/html/profile.html">
                    <img src="${profileImage}" alt="Profile" class="profile-img-nav">
                    <img src="${avatarIcon}" alt="Rank" class="avatar-rank-icon">
                </a>
            </li>
            <li><a id="logoutBtn" href="#" class="nav-link logout-link">Logout</a></li>
        `;
    } else {
        html += `<li><a class="nav-link" href="/html/login.html">Sign In</a></li>`;
    }

    html += `
            </ul>
            <div class="hamburger" onclick="toggleMobileMenu()">
                <span></span><span></span><span></span>
            </div>
        </div>
    </nav>
    <div class="mobile-menu-overlay" onclick="closeMobileMenu()"></div>
    <div class="mobile-menu">
        <ul class="mobile-nav">
            <li><a class="nav-link" href="/html/index.html" onclick="closeMobileMenu()">Home</a></li>
    `;

    if (user) {
        const profileImage = user.profileImagePath || "../pictures/default-avatar.png";

        html += `
            <li><a class="nav-link" href="/html/favorites.html" onclick="closeMobileMenu()">My Favorites</a></li>
            <li><a class="nav-link" href="/html/inbox.html" onclick="closeMobileMenu()">Inbox</a></li>
            <li><a class="nav-link" href="/html/threads.html" onclick="closeMobileMenu()">Threads</a></li>
            <li><a class="nav-link" href="/html/profile.html" onclick="closeMobileMenu()">Profile</a></li>
        `;

        if (user.isAdmin) {
            html += `<li><a class="nav-link" href="/html/admin.html" onclick="closeMobileMenu()">Admin</a></li>`;
        }

        html += `<li><a id="mobileLogoutBtn" class="nav-link logout-link" href="#">Logout</a></li>`;
    } else {
        html += `<li><a class="nav-link" href="/html/login.html" onclick="closeMobileMenu()">Sign In</a></li>`;
    }

    html += `
        </ul>
    </div>
    `;

    container.innerHTML = html;
    bindNavbarEvents();
}

// ✅ Bind logout button events
function bindNavbarEvents() {
    const logoutBtn = document.getElementById("logoutBtn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", () => logout());
    }

    const mobileLogoutBtn = document.getElementById("mobileLogoutBtn");
    if (mobileLogoutBtn) {
        mobileLogoutBtn.addEventListener("click", () => {
            logout();
            closeMobileMenu();
        });
    }
}

// ✅ Clear session and redirect to home
function logout() {
    sessionStorage.clear();
    window.location.href = "/html/index.html";
}

// ✅ Listen to real-time inbox count using Firebase
function listenToInboxCount(userId) {
    const countRef = ref(db, `userInboxCount/${userId}`);
    onValue(countRef, (snapshot) => {
        const count = snapshot.val();
        const inboxItem = document.getElementById("inboxNavItem");
        if (inboxItem) {
            inboxItem.innerHTML = `Inbox ${count > 0 ? `<span class="inbox-badge">${count}</span>` : ''}`;
        }
    });
}

// Initialize mobile menu functionality
function initMobileMenu() {
    const hamburger = document.querySelector('.hamburger');
    const overlay = document.querySelector('.mobile-menu-overlay');
    const mobileNavLinks = document.querySelectorAll('.mobile-nav-link');

    if (hamburger) {
        hamburger.addEventListener('click', toggleMobileMenu);
    }

    if (overlay) {
        overlay.addEventListener('click', closeMobileMenu);
    }

    // Add click listeners to mobile nav links
    mobileNavLinks.forEach(link => {
        link.addEventListener('click', closeMobileMenu);
    });

    // Close menu on outside click
    document.addEventListener('click', function (event) {
        const mobileMenu = document.querySelector('.mobile-menu');
        const navbar = document.querySelector('.navbar');

        if (mobileMenu && navbar) {
            if (!navbar.contains(event.target) &&
                !mobileMenu.contains(event.target) &&
                mobileMenu.classList.contains('active')) {
                closeMobileMenu();
            }
        }
    });

    // Close menu on escape key
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeMobileMenu();
        }
    });
}

// Toggle mobile menu
function toggleMobileMenu() {
    const mobileMenu = document.querySelector('.mobile-menu');
    const overlay = document.querySelector('.mobile-menu-overlay');
    const hamburger = document.querySelector('.hamburger');

    if (mobileMenu && overlay && hamburger) {
        const isActive = mobileMenu.classList.contains('active');

        if (isActive) {
            closeMobileMenu();
        } else {
            mobileMenu.classList.add('active');
            overlay.classList.add('active');
            hamburger.classList.add('active');
            document.body.style.overflow = 'hidden';
            document.body.classList.add('mobile-menu-open'); 
        }
    }
}

// Close mobile menu
function closeMobileMenu() {
    const mobileMenu = document.querySelector('.mobile-menu');
    const overlay = document.querySelector('.mobile-menu-overlay');
    const hamburger = document.querySelector('.hamburger');

    if (mobileMenu && overlay && hamburger) {
        mobileMenu.classList.remove('active');
        overlay.classList.remove('active');
        hamburger.classList.remove('active');
        document.body.style.overflow = '';
        document.body.classList.remove('mobile-menu-open'); 
    }
}

// Make functions available globally
window.toggleMobileMenu = toggleMobileMenu;
window.closeMobileMenu = closeMobileMenu;