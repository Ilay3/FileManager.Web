// Улучшенный JavaScript для FileManager с современными анимациями

let mainContainer;
let isLoading = false;
let loadingTimeout;

// Утилиты для анимаций
const animations = {
    fadeIn: (element, duration = 300) => {
        element.style.opacity = '0';
        element.style.transition = `opacity ${duration}ms ease`;
        element.offsetHeight; // force reflow
        element.style.opacity = '1';
    },

    slideInUp: (element, duration = 400) => {
        element.style.transform = 'translateY(20px)';
        element.style.opacity = '0';
        element.style.transition = `all ${duration}ms ease`;
        element.offsetHeight; // force reflow
        element.style.transform = 'translateY(0)';
        element.style.opacity = '1';
    },

    bounceIn: (element, duration = 600) => {
        element.style.transform = 'scale(0.3)';
        element.style.opacity = '0';
        element.style.transition = `all ${duration}ms cubic-bezier(0.68, -0.55, 0.265, 1.55)`;
        element.offsetHeight; // force reflow
        element.style.transform = 'scale(1)';
        element.style.opacity = '1';
    }
};

// Показать индикатор загрузки страницы
function showPageLoading() {
    const loadingBar = document.getElementById('pageLoadingBar');
    if (loadingBar) {
        loadingBar.style.display = 'block';
        loadingBar.style.animation = 'pageLoad 1s ease-in-out infinite';
    }
}

// Скрыть индикатор загрузки страницы
function hidePageLoading() {
    const loadingBar = document.getElementById('pageLoadingBar');
    if (loadingBar) {
        loadingBar.style.display = 'none';
    }
}

// Добавить эффект загрузки к кнопке
function addButtonLoading(button) {
    if (button) {
        button.classList.add('btn-loading');
        button.disabled = true;
    }
}

// Убрать эффект загрузки с кнопки
function removeButtonLoading(button) {
    if (button) {
        button.classList.remove('btn-loading');
        button.disabled = false;
    }
}

// Инициализация при загрузке DOM
document.addEventListener('DOMContentLoaded', function () {
    mainContainer = document.getElementById('pageContainer');

    // Анимация загрузки страницы
    if (mainContainer) {
        animations.slideInUp(mainContainer);
    }

    // Автофокус на первое поле ввода
    const firstInput = document.querySelector('input[type="text"], input[type="email"]');
    if (firstInput) {
        setTimeout(() => firstInput.focus(), 100);
    }

    // Подтверждение удаления с анимацией
    initializeDeleteConfirmations();

    // Поиск в реальном времени
    initializeLiveSearch();

    // Анимация появления элементов
    animateVisibleElements();

    // Обработка форм с анимациями
    initializeFormAnimations();

    // Drag & Drop анимации
    initializeDragDropAnimations();

    // Инициализация тултипов
    initializeTooltips();
});

// Инициализация подтверждений удаления
function initializeDeleteConfirmations() {
    const deleteButtons = document.querySelectorAll('[data-confirm]');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            const message = this.dataset.confirm || 'Вы уверены?';

            showCustomConfirm(message, () => {
                // Если пользователь подтвердил действие
                if (this.href) {
                    window.location.href = this.href;
                } else if (this.onclick) {
                    this.onclick();
                }
            });
        });
    });
}

