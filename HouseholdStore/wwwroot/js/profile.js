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

    editForm.addEventListener('submit', function () {
        nameInputs.forEach(input => {
            input.value = normalizeNameInput(input.value, true);
        });
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