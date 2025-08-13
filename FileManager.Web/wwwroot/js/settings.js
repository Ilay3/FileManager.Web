/**
 * Скрипт для страниц настроек
 */
document.addEventListener('DOMContentLoaded', function () {
    // Инициализация компонентов
    initSettingsLoader();
    initSettingsAlerts();
    initSettingsForms();
    initSettingsToggles();
    initSettingsNav();
});

/**
 * Инициализация индикатора загрузки
 */
function initSettingsLoader() {
    // Создаем элемент лоадера, если его нет
    if (!document.querySelector('.settings-loader')) {
        const loaderContainer = document.createElement('div');
        loaderContainer.className = 'settings-loader';
        loaderContainer.innerHTML = '<div class="settings-spinner"></div>';
        document.body.appendChild(loaderContainer);
    }

    // Функция для показа загрузчика
    window.showSettingsLoader = function () {
        const loader = document.querySelector('.settings-loader');
        if (loader) {
            loader.style.display = 'flex';
        }
    };

    // Функция для скрытия загрузчика
    window.hideSettingsLoader = function () {
        const loader = document.querySelector('.settings-loader');
        if (loader) {
            loader.style.display = 'none';
        }
    };

    // Показываем загрузчик при отправке формы
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            window.showSettingsLoader();

            // Деактивируем кнопки
            const buttons = this.querySelectorAll('button[type="submit"]');
            buttons.forEach(button => {
                button.disabled = true;
                button.dataset.originalText = button.textContent;
                button.textContent = 'Сохранение...';
            });
        });
    });
}

/**
 * Анимация и стилизация уведомлений
 */
function initSettingsAlerts() {
    // Находим все алерты на странице
    const alerts = document.querySelectorAll('.alert');

    alerts.forEach(alert => {
        // Добавляем класс стилей для настроек
        alert.classList.add('settings-styled');

        // Автоматически скрываем уведомления через 5 секунд
        setTimeout(() => {
            if (alert.parentNode) {
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-10px)';
                alert.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

                setTimeout(() => {
                    if (alert.parentNode) {
                        alert.remove();
                    }
                }, 500);
            }
        }, 5000);
    });
}

/**
 * Улучшения для форм настроек
 */
function initSettingsForms() {
    // Стилизуем стандартные элементы форм
    document.querySelectorAll('.form-control').forEach(input => {
        input.classList.add('settings-styled');
    });

    document.querySelectorAll('.form-select').forEach(select => {
        select.classList.add('settings-styled');
    });

    document.querySelectorAll('.btn').forEach(button => {
        button.classList.add('settings-styled');
    });

    // Добавляем анимацию при фокусе на поля ввода
    document.querySelectorAll('input, textarea, select').forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.classList.add('is-focused');
        });

        input.addEventListener('blur', function () {
            this.parentElement.classList.remove('is-focused');
        });
    });

    // Конвертируем числовые значения MB в более читабельный формат
    document.querySelectorAll('input[type="number"][id*="Size"], input[type="number"][id*="Quota"]').forEach(input => {
        const formatSizeUnits = (bytes) => {
            if (bytes < 1024) return bytes + ' B';
            else if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
            else if (bytes < 1073741824) return (bytes / 1048576).toFixed(1) + ' MB';
            else return (bytes / 1073741824).toFixed(1) + ' GB';
        };

        // Добавляем элемент для отображения форматированного значения
        const formattedValueEl = document.createElement('div');
        formattedValueEl.className = 'settings-help-text';
        formattedValueEl.textContent = formatSizeUnits(parseInt(input.value) || 0);
        input.parentElement.appendChild(formattedValueEl);

        // Обновляем отформатированное значение при изменении поля
        input.addEventListener('input', function () {
            formattedValueEl.textContent = formatSizeUnits(parseInt(this.value) || 0);
        });
    });
}

/**
 * Инициализация переключателей
 */
