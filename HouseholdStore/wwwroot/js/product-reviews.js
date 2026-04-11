document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById('reviewModal');
    const starContainer = document.getElementById('starContainer');
    if (!modal || !starContainer) return;

    const ratingInput = document.getElementById('ratingInput');
    let currentRating = parseFloat(ratingInput.value) || 5;

    window.openReviewModal = function () {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        renderStars(currentRating);
    };

    window.closeReviewModal = function () {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    };

    modal.addEventListener('click', e => {
        if (e.target === modal) closeReviewModal();
    });

    const wraps = Array.from(starContainer.querySelectorAll('.star-wrap'));

    function renderStars(val) {
        wraps.forEach(wrap => {
            const full = parseFloat(wrap.dataset.full);
            const half = parseFloat(wrap.dataset.half);
            const svgHalf = wrap.querySelector('.star-half');
            const svgFull = wrap.querySelector('.star-full');
            const svgBg = wrap.querySelector('.star-bg');

            if (val >= full) {
                // полная жёлтая
                svgFull.style.display = '';
                svgHalf.style.display = 'none';
                svgBg.style.display = 'none';
            } else if (val >= half) {
                // половина жёлтая
                svgFull.style.display = 'none';
                svgHalf.style.display = '';
                svgBg.style.display = '';
            } else {
                // серая
                svgFull.style.display = 'none';
                svgHalf.style.display = 'none';
                svgBg.style.display = '';
            }
        });
    }

    // Hover
    starContainer.addEventListener('mousemove', function (e) {
        const zone = e.target.closest('.star-zone-left, .star-zone-right');
        if (!zone) return;
        renderStars(parseFloat(zone.dataset.value));
    });

    starContainer.addEventListener('mouseleave', function () {
        renderStars(currentRating);
    });

    // Клик
    starContainer.addEventListener('click', function (e) {
        const zone = e.target.closest('.star-zone-left, .star-zone-right');
        if (!zone) return;
        currentRating = parseFloat(zone.dataset.value);
        ratingInput.value = currentRating;
        renderStars(currentRating);
    });

    renderStars(currentRating);
});

let isDesc = true;
let currentCriteria = 'date';

function sortReviews(criteria) {
    const container = document.getElementById('review-list-container');
    const reviews = Array.from(container.getElementsByClassName('review-item'));
    const dateLink = document.getElementById('sort-date');
    const ratingLink = document.getElementById('sort-rating');

    if (currentCriteria === criteria) {
        isDesc = !isDesc;
    } else {
        currentCriteria = criteria;
        isDesc = true;
    }

    dateLink.classList.remove('active', 'sort-asc', 'sort-desc');
    ratingLink.classList.remove('active', 'sort-asc', 'sort-desc');

    const activeLink = (criteria === 'date') ? dateLink : ratingLink;
    activeLink.classList.add('active');
    activeLink.classList.add(isDesc ? 'sort-desc' : 'sort-asc');

    reviews.sort((a, b) => {
        let valA, valB;

        if (criteria === 'date') {
            valA = parseInt(a.getAttribute('data-date'), 10);
            valB = parseInt(b.getAttribute('data-date'), 10);
        } else {
            valA = parseFloat(a.getAttribute('data-rating'));
            valB = parseFloat(b.getAttribute('data-rating'));
        }

        if (isDesc) {
            return valB - valA; // От большего к меньшему
        } else {
            return valA - valB; // От меньшего к большему
        }
    });

    container.innerHTML = '';
    reviews.forEach(review => container.appendChild(review));
}