// Кастомное модальное окно подтверждения
function showCustomConfirm(message, onConfirm) {
    const modal = document.createElement('div');
    modal.className = 'modal modal-backdrop';
    modal.innerHTML = `
        <div class="modal-content modal-content-animated">
            <div class="modal-header">
                <h3>Подтверждение</h3>
            </div>
            <div class="modal-body">
                <p>${message}</p>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary" id="cancelBtn">Отмена</button>
                <button class="btn btn-primary" id="confirmBtn">Подтвердить</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);

    // Показываем модальное окно с анимацией
    modal.style.display = 'flex';
    setTimeout(() => {
        modal.classList.add('show');
        modal.querySelector('.modal-content-animated').classList.add('show');
    }, 10);

    // Обработчики кнопок
    modal.querySelector('#cancelBtn').onclick = () => closeModal(modal);
    modal.querySelector('#confirmBtn').onclick = () => {
        onConfirm();
        closeModal(modal);
    };

    // Закрытие по клику вне модального окна
    modal.onclick = (e) => {
        if (e.target === modal) closeModal(modal);
    };

    function closeModal(modal) {
        modal.classList.remove('show');
        modal.querySelector('.modal-content-animated').classList.remove('show');
        setTimeout(() => modal.remove(), 200);
    }
}

// Инициализация живого поиска
function initializeLiveSearch() {
    const searchInput = document.querySelector('#searchInput, .search-input');
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);

            // Добавляем индикатор загрузки
            this.style.backgroundImage = 'url("data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'20\' height=\'20\' viewBox=\'0 0 24 24\' fill=\'none\' stroke=\'%23666\' stroke-width=\'2\' stroke-linecap=\'round\' stroke-linejoin=\'round\'%3E%3Ccircle cx=\'11\' cy=\'11\' r=\'8\'/%3E%3Cpath d=\'m21 21-4.35-4.35\'/%3E%3C/svg%3E")';
            this.style.backgroundRepeat = 'no-repeat';
            this.style.backgroundPosition = 'right 12px center';
            this.style.paddingRight = '40px';

            searchTimeout = setTimeout(() => {
                // Убираем индикатор загрузки
                this.style.backgroundImage = '';
                this.style.paddingRight = '';

                // Здесь будет вызов поиска
                if (typeof filesManager !== 'undefined' && this.value.length > 0) {
                    filesManager.performSearch(this.value);
                }
            }, 300);
        });
    }
}

// Анимация появления видимых элементов
function animateVisibleElements() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const element = entry.target;
                element.classList.add('fade-in-up');
                observer.unobserve(element);
            }
        });
    }, observerOptions);

    // Наблюдаем за файловыми карточками и элементами списка
    document.querySelectorAll('.file-card, .tile-row, .stat-card').forEach(el => {
        observer.observe(el);
    });
}

// Инициализация анимаций форм
function initializeFormAnimations() {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
            if (submitBtn && !submitBtn.classList.contains('btn-loading')) {
                addButtonLoading(submitBtn);
            }
        });

        // Анимация полей ввода
        const inputs = form.querySelectorAll('input, textarea, select');
        inputs.forEach(input => {
            input.addEventListener('focus', function () {
                this.parentElement.classList.add('focused');
            });

            input.addEventListener('blur', function () {
                this.parentElement.classList.remove('focused');
                if (this.value) {
                    this.parentElement.classList.add('has-value');
                } else {
                    this.parentElement.classList.remove('has-value');
                }
            });

            // Проверка на уже заполненные поля
            if (input.value) {
                input.parentElement.classList.add('has-value');
            }
        });
    });
}

// Инициализация Drag & Drop анимаций
function initializeDragDropAnimations() {
    const dropZones = document.querySelectorAll('.drop-zone, .content-area');

    dropZones.forEach(zone => {
        zone.classList.add('drop-zone-animate');

        zone.addEventListener('dragenter', function (e) {
            e.preventDefault();
            this.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', function (e) {
            e.preventDefault();
            if (!this.contains(e.relatedTarget)) {
                this.classList.remove('drag-over');
            }
        });

        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            this.classList.remove('drag-over');
            this.classList.add('success-pulse');
            setTimeout(() => this.classList.remove('success-pulse'), 600);
        });
    });
}

// Инициализация тултипов
function initializeTooltips() {
    const elementsWithTitles = document.querySelectorAll('[title]');
    elementsWithTitles.forEach(element => {
        let tooltip;

        element.addEventListener('mouseenter', function () {
            const title = this.getAttribute('title');
            if (!title) return;

            // Убираем стандартный тултип
            this.setAttribute('data-original-title', title);
            this.removeAttribute('title');

            // Создаем кастомный тултип
            tooltip = document.createElement('div');
            tooltip.className = 'custom-tooltip';
            tooltip.textContent = title;
            tooltip.style.cssText = `
                position: absolute;
                background: var(--text);
                color: var(--surface);
                padding: 6px 10px;
                border-radius: 4px;
                font-size: 12px;
                z-index: 9999;
                pointer-events: none;
                opacity: 0;
                transition: opacity 0.2s ease;
                box-shadow: var(--shadow);
            `;

            document.body.appendChild(tooltip);

            // Позиционируем тултип
            const rect = this.getBoundingClientRect();
            tooltip.style.left = rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2) + 'px';
            tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';

            // Показываем с анимацией
            setTimeout(() => tooltip.style.opacity = '1', 10);
        });

        element.addEventListener('mouseleave', function () {
            if (tooltip) {
                tooltip.style.opacity = '0';
                const toRemove = tooltip;
                setTimeout(() => toRemove.remove(), 200);
                tooltip = null;
            }

            // Восстанавливаем оригинальный title
            const originalTitle = this.getAttribute('data-original-title');
            if (originalTitle) {
                this.setAttribute('title', originalTitle);
                this.removeAttribute('data-original-title');
            }
        });
    });
}

// Улучшенная функция загрузки страницы
window.loadPage = async function (url, addToHistory = true) {
    if (isLoading) return;

    isLoading = true;
    showPageLoading();

    try {
        const response = await fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const html = await response.text();
        const doc = new DOMParser().parseFromString(html, 'text/html');
        const newContent = doc.getElementById('pageContainer');
        const current = document.getElementById('pageContainer');

        if (newContent && current) {
            // Анимация выхода
            current.style.opacity = '0';
            current.style.transform = 'translateY(-10px)';

            setTimeout(() => {
                current.innerHTML = newContent.innerHTML;

                // Анимация входа
                animations.slideInUp(current);

                // Переинициализация компонентов
                if (typeof initializeFilesManager === 'function') {
                    initializeFilesManager();
                }

                animateVisibleElements();
                initializeFormAnimations();
                initializeDragDropAnimations();
                initializeTooltips();

                if (addToHistory) {
                    history.pushState(null, '', url);
                }
            }, 150);
        } else {
            window.location.href = url;
        }
    } catch (error) {
        console.error('Ошибка загрузки страницы:', error);
        showNotification('Ошибка загрузки страницы', 'error');
        window.location.href = url;
    } finally {
        setTimeout(() => {
            hidePageLoading();
            isLoading = false;
        }, 300);
    }
};

// Обработка кликов по ссылкам
document.addEventListener('click', function (e) {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('mailto:') || href.startsWith('tel:')) return;
    if (link.getAttribute('target') === '_blank' || link.hasAttribute('download')) return;
    if (link.origin !== window.location.origin) return;
    if (link.classList.contains('no-spa')) return;

    e.preventDefault();

    // Добавляем эффект клика
    link.style.transform = 'scale(0.98)';
    setTimeout(() => {
        link.style.transform = '';
        loadPage(link.href);
    }, 100);
});

// Обработка истории браузера
window.addEventListener('popstate', () => {
    loadPage(window.location.href, false);
});

// Улучшенная функция показа уведомлений
window.showNotification = function (message, type = 'info', duration = 5000) {
    const container = document.getElementById('notificationContainer');
    if (!container) return;

    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;

    // Добавляем иконку в зависимости от типа
    const icons = {
        success: 'bi-check-circle',
        error: 'bi-x-circle',
        warning: 'bi-exclamation-triangle',
        info: 'bi-info-circle'
    };

    notification.innerHTML = `
        <div style="display: flex; align-items: center; gap: 8px;">
            <i class="bi ${icons[type] || icons.info}"></i>
            <span>${message}</span>
        </div>
    `;

    container.appendChild(notification);

    // Анимация появления
    animations.slideInUp(notification);

    // Автоматическое скрытие
    setTimeout(() => {
        notification.style.opacity = '0';
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => notification.remove(), 300);
    }, duration);

    // Клик для закрытия
    notification.addEventListener('click', () => {
        notification.style.opacity = '0';
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => notification.remove(), 300);
    });
};

// Утилитная функция для создания скелетонов загрузки
function createSkeleton(type = 'text', count = 3) {
    const container = document.createElement('div');

    for (let i = 0; i < count; i++) {
        const skeleton = document.createElement('div');
        skeleton.className = `skeleton skeleton-${type}`;
        container.appendChild(skeleton);
    }

    return container;
}

// Функция для замены контента скелетоном на время загрузки
function showSkeletonLoading(element, type = 'text') {
    if (!element) return;

    const originalContent = element.innerHTML;
    element.setAttribute('data-original-content', originalContent);
    element.innerHTML = '';
    element.appendChild(createSkeleton(type, 3));
}

// Функция для восстановления контента из скелетона
function hideSkeleton(element) {
    if (!element) return;

    const originalContent = element.getAttribute('data-original-content');
    if (originalContent) {
        element.innerHTML = originalContent;
        element.removeAttribute('data-original-content');
        animations.fadeIn(element);
    }
}

// Глобальные утилиты
window.animations = animations;
window.showPageLoading = showPageLoading;
window.hidePageLoading = hidePageLoading;
window.addButtonLoading = addButtonLoading;
window.removeButtonLoading = removeButtonLoading;
window.showSkeletonLoading = showSkeletonLoading;
window.hideSkeleton = hideSkeleton;