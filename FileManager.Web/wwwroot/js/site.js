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
    if (sidebar) {
        sidebar.classList.remove('sidebar-collapsed');
    }

    if (typeof initializeFilesManager === 'function') {
        initializeFilesManager();
    }
    if (window.contextMenu && typeof window.contextMenu.init === 'function' && window.filesManager) {
        window.contextMenu.init(window.filesManager);
    }
});

