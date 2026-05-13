(function () {
    var partialHeaders = { 'X-Admin-Partial': '1', 'Accept': 'text/html' };

    var CATEGORIES_LOAD_MORE_CHUNK = 6;
    var categoriesRevealState = { visibleCount: CATEGORIES_LOAD_MORE_CHUNK };

    var PRODUCTS_LOAD_MORE_CHUNK = 10;
    var productsRevealState = { visibleCount: PRODUCTS_LOAD_MORE_CHUNK };

    var adminOfferLists = {
        promocodes: {
            chunk: 8,
            visibleCount: 8,
            rowSelector: '[data-promo-code-row]',
            searchAttr: 'data-promo-code-search',
            wrapId: 'promo-codes-pagination-wrap',
            loadMoreId: 'promo-codes-load-more-btn',
            loadLessId: 'promo-codes-load-less-btn',
            infoId: 'promo-codes-pagination-info',
            statusFilterId: 'promo-code-status-filter',
            statusAttr: 'data-promo-code-status'
        },
        promotions: {
            chunk: 5,
            visibleCount: 5,
            rowSelector: '[data-promotion-row]',
            searchAttr: 'data-promotion-search',
            wrapId: 'promotions-pagination-wrap',
            loadMoreId: 'promotions-load-more-btn',
            loadLessId: 'promotions-load-less-btn',
            infoId: 'promotions-pagination-info'
        },
        discounts: {
            chunk: 7,
            visibleCount: 7,
            rowSelector: '[data-discount-row]',
            searchAttr: 'data-discount-search',
            wrapId: 'discounts-pagination-wrap',
            loadMoreId: 'discounts-load-more-btn',
            loadLessId: 'discounts-load-less-btn',
            infoId: 'discounts-pagination-info'
        }
    };

    /** Категории: показ по chunk, «Показать ещё» и «Свернуть» (на chunk). resetReveal — при поиске / первый заход. */
    function syncCategoriesAdminTable(resetReveal) {
        var tbody = document.getElementById('categories-filter-tbody');
        var wrap = document.getElementById('categories-pagination-wrap');
        var loadLessBtn = document.getElementById('categories-load-less-btn');
        var loadMoreBtn = document.getElementById('categories-load-more-btn');
        var info = document.getElementById('categories-pagination-info');
        if (!tbody) return;

        var emptyRow = tbody.querySelector('tr.categories-empty-row');
        var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr[data-category-search]'));

        if (emptyRow && rows.length === 0) {
            emptyRow.style.display = '';
            emptyRow.classList.remove('category-admin-row--search-hide', 'category-admin-row--page-hide');
            if (wrap) wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (emptyRow) {
            emptyRow.style.display = 'none';
            emptyRow.classList.add('category-admin-row--search-hide');
        }

        if (resetReveal) categoriesRevealState.visibleCount = CATEGORIES_LOAD_MORE_CHUNK;

        var headerInput = document.getElementById('admin-header-search');
        var q = (headerInput && headerInput.value ? headerInput.value : '').toLowerCase().trim();

        var matched = [];
        rows.forEach(function (row) {
            var hay = (row.getAttribute('data-category-search') || '').toLowerCase();
            var match = !q || hay.indexOf(q) !== -1;
            row.classList.toggle('category-admin-row--search-hide', !match);
            row.classList.remove('category-admin-row--page-hide');
            if (match) matched.push(row);
        });

        if (!wrap) return;

        if (matched.length === 0) {
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (matched.length <= CATEGORIES_LOAD_MORE_CHUNK) {
            matched.forEach(function (row) {
                row.classList.remove('category-admin-row--page-hide');
            });
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (categoriesRevealState.visibleCount > matched.length) {
            categoriesRevealState.visibleCount = matched.length;
        }

        var showUpTo = Math.min(categoriesRevealState.visibleCount, matched.length);

        matched.forEach(function (row, idx) {
            row.classList.toggle('category-admin-row--page-hide', idx >= showUpTo);
        });

        wrap.classList.remove('d-none');
        if (loadLessBtn) {
            var nextShowUpLess = Math.max(
                CATEGORIES_LOAD_MORE_CHUNK,
                showUpTo - CATEGORIES_LOAD_MORE_CHUNK
            );
            var hideCount = showUpTo - nextShowUpLess;
            if (showUpTo > CATEGORIES_LOAD_MORE_CHUNK && hideCount > 0) {
                loadLessBtn.classList.remove('d-none');
                loadLessBtn.textContent =
                    hideCount < CATEGORIES_LOAD_MORE_CHUNK
                        ? 'Свернуть (' + hideCount + ')'
                        : 'Свернуть ' + CATEGORIES_LOAD_MORE_CHUNK;
                loadLessBtn.disabled = false;
            } else {
                loadLessBtn.classList.add('d-none');
            }
        }
        if (loadMoreBtn) {
            var remaining = matched.length - showUpTo;
            if (remaining > 0) {
                loadMoreBtn.classList.remove('d-none');
                loadMoreBtn.textContent =
                    remaining <= CATEGORIES_LOAD_MORE_CHUNK
                        ? 'Показать ещё (' + remaining + ')'
                        : 'Показать ещё ' + CATEGORIES_LOAD_MORE_CHUNK;
                loadMoreBtn.disabled = false;
            } else {
                loadMoreBtn.classList.add('d-none');
            }
        }
        if (info) info.textContent = 'Показано ' + showUpTo + ' из ' + matched.length;
    }

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
            input.placeholder = 'Поиск по названию, артикулу, описанию…';
        } else if (searchTarget === 'products-categories') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по названию категории и характеристикам…';
        } else if (searchTarget === 'promocodes') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по коду промокода и проценту…';
        } else if (searchTarget === 'promotions') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по названию, типу акции и условиям…';
        } else if (searchTarget === 'discounts') {
            wrap.classList.remove('d-none');
            input.placeholder = 'Поиск по названию, размеру и типу скидки…';
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
        tbody.querySelectorAll('tr.order-row-summary[data-order-search]').forEach(function (row) {
            var hay = (row.getAttribute('data-order-search') || '').toLowerCase();
            var match = !q || hay.indexOf(q) !== -1;
            row.style.display = match ? '' : 'none';
            var next = row.nextElementSibling;
            if (next && next.classList && next.classList.contains('order-row-detail')) {
                next.style.display = match ? '' : 'none';
            }
        });
    }

    function parsePositiveDecimalOrClear(raw) {
        if (raw == null) return null;
        var v = ('' + raw).trim().replace(',', '.');
        if (v === '') return null;
        var n = parseFloat(v);
        if (!isFinite(n) || n < 0) return null;
        return n;
    }

    function parseNonNegativeIntOrClear(raw) {
        if (raw == null) return null;
        var v = ('' + raw).trim();
        if (v === '') return null;
        var n = parseInt(v, 10);
        if (!isFinite(n) || n < 0 || /[.,]/.test(v)) return null;
        return n;
    }

    function normalizeProductStockInputValue(el) {
        var v = (el.value || '').trim().replace(',', '.');
        if (v === '') return;
        if (!/^\d+$/.test(v)) {
            var n = parseFloat(v);
            el.value =
                !isFinite(n) || n < 0 ? '' : String(Math.max(0, Math.floor(Math.abs(n))));
        }
    }

    /** Во время ввода убрать минус остальное режем только для количества (только целые цифры). */
    function onProductNumericFilterInput(el, kind) {
        if (!el || el.disabled) return;
        el.value = el.value.replace(/-/g, '');
        if (kind === 'stock') el.value = ('' + el.value).replace(/[^\d]/g, '');
    }

    function bindAdminProductListFilters() {
        document.addEventListener('input', function (e) {
            var tid = e.target.id;
            if (tid === 'admin-product-filter-price-min' || tid === 'admin-product-filter-price-max') {
                onProductNumericFilterInput(e.target, 'price');
                applyProductsSearchFilter();
            } else if (tid === 'admin-product-filter-stock-min' || tid === 'admin-product-filter-stock-max') {
                onProductNumericFilterInput(e.target, 'stock');
                applyProductsSearchFilter();
            }
        });
        document.addEventListener(
            'blur',
            function (e) {
                var tid = e.target.id;
                if (tid === 'admin-product-filter-price-min' || tid === 'admin-product-filter-price-max') {
                    enforceProductPriceRangeOnBlur();
                    applyProductsSearchFilter();
                }
                if (tid === 'admin-product-filter-stock-min' || tid === 'admin-product-filter-stock-max') {
                    enforceProductStockRangeOnBlur();
                    applyProductsSearchFilter();
                }
            },
            true
        );
        document.addEventListener('click', function (e) {
            if (!e.target.closest('#admin-product-filter-reset')) return;
            resetAdminProductFilters();
        });
    }

    /** При потере фокуса при обоих числах оставить «от» меньше «до» (повышаем максимум). */
    function enforceProductPriceRangeOnBlur() {
        var minEl = document.getElementById('admin-product-filter-price-min');
        var maxEl = document.getElementById('admin-product-filter-price-max');
        if (!minEl || !maxEl) return;
        var lowRaw = parsePositiveDecimalOrClear(minEl.value.replace(',', '.'));
        var hiRaw = parsePositiveDecimalOrClear(maxEl.value.replace(',', '.'));
        minEl.value = lowRaw != null ? String(lowRaw).replace('.', ',') : '';
        maxEl.value = hiRaw != null ? String(hiRaw).replace('.', ',') : '';
        lowRaw = parsePositiveDecimalOrClear(minEl.value.replace(',', '.'));
        hiRaw = parsePositiveDecimalOrClear(maxEl.value.replace(',', '.'));
        if (lowRaw != null && hiRaw != null && lowRaw > hiRaw) {
            maxEl.value = String(lowRaw).replace('.', ',');
        }
    }

    function enforceProductStockRangeOnBlur() {
        var minEl = document.getElementById('admin-product-filter-stock-min');
        var maxEl = document.getElementById('admin-product-filter-stock-max');
        if (!minEl || !maxEl) return;
        normalizeProductStockInputValue(minEl);
        normalizeProductStockInputValue(maxEl);
        var low = parseNonNegativeIntOrClear(minEl.value);
        var hi = parseNonNegativeIntOrClear(maxEl.value);
        minEl.value = low != null ? String(low) : '';
        maxEl.value = hi != null ? String(hi) : '';
        if (low != null && hi != null && low > hi) maxEl.value = String(low);
    }

    function resetAdminProductFilters() {
        ['admin-product-filter-price-min', 'admin-product-filter-price-max', 'admin-product-filter-stock-min', 'admin-product-filter-stock-max'].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.value = '';
        });
        syncProductsAdminTable(true);
    }

    /** Товары фильтры + показ порциями (как категории), chunk = 10. resetReveal при смене фильтров. */
    function syncProductsAdminTable(resetReveal) {
        var tbody = document.getElementById('products-filter-tbody');
        var wrap = document.getElementById('products-pagination-wrap');
        var loadLessBtn = document.getElementById('products-load-less-btn');
        var loadMoreBtn = document.getElementById('products-load-more-btn');
        var info = document.getElementById('products-pagination-info');
        if (!tbody) return;

        if (resetReveal) productsRevealState.visibleCount = PRODUCTS_LOAD_MORE_CHUNK;

        var headerInput = document.getElementById('admin-header-search');
        var q = headerInput ? (headerInput.value || '').toLowerCase().trim() : '';

        var rawMinPrice = '';
        var rawMaxPrice = '';
        var rawMinStock = '';
        var rawMaxStock = '';
        var minPriceEl = document.getElementById('admin-product-filter-price-min');
        var maxPriceEl = document.getElementById('admin-product-filter-price-max');
        var minStockEl = document.getElementById('admin-product-filter-stock-min');
        var maxStockEl = document.getElementById('admin-product-filter-stock-max');
        if (minPriceEl) rawMinPrice = minPriceEl.value;
        if (maxPriceEl) rawMaxPrice = maxPriceEl.value;
        if (minStockEl) rawMinStock = minStockEl.value;
        if (maxStockEl) rawMaxStock = maxStockEl.value;

        var pLow = parsePositiveDecimalOrClear(rawMinPrice.replace(',', '.'));
        var pHi = parsePositiveDecimalOrClear(rawMaxPrice.replace(',', '.'));
        var sLow = parseNonNegativeIntOrClear(rawMinStock);
        var sHi = parseNonNegativeIntOrClear(rawMaxStock);

        if (pLow != null && pHi != null && pLow > pHi) {
            var pt = pLow;
            pLow = pHi;
            pHi = pt;
        }
        if (sLow != null && sHi != null && sLow > sHi) {
            var st = sLow;
            sLow = sHi;
            sHi = st;
        }

        var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr[data-product-search]'));
        var matched = [];
        rows.forEach(function (row) {
            var hay = (row.getAttribute('data-product-search') || '').toLowerCase();
            var okText = !q || hay.indexOf(q) !== -1;

            var rowPrice = parseFloat(row.getAttribute('data-product-price'));
            if (!isFinite(rowPrice)) rowPrice = 0;
            var okPrice = true;
            if (pLow != null && rowPrice < pLow - 1e-9) okPrice = false;
            if (pHi != null && rowPrice > pHi + 1e-9) okPrice = false;

            var stock = parseInt(row.getAttribute('data-product-stock'), 10);
            if (!isFinite(stock) || stock < 0) stock = 0;
            var okStock = true;
            if (sLow != null && stock < sLow) okStock = false;
            if (sHi != null && stock > sHi) okStock = false;

            var match = okText && okPrice && okStock;
            row.classList.toggle('product-admin-row--filter-hide', !match);
            row.classList.remove('product-admin-row--page-hide');
            if (match) matched.push(row);
        });

        if (!wrap) return;

        if (matched.length === 0) {
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (matched.length <= PRODUCTS_LOAD_MORE_CHUNK) {
            matched.forEach(function (row) {
                row.classList.remove('product-admin-row--page-hide');
            });
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (productsRevealState.visibleCount > matched.length) {
            productsRevealState.visibleCount = matched.length;
        }

        var showUpTo = Math.min(productsRevealState.visibleCount, matched.length);

        matched.forEach(function (row, idx) {
            row.classList.toggle('product-admin-row--page-hide', idx >= showUpTo);
        });

        wrap.classList.remove('d-none');
        if (loadLessBtn) {
            var nextShowUpLess = Math.max(
                PRODUCTS_LOAD_MORE_CHUNK,
                showUpTo - PRODUCTS_LOAD_MORE_CHUNK
            );
            var hideCount = showUpTo - nextShowUpLess;
            if (showUpTo > PRODUCTS_LOAD_MORE_CHUNK && hideCount > 0) {
                loadLessBtn.classList.remove('d-none');
                loadLessBtn.textContent =
                    hideCount < PRODUCTS_LOAD_MORE_CHUNK
                        ? 'Свернуть (' + hideCount + ')'
                        : 'Свернуть ' + PRODUCTS_LOAD_MORE_CHUNK;
                loadLessBtn.disabled = false;
            } else {
                loadLessBtn.classList.add('d-none');
            }
        }
        if (loadMoreBtn) {
            var remaining = matched.length - showUpTo;
            if (remaining > 0) {
                loadMoreBtn.classList.remove('d-none');
                loadMoreBtn.textContent =
                    remaining <= PRODUCTS_LOAD_MORE_CHUNK
                        ? 'Показать ещё (' + remaining + ')'
                        : 'Показать ещё ' + PRODUCTS_LOAD_MORE_CHUNK;
                loadMoreBtn.disabled = false;
            } else {
                loadMoreBtn.classList.add('d-none');
            }
        }
        if (info) info.textContent = 'Показано ' + showUpTo + ' из ' + matched.length;
    }

    function applyProductsSearchFilter() {
        syncProductsAdminTable(true);
    }

    function syncAdminOfferList(key, resetReveal) {
        var cfg = adminOfferLists[key];
        if (!cfg) return;

        var rows = Array.prototype.slice.call(document.querySelectorAll(cfg.rowSelector));
        if (rows.length === 0) return;

        if (resetReveal) cfg.visibleCount = cfg.chunk;

        var headerInput = document.getElementById('admin-header-search');
        var q = headerInput ? (headerInput.value || '').toLowerCase().trim() : '';
        var statusFilter = cfg.statusFilterId ? document.getElementById(cfg.statusFilterId) : null;
        var statusValue = statusFilter ? statusFilter.value : 'all';

        var matched = [];
        rows.forEach(function (row) {
            var hay = (row.getAttribute(cfg.searchAttr) || '').toLowerCase();
            var okText = !q || hay.indexOf(q) !== -1;
            var okStatus = !cfg.statusAttr || statusValue === 'all' || row.getAttribute(cfg.statusAttr) === statusValue;
            var match = okText && okStatus;
            row.classList.toggle('admin-offer-row--filter-hide', !match);
            row.classList.remove('admin-offer-row--page-hide');
            if (match) matched.push(row);
        });

        var wrap = document.getElementById(cfg.wrapId);
        var loadMoreBtn = document.getElementById(cfg.loadMoreId);
        var loadLessBtn = document.getElementById(cfg.loadLessId);
        var info = document.getElementById(cfg.infoId);
        if (!wrap) return;

        if (matched.length === 0) {
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (matched.length <= cfg.chunk) {
            matched.forEach(function (row) {
                row.classList.remove('admin-offer-row--page-hide');
            });
            wrap.classList.add('d-none');
            if (loadMoreBtn) loadMoreBtn.classList.add('d-none');
            if (loadLessBtn) loadLessBtn.classList.add('d-none');
            if (info) info.textContent = '';
            return;
        }

        if (cfg.visibleCount > matched.length) cfg.visibleCount = matched.length;
        var showUpTo = Math.min(cfg.visibleCount, matched.length);

        matched.forEach(function (row, idx) {
            row.classList.toggle('admin-offer-row--page-hide', idx >= showUpTo);
        });

        wrap.classList.remove('d-none');
        if (loadLessBtn) {
            var nextShowUpLess = Math.max(cfg.chunk, showUpTo - cfg.chunk);
            var hideCount = showUpTo - nextShowUpLess;
            if (showUpTo > cfg.chunk && hideCount > 0) {
                loadLessBtn.classList.remove('d-none');
                loadLessBtn.textContent = hideCount < cfg.chunk ? 'Свернуть (' + hideCount + ')' : 'Свернуть ' + cfg.chunk;
                loadLessBtn.disabled = false;
            } else {
                loadLessBtn.classList.add('d-none');
            }
        }
        if (loadMoreBtn) {
            var remaining = matched.length - showUpTo;
            if (remaining > 0) {
                loadMoreBtn.classList.remove('d-none');
                loadMoreBtn.textContent = remaining <= cfg.chunk ? 'Показать ещё (' + remaining + ')' : 'Показать ещё ' + cfg.chunk;
                loadMoreBtn.disabled = false;
            } else {
                loadMoreBtn.classList.add('d-none');
            }
        }
        if (info) info.textContent = 'Показано ' + showUpTo + ' из ' + matched.length;
    }

    function applyAdminSearchTarget(searchTarget, resetReveal) {
        if (searchTarget === 'orders') applyOrdersSearchFilter();
        if (searchTarget === 'products-list') applyProductsSearchFilter();
        if (searchTarget === 'products-categories') syncCategoriesAdminTable(resetReveal !== false);
        if (searchTarget === 'promocodes') syncAdminOfferList('promocodes', resetReveal !== false);
        if (searchTarget === 'promotions') syncAdminOfferList('promotions', resetReveal !== false);
        if (searchTarget === 'discounts') syncAdminOfferList('discounts', resetReveal !== false);
    }

    bindAdminProductListFilters();

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
            applyAdminSearchTarget(searchTarget, true);
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
                    var categoriesTbody = document.getElementById('categories-filter-tbody');
                    if (categoriesTbody) {
                        syncCategoriesAdminTable(true);
                        return;
                    }
                    if (document.querySelector('[data-promo-code-row]')) {
                        syncAdminOfferList('promocodes', true);
                        return;
                    }
                    if (document.querySelector('[data-promotion-row]')) {
                        syncAdminOfferList('promotions', true);
                        return;
                    }
                    if (document.querySelector('[data-discount-row]')) {
                        syncAdminOfferList('discounts', true);
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
        applyAdminSearchTarget(searchTarget, true);
        refreshChatBadge();

        document.addEventListener('change', function (e) {
            if (e.target.id === 'promo-code-status-filter') {
                syncAdminOfferList('promocodes', true);
            }
        });

        document.addEventListener('click', function (e) {
            var offerKeys = Object.keys(adminOfferLists);
            for (var oi = 0; oi < offerKeys.length; oi++) {
                var offerKey = offerKeys[oi];
                var cfg = adminOfferLists[offerKey];
                var offerLess = e.target.closest('#' + cfg.loadLessId);
                if (offerLess && !offerLess.disabled) {
                    cfg.visibleCount = Math.max(cfg.chunk, cfg.visibleCount - cfg.chunk);
                    syncAdminOfferList(offerKey, false);
                    return;
                }
                var offerMore = e.target.closest('#' + cfg.loadMoreId);
                if (offerMore && !offerMore.disabled) {
                    cfg.visibleCount += cfg.chunk;
                    syncAdminOfferList(offerKey, false);
                    return;
                }
            }

            var productsLess = e.target.closest('#products-load-less-btn');
            if (productsLess && !productsLess.disabled) {
                productsRevealState.visibleCount = Math.max(
                    PRODUCTS_LOAD_MORE_CHUNK,
                    productsRevealState.visibleCount - PRODUCTS_LOAD_MORE_CHUNK
                );
                syncProductsAdminTable(false);
                return;
            }
            var productsMore = e.target.closest('#products-load-more-btn');
            if (productsMore && !productsMore.disabled) {
                productsRevealState.visibleCount += PRODUCTS_LOAD_MORE_CHUNK;
                syncProductsAdminTable(false);
                return;
            }

            var loadLessBtnEl = e.target.closest('#categories-load-less-btn');
            if (loadLessBtnEl && !loadLessBtnEl.disabled) {
                categoriesRevealState.visibleCount = Math.max(
                    CATEGORIES_LOAD_MORE_CHUNK,
                    categoriesRevealState.visibleCount - CATEGORIES_LOAD_MORE_CHUNK
                );
                syncCategoriesAdminTable(false);
                return;
            }
            var loadMoreBtnEl = e.target.closest('#categories-load-more-btn');
            if (!loadMoreBtnEl || loadMoreBtnEl.disabled) return;
            categoriesRevealState.visibleCount += CATEGORIES_LOAD_MORE_CHUNK;
            syncCategoriesAdminTable(false);
        });

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
