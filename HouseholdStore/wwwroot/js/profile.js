document.addEventListener("DOMContentLoaded", function () {
    const menuItems = document.querySelectorAll(".menu-item");
    const sections = document.querySelectorAll(".profile-section");

    function activateTab(targetTab) {
        if (!targetTab) return;
        const item = document.querySelector(`.menu-item[data-tab="${targetTab}"]`);
        if (!item) return;
        item.click();
    }

    menuItems.forEach(item => {
        item.addEventListener("click", function () {
            const targetTab = this.getAttribute("data-tab");

            menuItems.forEach(i => i.classList.remove("active"));
            this.classList.add("active");

            sections.forEach(section => {
                if (section.id === `tab-${targetTab}`) {
                    section.style.display = "block";
                } else {
                    section.style.display = "none";
                }
            });
        });
    });

    const initialTab = document.querySelector(".profile-container")?.dataset.profileTab;
    if (initialTab) {
        activateTab(initialTab);
    }

    const editForm = document.querySelector('#edit-mode form');
    if (!editForm) {
        return;
    }

    const nameInputs = editForm.querySelectorAll('input[name="FirstName"], input[name="LastName"]');
    const phoneInput = editForm.querySelector('input[name="Phone"]');
    const BELARUS_PHONE_DIGIT_LIMIT = 12;

    function normalizeNameInput(rawValue, trimEnd) {
        if (!rawValue) {
            return '';
        }

        let normalized = rawValue.replace(/^\s+/, '').replace(/\s{2,}/g, ' ');
        if (trimEnd) {
            normalized = normalized.trim();
        }

        return normalized;
    }

    nameInputs.forEach(input => {
        input.addEventListener('input', function () {
            this.value = normalizeNameInput(this.value, false);
        });

        input.addEventListener('blur', function () {
            this.value = normalizeNameInput(this.value, true);
        });
    });

    function formatBelarusPhone(rawValue) {
        let digits = (rawValue || '').replace(/\D/g, '');

        if (digits.startsWith('80')) {
            digits = `375${digits.slice(2)}`;
        } else if (digits.startsWith('0')) {
            digits = `375${digits.slice(1)}`;
        } else if (!digits.startsWith('375')) {
            digits = `375${digits}`;
        }

        digits = digits.slice(0, BELARUS_PHONE_DIGIT_LIMIT);

        const operatorCode = digits.slice(3, 5);
        const firstPart = digits.slice(5, 8);
        const secondPart = digits.slice(8, 10);
        const thirdPart = digits.slice(10, 12);

        let formatted = '+375';
        if (operatorCode) {
            formatted += ` (${operatorCode}`;
            if (operatorCode.length === 2) {
                formatted += ')';
            }
        }
        if (firstPart) {
            formatted += ` ${firstPart}`;
        }
        if (secondPart) {
            formatted += `-${secondPart}`;
        }
        if (thirdPart) {
            formatted += `-${thirdPart}`;
        }

        return formatted;
    }

    if (phoneInput) {
        phoneInput.addEventListener('focus', function () {
            if (!this.value.trim()) {
                this.value = '+375';
            }
        });

        phoneInput.addEventListener('input', function () {
            this.value = formatBelarusPhone(this.value);
        });

        phoneInput.addEventListener('keydown', function (e) {
            const allowedKeys = ['Backspace', 'Delete', 'Tab', 'Escape', 'Enter', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
            if (allowedKeys.includes(e.key) || e.ctrlKey || e.metaKey) {
                return;
            }

            const digits = this.value.replace(/\D/g, '');
            const hasSelection = this.selectionStart !== this.selectionEnd;
            if (/^\d$/.test(e.key) && digits.length >= BELARUS_PHONE_DIGIT_LIMIT && !hasSelection) {
                e.preventDefault();
            }
        });

        phoneInput.value = formatBelarusPhone(phoneInput.value);
    }

    editForm.addEventListener('submit', function () {
        nameInputs.forEach(input => {
            input.value = normalizeNameInput(input.value, true);
        });

        if (phoneInput) {
            phoneInput.value = formatBelarusPhone(phoneInput.value);
        }
    });

    const currentOrdersSearch = document.getElementById('current-orders-search');
    const currentOrdersStatusFilter = document.getElementById('current-orders-status-filter');
    const currentOrderCards = Array.from(document.querySelectorAll('[data-current-order-card]'));
    const currentOrdersNoResults = document.getElementById('current-orders-no-results');

    function syncCurrentOrdersFilters() {
        if (currentOrderCards.length === 0) {
            return;
        }

        const query = (currentOrdersSearch?.value || '').trim().toLowerCase();
        const status = currentOrdersStatusFilter?.value || '';
        let visibleCount = 0;

        currentOrderCards.forEach(card => {
            const cardSearch = (card.dataset.orderSearch || '').toLowerCase();
            const cardStatus = card.dataset.orderStatus || '';
            const matchesSearch = !query || cardSearch.includes(query);
            const matchesStatus = !status || cardStatus === status;
            const isVisible = matchesSearch && matchesStatus;

            card.classList.toggle('d-none', !isVisible);
            if (isVisible) {
                visibleCount += 1;
            }
        });

        currentOrdersNoResults?.classList.toggle('d-none', visibleCount > 0);
    }

    currentOrdersSearch?.addEventListener('input', syncCurrentOrdersFilters);
    currentOrdersStatusFilter?.addEventListener('change', syncCurrentOrdersFilters);
    syncCurrentOrdersFilters();

    document.querySelectorAll('[data-profile-offer-group]').forEach(group => {
        const toggle = group.querySelector('[data-offer-group-toggle]');
        if (!toggle) {
            return;
        }

        toggle.addEventListener('click', () => {
            const isOpen = group.classList.contains('offer-group--open');
            const allGroups = Array.from(document.querySelectorAll('[data-profile-offer-group]'));

            allGroups.forEach(item => {
                item.classList.remove('offer-group--open');
                item.querySelector('[data-offer-group-toggle]')?.setAttribute('aria-expanded', 'false');
            });

            if (!isOpen) {
                group.classList.add('offer-group--open');
                toggle.setAttribute('aria-expanded', 'true');
            }
        });
    });

    document.querySelectorAll('[data-profile-offer-list]').forEach(list => {
        const pageSize = Number.parseInt(list.dataset.offerPageSize || '4', 10);
        const cards = Array.from(list.querySelectorAll('[data-offer-card]'));
        const searchInput = list.querySelector('[data-offer-search-input]');
        const emptyState = list.querySelector('[data-offer-empty]');
        const pagination = list.querySelector('[data-offer-pagination]');
        const moreButton = list.querySelector('[data-offer-more]');
        const lessButton = list.querySelector('[data-offer-less]');
        let revealCount = pageSize;

        function getMatchingCards() {
            const query = (searchInput?.value || '').trim().toLowerCase();
            return cards.filter(card => {
                const searchText = (card.dataset.offerSearch || '').toLowerCase();
                return !query || searchText.includes(query);
            });
        }

        function syncOfferList(resetReveal) {
            if (resetReveal) {
                revealCount = pageSize;
            }

            const matchingCards = getMatchingCards();
            const visibleCards = matchingCards.slice(0, revealCount);
            const visibleSet = new Set(visibleCards);

            cards.forEach(card => {
                card.classList.toggle('d-none', !visibleSet.has(card));
            });

            emptyState?.classList.toggle('d-none', matchingCards.length > 0);
            pagination?.classList.toggle('d-none', matchingCards.length <= pageSize);
            moreButton?.classList.toggle('d-none', revealCount >= matchingCards.length);
            lessButton?.classList.toggle('d-none', revealCount <= pageSize);
        }

        searchInput?.addEventListener('input', () => syncOfferList(true));
        moreButton?.addEventListener('click', () => {
            revealCount += pageSize;
            syncOfferList(false);
        });
        lessButton?.addEventListener('click', () => {
            revealCount = Math.max(pageSize, revealCount - pageSize);
            syncOfferList(false);
        });

        syncOfferList(true);
    });
});

function enableEdit() {
    document.getElementById('view-mode').style.display = 'none';
    document.getElementById('edit-mode').style.display = 'block';
}

function cancelEdit() {
    document.getElementById('edit-mode').style.display = 'none';
    document.getElementById('view-mode').style.display = 'block';
}



document.querySelector('[data-tab="favourites"]')?.addEventListener('click', async function () {
    const grid = document.getElementById('favourites-grid');
    if (grid.dataset.loaded) return;

    const jsonRes = await fetch('/Favourites/GetJson');
    if (!jsonRes.ok) { grid.innerHTML = '<p>Ошибка загрузки</p>'; return; }

    const products = await jsonRes.json();
    grid.dataset.loaded = '1';

    if (!products.length) {
        grid.innerHTML = '<div style="text-align:center;padding:40px;color:#aaa;border:2px dashed #f0f0f0;border-radius:20px;"><p>Избранных товаров нет</p><a href="/Home/Index" style="color:#28a745;font-weight:700;">В каталог</a></div>';
        return;
    }

    grid.innerHTML = products.map(p => {
        const BYN_HTML = '<span class="nbrb-icon">&#xE901;</span>';
        const price = p.discount > 0
            ? `<span style="color:#e74c3c;font-weight:700;">${(p.price * (1 - p.discount / 100)).toFixed(2)} ${BYN_HTML}</span>
               <span style="text-decoration:line-through;color:#999;font-size:13px;margin-left:6px;">${p.price} ${BYN_HTML}</span>`
            : `<span style="font-weight:700;">${p.price} ${BYN_HTML}</span>`;

        return `
        <div class="favourite-card">
            <button type="button" class="favourite-remove" onclick="removeFavourite(${p.id}, this)"
                    aria-label="Удалить из избранного" title="Удалить из избранного">
                <i class="bi bi-heart-fill" aria-hidden="true"></i>
            </button>
            <a class="favourite-card-link" href="/Home/Details/${p.id}">
                <img src="https://localhost:7188/api/Product/${p.id}/image"
                     alt=""
                     onerror="this.src='/image/no-image.png'"/>
                <div class="favourite-card-name">${p.name}</div>
            </a>
            <div class="favourite-card-price">${price}</div>
        </div>`;
    }).join('');
});

async function removeFavourite(productId, btn) {
    await fetch(`/Favourites/Toggle?productId=${productId}`, { method: 'POST' });
    const card = btn.closest('.favourite-card');
    card?.remove();

    const grid = document.getElementById('favourites-grid');
    if (grid && !grid.querySelector('.favourite-card')) {
        grid.innerHTML = '<div style="text-align:center;padding:40px;color:#aaa;border:2px dashed #f0f0f0;border-radius:20px;"><p>Избранных товаров нет</p><a href="/Home/Index" style="color:#28a745;font-weight:700;">В каталог</a></div>';
    }
}