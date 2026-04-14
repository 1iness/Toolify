document.addEventListener("DOMContentLoaded", function () {
    const menuItems = document.querySelectorAll(".menu-item");
    const sections = document.querySelectorAll(".profile-section");

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

    const res = await fetch('/Favourites/Index', {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });

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
        <div class="product-card" style="border:1px solid #eee;border-radius:12px;padding:15px;">
            <a href="/Home/Details/${p.id}" style="text-decoration:none;color:inherit;">
                <img src="https://localhost:7188/api/Product/${p.id}/image"
                     style="width:100%;height:160px;object-fit:contain;margin-bottom:10px;"
                     onerror="this.src='/image/no-image.png'"/>
                <div style="font-weight:500;margin-bottom:8px;">${p.name}</div>
            </a>
            <div style="margin-bottom:10px;">${price}</div>
           <button onclick="removeFavourite(${p.id}, this)"
                style="width:100%;padding:8px;border:1px solid #ff4d4f;color:#ff4d4f;
                   background:#fff;border-radius:8px;cursor:pointer;font-weight:600;
                   display:flex;align-items:center;justify-content:center;gap:6px;">
                <i class="bi bi-heart-fill" style="font-size:15px;"></i> Удалить
            </button>
        </div>`;
    }).join('');
});

async function removeFavourite(productId, btn) {
    await fetch(`/Favourites/Toggle?productId=${productId}`, { method: 'POST' });
    btn.closest('.product-card').remove();
}