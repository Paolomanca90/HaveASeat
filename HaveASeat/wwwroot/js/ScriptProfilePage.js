// Funzioni per gestire la modifica delle sezioni
function toggleEdit(section) {
    const viewDiv = document.getElementById(section + '-view');
    const editDiv = document.getElementById(section + '-edit');

    if (viewDiv.classList.contains('hidden')) {
        // Modalità visualizzazione
        viewDiv.classList.remove('hidden');
        editDiv.classList.add('hidden');
    } else {
        // Modalità modifica
        viewDiv.classList.add('hidden');
        editDiv.classList.remove('hidden');
    }
}

function saveSection(section) {
    // Simula il salvataggio dei dati
    showNotification('Modifiche salvate con successo! ✅');

    // Torna alla modalità visualizzazione
    setTimeout(() => {
        toggleEdit(section);
    }, 500);
}

function cancelEdit(section) {
    // Torna alla modalità visualizzazione senza salvare
    toggleEdit(section);
}

function togglePreference(toggle) {
    toggle.classList.toggle('active');
    showNotification('Preferenza aggiornata! ⚙️');
}

function editAvatar() {
    // Simula la modifica dell'avatar
    const avatars = [
        'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop&crop=face',
        'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=200&h=200&fit=crop&crop=face',
        'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&h=200&fit=crop&crop=face'
    ];

    const currentAvatar = document.getElementById('avatar');
    const randomAvatar = avatars[Math.floor(Math.random() * avatars.length)];
    currentAvatar.src = randomAvatar;

    showNotification('Avatar aggiornato! 📸');
}

function showNotification(message) {
    const notification = document.getElementById('notification');
    notification.textContent = message;
    notification.classList.add('show');

    setTimeout(() => {
        notification.classList.remove('show');
    }, 3000);
}

// Animazioni al caricamento della pagina
document.addEventListener('DOMContentLoaded', function () {
    const sections = document.querySelectorAll('.section');
    sections.forEach((section, index) => {
        section.style.animationDelay = `${index * 0.1}s`;
    });
});

// Aggiunge effetti hover dinamici
document.querySelectorAll('.info-item').forEach(item => {
    item.addEventListener('mouseenter', function () {
        this.style.backgroundColor = 'rgba(102, 126, 234, 0.05)';
        this.style.transform = 'translateX(5px)';
    });

    item.addEventListener('mouseleave', function () {
        this.style.backgroundColor = 'transparent';
        this.style.transform = 'translateX(0)';
    });
});