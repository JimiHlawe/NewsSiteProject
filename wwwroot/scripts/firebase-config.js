// firebase-config.js
import { initializeApp } from "https://www.gstatic.com/firebasejs/9.0.0/firebase-app.js";
import { getDatabase } from "https://www.gstatic.com/firebasejs/9.0.0/firebase-database.js";

// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
    apiKey: "AIzaSyAekTJ1zWYSUUSvYv4NPlxyIY5_1GrvZ8c",
    authDomain: "news-project-e6f1e.firebaseapp.com",
    databaseURL: "https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app",
    projectId: "news-project-e6f1e",
    storageBucket: "news-project-e6f1e.firebasestorage.app",
    messagingSenderId: "133902294593",
    appId: "1:133902294593:web:60eda1ed19bbae355930c0",
    measurementId: "G-999LH7BWRB"
};

const app = initializeApp(firebaseConfig);
const db = getDatabase(app);

export { db };
