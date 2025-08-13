(function () {
    const loader = document.getElementById('loader');

    window.showLoader = function () {
        if (loader) {
            loader.style.display = 'flex';
        }
    };

    window.hideLoader = function () {
        if (loader) {
            loader.style.display = 'none';
        }
    };

    window.fetchWithProgress = async function (url, options) {
        showLoader();
        try {
            return await fetch(url, options);
        } finally {
            hideLoader();
        }
    };
})();
