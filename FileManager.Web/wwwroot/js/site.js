let mainContainer;
let isLoading = false;

document.addEventListener('DOMContentLoaded', function () {
    mainContainer = document.getElementById('pageContainer');
    if (mainContainer) {
        mainContainer.classList.add('page-enter');
        mainContainer.addEventListener('animationend', () => {
            mainContainer.classList.remove('page-enter');
        }, { once: true });
    }
    // Автофокус на первое поле ввода в формах
    const firstInput = document.querySelector('input[type="text"], input[type="email"]');
    if (firstInput) {
        firstInput.focus();
    }

    // Подтверждение удаления
    const deleteButtons = document.querySelectorAll('[data-confirm]');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const message = this.dataset.confirm || 'Вы уверены?';
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    // Поиск в реальном времени
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                // Здесь будет логика поиска
                console.log('Поиск:', this.value);
            }, 300);
        });
    }

    const themeToggle = document.getElementById('themeToggle');
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
        document.body.classList.add('dark-theme');
    }
    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            document.body.classList.toggle('dark-theme');
            const mode = document.body.classList.contains('dark-theme') ? 'dark' : 'light';
            localStorage.setItem('theme', mode);
        });
    }
});

window.loadPage = async function (url, addToHistory = true) {
    if (isLoading) return;
    isLoading = true;
    console.log('loadPage', url);
    try {
        const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!response.ok) {
            window.location.href = url;
            return;
        }
        const html = await response.text();
        const doc = new DOMParser().parseFromString(html, 'text/html');
        const newContent = doc.getElementById('pageContainer');
        const current = document.getElementById('pageContainer');
        if (newContent && current) {
            current.innerHTML = newContent.innerHTML;
            if (typeof initializeFilesManager === 'function') {
                initializeFilesManager();
            }
            if (addToHistory) {
                history.pushState(null, '', url);
            }
        } else {
            window.location.href = url;
        }
    } catch (err) {
        console.error('loadPage error', err);
        window.location.href = url;
    } finally {
        isLoading = false;
    }
};

document.addEventListener('click', function (e) {
    const link = e.target.closest('a');
    if (!link) return;
    const href = link.getAttribute('href');
    if (!href || href.startsWith('#')) return;
    if (link.getAttribute('target') === '_blank' || link.hasAttribute('download')) return;
    if (link.origin !== window.location.origin) return;
    e.preventDefault();
    loadPage(link.href);
});

window.addEventListener('popstate', () => {
    loadPage(window.location.href, false);
});

window.showNotification = function (message, type = 'info') {
    const container = document.getElementById('notificationContainer');
    if (!container) {
        return;
    }
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    container.appendChild(notification);
    setTimeout(() => notification.remove(), 5000);
};