function initSettingsToggles() {
    // Заменяем стандартные чекбоксы на красивые переключатели
    document.querySelectorAll('.form-check-input[type="checkbox"]').forEach(checkbox => {
        // Создаем только если ещё не обёрнут в toggle
        if (!checkbox.parentElement.classList.contains('settings-toggle')) {
            const label = checkbox.parentElement;
            const labelText = label.textContent.trim();

            // Создаем переключатель
            const toggle = document.createElement('label');
            toggle.className = 'settings-toggle';

            // Перемещаем чекбокс внутрь переключателя
            toggle.appendChild(checkbox.cloneNode(true));

            // Добавляем ползунок
            const slider = document.createElement('span');
            slider.className = 'settings-toggle-slider';
            toggle.appendChild(slider);

            // Удаляем оригинальный чекбокс
            checkbox.remove();

            // Очищаем текст метки
            label.textContent = '';

            // Добавляем переключатель и текст
            label.appendChild(toggle);
            label.appendChild(document.createTextNode(labelText));

            // Сохраняем классы для стилизации
            label.classList.add('settings-form-check');
        }
    });
}

/**
 * Инициализация навигации по настройкам
 */
function initSettingsNav() {
    // Создаем навигацию, если на странице есть заголовок "Настройки"
    const titleEl = document.querySelector('h2');
    if (titleEl && (titleEl.textContent.includes('Настройки') || titleEl.textContent.includes('настройки'))) {
        // Сохраняем родительский элемент до начала манипуляций
        const container = titleEl.parentNode;

        // Создаем навигацию только если её ещё нет
        if (!document.querySelector('.settings-nav')) {
            const settingsPages = [
                { url: '/Admin/Settings', title: 'Общие', icon: 'bi bi-gear' },
                { url: '/Admin/Settings/Storage', title: 'Хранилище', icon: 'bi bi-hdd' },
                { url: '/Admin/Settings/Security', title: 'Безопасность', icon: 'bi bi-shield-lock' },
                { url: '/Admin/Settings/Email', title: 'Эл. почта', icon: 'bi bi-envelope' },
                { url: '/Admin/Settings/Audit', title: 'Аудит', icon: 'bi bi-list-check' },
                { url: '/Admin/Settings/Uploads', title: 'Загрузки', icon: 'bi bi-upload' },
                { url: '/Admin/Settings/Versioning', title: 'Версионирование', icon: 'bi bi-clock-history' },
                { url: '/Admin/Settings/Cleanup', title: 'Очистка', icon: 'bi bi-trash' }
            ];

            const nav = document.createElement('div');
            nav.className = 'settings-nav';

            // Получаем текущий путь
            const currentPath = window.location.pathname;

            // Создаем ссылки навигации
            settingsPages.forEach(page => {
                const link = document.createElement('a');
                link.href = page.url;
                link.className = 'settings-nav-item';
                link.innerHTML = `<i class="${page.icon}"></i> ${page.title}`;

                // Добавляем активный класс для текущей страницы
                if (currentPath === page.url) {
                    link.classList.add('active');
                }

                nav.appendChild(link);
            });

            // Вставляем навигацию перед заголовком
            container.insertBefore(nav, titleEl);

            // Оборачиваем контент в контейнер для стилизации
            const contentElements = Array.from(container.children).filter(el => el !== nav);

            const contentContainer = document.createElement('div');
            contentContainer.className = 'settings-content';

            // Перемещаем элементы в контейнер
            contentElements.forEach(el => {
                contentContainer.appendChild(el);
            });

            // Добавляем контейнер на страницу
            container.appendChild(contentContainer);

            // Оборачиваем всё в общий контейнер
            const mainContainer = document.createElement('div');
            mainContainer.className = 'settings-container';

            // Перемещаем навигацию и контент в главный контейнер
            mainContainer.appendChild(nav);
            mainContainer.appendChild(contentContainer);

            // Заменяем старое содержимое
            container.parentNode.replaceChild(mainContainer, container);
        }
    }
}

/**
 * Функция для создания индикатора загрузки при переходах между страницами
 */
function navigateWithTransition(url) {
    window.showSettingsLoader();
    window.location.href = url;
}

// Добавляем функцию в глобальную область видимости
window.navigateWithTransition = navigateWithTransition;