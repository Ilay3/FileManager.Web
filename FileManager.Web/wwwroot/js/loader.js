(function(){
    const loader = document.getElementById('loader');
    const bar = document.getElementById('loader-bar');
    function update(percent){
        if(bar){
            bar.style.width = percent + '%';
            bar.textContent = percent + '%';
        }
    }
    window.showLoader = function(percent){
        if(loader){
            loader.style.display = 'flex';
            update(percent);
        }
    };
    window.hideLoader = function(){
        if(loader){
            loader.style.display = 'none';
        }
    };
    window.fetchWithProgress = async function(url, options){
        showLoader(0);
        try {
            const response = await fetch(url, options);
            const contentLength = response.headers.get('Content-Length');
            if(!response.body || !contentLength){
                update(100);
                return response;
            }
            const total = parseInt(contentLength,10);
            let loaded = 0;
            const reader = response.body.getReader();
            const chunks = [];
            while(true){
                const {done, value} = await reader.read();
                if(done) break;
                chunks.push(value);
                loaded += value.length;
                update(Math.round((loaded/total)*100));
            }
            update(100);
            const blob = new Blob(chunks);
            return new Response(blob, {
                status: response.status,
                statusText: response.statusText,
                headers: response.headers
            });
        } finally {
            hideLoader();
        }
    };
})();
