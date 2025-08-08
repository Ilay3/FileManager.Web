let mainContainer;
let isLoading = false;

function initializeLayout() {
    // Поиск в хедере
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        const params = new URLSearchParams(window.location.search);
        searchInput.value = params.get('SearchRequest.SearchTerm') || '';
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const term = this.value;
            searchTimeout = setTimeout(() => {
                if (window.filesManager) {
                    filesManager.performSearch(term);
                } else {
                    const url = '/Files?SearchRequest.SearchTerm=' + encodeURIComponent(term);
                    if (typeof loadPage === 'function') {
                        loadPage(url);
                    } else {
                        window.location.href = url;
                    }
                }
            }, 300);
        });
    }

    const uploadBtn = document.getElementById('btnUpload');
    if (uploadBtn) {
        uploadBtn.addEventListener('click', () => {
            const folderId = window.filesManager ? filesManager.currentFolderId : null;
            openUploadModal(folderId);
        });
    }
    const createFolderBtn = document.getElementById('btnCreateFolder');
    if (createFolderBtn) {
        createFolderBtn.addEventListener('click', () => {
            const folderId = window.filesManager ? filesManager.currentFolderId : null;
            openCreateFolderModal(folderId);
        });
    }
    const accessBtn = document.getElementById('btnManageAccess');
    if (accessBtn) {
        accessBtn.addEventListener('click', () => {
            const folderId = window.filesManager ? filesManager.currentFolderId : null;
            openAccessModal(folderId, true);
        });
    }
}

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

    // Переключение видимости пароля
    document.querySelectorAll('.password-toggle').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = document.getElementById(this.dataset.target);
            if (!input) return;
            const isPassword = input.getAttribute('type') === 'password';
            input.setAttribute('type', isPassword ? 'text' : 'password');
            this.innerHTML = isPassword ? '<i class="bi bi-eye-slash"></i>' : '<i class="bi bi-eye"></i>';
        });
    });

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

    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const savedSidebar = localStorage.getItem('sidebar');
    if (sidebar && savedSidebar === 'collapsed') {
        sidebar.classList.add('sidebar-collapsed');
    }
    if (sidebar && sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('sidebar-collapsed');
            const state = sidebar.classList.contains('sidebar-collapsed') ? 'collapsed' : 'expanded';
            localStorage.setItem('sidebar', state);
        });
    }

    initializeLayout();
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
            const scripts = newContent.querySelectorAll('script');
            current.innerHTML = newContent.innerHTML;
            scripts.forEach(oldScript => {
                const script = document.createElement('script');
                if (oldScript.src) {
                    script.src = oldScript.src;
                } else {
                    script.textContent = oldScript.textContent;
                }
                document.body.appendChild(script);
                document.body.removeChild(script);
            });
            if (typeof initializeFilesManager === 'function') {
                initializeFilesManager();
            }
            initializeLayout();
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

