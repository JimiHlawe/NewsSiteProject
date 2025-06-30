const apiBase = "/api/Users";

document.addEventListener("DOMContentLoaded", () => {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    if (!user) {
        alert("Please log in");
        window.location.href = "index.html";
        return;
    }

    document.getElementById("profileName").innerText = user.name;
    document.getElementById("profileEmail").innerText = user.email;

    loadUserTags();
    loadAllTags();
});

function loadUserTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    fetch(`${apiBase}/GetTags/${user.id}`)
        .then(res => res.json())
        .then(tags => {
            const container = document.getElementById("userTags");
            container.innerHTML = "";
            tags.forEach(tag => {
                const div = document.createElement("div");
                div.innerHTML = `${tag.name} <button onclick="removeTag(${tag.id})">Remove</button>`;
                container.appendChild(div);
            });
        });
}

function loadAllTags() {
    fetch(`${apiBase}/AllTags`)
        .then(res => res.json())
        .then(tags => {
            const container = document.getElementById("allTagsContainer");
            container.innerHTML = "";
            tags.forEach(tag => {
                container.innerHTML += `
                    <label>
                        <input type="checkbox" value="${tag.id}"> ${tag.name}
                    </label><br>`;
            });
        });
}

function removeTag(tagId) {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));

    fetch(`${apiBase}/RemoveTag`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, tagId })
    })
        .then(() => {
            loadUserTags();
            alert("Tag removed");
        });
}

function addSelectedTags() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const selected = document.querySelectorAll("#allTagsContainer input:checked");

    selected.forEach(chk => {
        fetch(`${apiBase}/AddTag`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userId: user.id, tagId: parseInt(chk.value) })
        });
    });

    alert("Tags added!");
    loadUserTags();
}

function updatePassword() {
    const user = JSON.parse(sessionStorage.getItem("loggedUser"));
    const newPass = document.getElementById("newPassword").value.trim();

    if (newPass.length < 5) {
        alert("Password too short");
        return;
    }

    fetch(`${apiBase}/UpdatePassword`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId: user.id, newPassword: newPass })
    })
        .then(() => {
            alert("Password updated");
            document.getElementById("newPassword").value = "";
        });
}
