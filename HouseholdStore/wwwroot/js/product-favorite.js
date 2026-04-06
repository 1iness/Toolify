document.querySelector('.btn-favourite')?.addEventListener('click', async function () {
    const productId = this.dataset.productId;
    const res = await fetch(`/Favourites/Toggle?productId=${productId}`, { method: 'POST' });
    const data = await res.json();

    const icon = this.querySelector('i');
    const span = this.querySelector('span');

    if (data.isFavourite) {
        icon.className = 'bi bi-heart-fill';
        icon.style.color = '#ff4d4f';
        span.textContent = 'В избранном';
    } else {
        icon.className = 'bi bi-heart';
        icon.style.color = 'currentColor';
        span.textContent = 'В избранное';
    }
});