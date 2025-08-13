const originalFetch = window.fetch;
window.fetch = (input, init = {}) => {
    init = init || {};
    init.credentials = init.credentials || 'include';
    init.headers = init.headers || {};
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        if (init.headers instanceof Headers) {
            if (!init.headers.has('RequestVerificationToken')) {
                init.headers.set('RequestVerificationToken', token);
            }
        } else if (!init.headers['RequestVerificationToken']) {
            init.headers['RequestVerificationToken'] = token;
        }
    }

    return originalFetch(input, init);
};

let mainContainer;

function initializeLayout() {
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
    document.body.classList.add('fade-enter-active');
    document.body.addEventListener('transitionend', () => {
        document.body.classList.remove('fade-enter');
        document.body.classList.remove('fade-enter-active');
    }, { once: true });
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
    if (sidebar) {
        sidebar.classList.remove('sidebar-collapsed');
    }

    initializeLayout();
    if (typeof initializeFilesManager === 'function') {
        initializeFilesManager();
    }
    if (window.contextMenu && typeof window.contextMenu.init === 'function' && window.filesManager) {
        window.contextMenu.init(window.filesManager);
    }
});

