(function () {
    var partialHeaders = { 'X-Admin-Partial': '1', 'Accept': 'text/html' };

    function shouldInterceptAnchor(a) {
        if (!a || a.closest('[data-admin-no-spa]')) return false;
        if (a.target === '_blank' || a.hasAttribute('download')) return false;
        var href = a.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return false;
        var u;
        try {
            u = new URL(href, window.location.origin);
        } catch (e) {
            return false;
        }
        if (u.origin !== window.location.origin) return false;
        if (u.pathname.indexOf('/Export') !== -1) return false;
        if (u.pathname.indexOf('/Admin/Delete') !== -1) return false;
        if (u.pathname.indexOf('/Admin/GetFeatures') !== -1) return false;
        if (u.pathname.indexOf('/Chat/AdminConversation') !== -1) return false;
        if (u.pathname.indexOf('/Admin') === 0 || u.pathname === '/Chat/Admin') return true;
        return false;
    }

    function executeScripts(container) {
        container.querySelectorAll('script').forEach(function (oldScript) {
            var s = document.createElement('script');
            for (var i = 0; i < oldScript.attributes.length; i++) {
                var attr = oldScript.attributes[i];
                s.setAttribute(attr.name, attr.value);
            }
            s.textContent = oldScript.textContent;
            oldScript.parentNode.replaceChild(s, oldScript);
        });
    }

    function applyNavActive(panel) {
        document.querySelectorAll('[data-admin-panel]').forEach(function (el) {
            el.classList.remove('is-active');
        });
        var exact = document.querySelector('[data-admin-panel="' + panel + '"]');
        if (exact) exact.classList.add('is-active');
        if (panel === 'products-edit') {
            var listLink = document.querySelector('[data-admin-panel="products-list"]');
            if (listLink) listLink.classList.add('is-active');
        }
    }

    function setAdminPanelWideClass(panel) {
        var root = document.getElementById('admin-panel-root');
        if (!root) return;
        if (panel === 'chat') root.classList.remove('admin-panel-root--wide');
        else root.classList.add('admin-panel-root--wide');
    }

    function applySearchVisibility(searchTarget) {
        var wrap = document.getElementById('admin-header-search-wrap');
        var input = document.getElementById('admin-header-search');
        if (!wrap || !input) return;
        if (searchTarget === 'clients') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по ФИО, email и телефону…';
        } else if (searchTarget === 'orders') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по имени и email…';
        } else if (searchTarget === 'products-list') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по названию товара…';
        } else {
            wrap.classList.add('d-none');
            input.value = '';
        }
    }

    function applyOrdersSearchFilter() {
        var headerInput = document.getElementById('admin-header-search');
        var tbody = document.getElementById('orders-filter-tbody');
        if (!headerInput || !tbody) return;
        var q = (headerInput.value || '').toLowerCase().trim();
        tbody.querySelectorAll('tr[data-order-search]').forEach(function (row) {
            var hay = (row.getAttribute('data-order-search') || '').toLowerCase();
            row.style.display = !q || hay.indexOf(q) !== -1 ? '' : 'none';
        });
    }

    function applyProductsSearchFilter() {
        var headerInput = document.getElementById('admin-header-search');
        var tbody = document.getElementById('products-filter-tbody');
        if (!headerInput || !tbody) return;
        var q = (headerInput.value || '').toLowerCase().trim();
        tbody.querySelectorAll('tr[data-product-search]').forEach(function (row) {
            var hay = (row.getAttribute('data-product-search') || '').toLowerCase();
            row.style.display = !q || hay.indexOf(q) !== -1 ? '' : 'none';
        });
    }

    function updateHeadingFromPanel(root, titleFromHeader) {
        var headingEl = document.querySelector('.admin-page-heading');
        if (!headingEl || !root) return;
        if (titleFromHeader) {
            headingEl.textContent = titleFromHeader;
            return;
        }
        var pick = root.querySelector('[data-admin-heading]');
        if (pick) {
            headingEl.textContent = pick.getAttribute('data-admin-heading');
            return;
        }
        var h = root.querySelector('h1');
        if (!h) h = root.querySelector('.page-header h2, .orders-container h2, .clients-hero-title');
        if (!h) h = root.querySelector('h2.h3, h2.fw-bold');
        if (h) headingEl.textContent = h.textContent.trim();
    }

    function canonicalAdminUrl(url) {
        try {
            var u = new URL(url, window.location.origin);
            u.searchParams.delete('adminPartial');
            u.searchParams.delete('partial');
            return u.pathname + u.search + u.hash;
        } catch (e) {
            return url;
        }
    }

    function fetchAdminPartialUrl(url) {
        try {
            var u = new URL(url, window.location.origin);
            u.searchParams.set('adminPartial', '1');
            return u.pathname + u.search;
        } catch (e) {
            return url;
        }
    }
    function sanitizeInjectedAdminHtml(html) {
        try {
            var tpl = document.createElement('template');
            tpl.innerHTML = html.trim();
            var shell = tpl.content.querySelector('.admin-shell');
            if (!shell) return html.trim();
            var innerPanel = shell.querySelector('#admin-panel-root');
            if (innerPanel) return innerPanel.innerHTML;
            return shell.innerHTML;
        } catch (e) {
            return html.trim();
        }
    }

    async function refreshChatBadge() {
        try {
            var res = await fetch('/Chat/AdminUnreadCount', { credentials: 'same-origin' });
            if (!res.ok) return;
            var data = await res.json();
            var n = data.count || 0;
            var badge = document.getElementById('admin-chat-badge');
            if (!badge) return;
            if (n > 0) {
                badge.textContent = n > 99 ? '99+' : String(n);
                badge.classList.remove('d-none');
            } else badge.classList.add('d-none');
        } catch (e) {}
    }

    async function loadPanel(url, pushState) {
        var root = document.getElementById('admin-panel-root');
        if (!root) return;
        root.classList.add('admin-panel-loading');
        var displayUrl = canonicalAdminUrl(url);
        var fetchUrl = fetchAdminPartialUrl(url);
        try {
            var res = await fetch(fetchUrl, { headers: partialHeaders, credentials: 'same-origin' });
            if (!res.ok) {
                window.location.href = displayUrl;
                return;
            }
            var html = await res.text();
            var panel = res.headers.get('x-admin-panel') || '';
            var searchTarget = res.headers.get('x-admin-search') || 'none';
            var rawTitle = res.headers.get('x-admin-page-title');
            var titleFromHeader = null;
            if (rawTitle) {
                try {
                    titleFromHeader = decodeURIComponent(rawTitle);
                } catch (e) {
                    titleFromHeader = null;
                }
            }
            root.innerHTML = sanitizeInjectedAdminHtml(html);
            executeScripts(root);
            applyNavActive(panel);
            setAdminPanelWideClass(panel);
            applySearchVisibility(searchTarget);
            updateHeadingFromPanel(root, titleFromHeader);
            if (searchTarget === 'orders') applyOrdersSearchFilter();
            if (searchTarget === 'products-list') applyProductsSearchFilter();
            document.dispatchEvent(
                new CustomEvent('admin-panel-loaded', {
                    bubbles: true,
                    detail: { url: displayUrl, panel: panel }
                })
            );
            await refreshChatBadge();
            document.body.classList.remove('admin-sidebar-open');
            if (pushState !== false) history.pushState({ adminPanelUrl: displayUrl }, '', displayUrl);
        } catch (err) {
            window.location.href = displayUrl;
        } finally {
            root.classList.remove('admin-panel-loading');
        }
    }

    function bindClientsSearchBridge() {
        document.addEventListener(
            'input',
            function (e) {
                if (e.target.id === 'admin-header-search') {
                    var pageInput = document.getElementById('userSearch');
                    if (pageInput) {
                        pageInput.value = e.target.value;
                        pageInput.dispatchEvent(new Event('input', { bubbles: true }));
                        return;
                    }
                    var ordersTbody = document.getElementById('orders-filter-tbody');
                    if (ordersTbody) {
                        applyOrdersSearchFilter();
                        return;
                    }
                    var productsTbody = document.getElementById('products-filter-tbody');
                    if (productsTbody) {
                        applyProductsSearchFilter();
                        return;
                    }
                }
                if (e.target.id === 'userSearch') {
                    var hdr = document.getElementById('admin-header-search');
                    var wrap = document.getElementById('admin-header-search-wrap');
                    if (hdr && wrap && !wrap.classList.contains('d-none')) hdr.value = e.target.value;
                }
            },
            true
        );
    }

    document.addEventListener('DOMContentLoaded', function () {
        var body = document.body;
        var initial = body.dataset.initialPanel || 'home';
        var searchTarget = body.dataset.searchTarget || 'none';
        applyNavActive(initial);
        setAdminPanelWideClass(initial);
        applySearchVisibility(searchTarget);
        bindClientsSearchBridge();
        if (searchTarget === 'orders') applyOrdersSearchFilter();
        if (searchTarget === 'products-list') applyProductsSearchFilter();
        refreshChatBadge();

        document.addEventListener(
            'click',
            function (e) {
                var a = e.target.closest('a[href]');
                if (!a || !shouldInterceptAnchor(a)) return;
                e.preventDefault();
                loadPanel(a.href);
            },
            true
        );

        document.getElementById('admin-sidebar-toggle')?.addEventListener('click', function () {
            document.body.classList.toggle('admin-sidebar-open');
        });
        document.getElementById('admin-shell-overlay')?.addEventListener('click', function () {
            document.body.classList.remove('admin-sidebar-open');
        });

        window.addEventListener('popstate', function () {
            loadPanel(window.location.href, false);
        });

        history.replaceState({ adminPanelUrl: window.location.href }, '', window.location.href);
    });
})();
