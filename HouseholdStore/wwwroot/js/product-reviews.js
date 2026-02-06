document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById('reviewModal');
    const starContainer = document.getElementById('starContainer');

    // если на странице нет модального окна выйдет 
    if (!modal || !starContainer) return;

    const stars = starContainer.querySelectorAll('.star-item');
    const ratingInput = document.getElementById('ratingInput');
    let currentRating = parseInt(ratingInput.value) || 5;

    // --- опен/клоуз ---
    window.openReviewModal = function () {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }

    window.closeReviewModal = function () {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }

    // если нажать вне модалки, то модалка закроется
    modal.addEventListener('click', (e) => {
        if (e.target === modal) closeReviewModal();
    });

    // --- логика работы звезд ---
    stars.forEach(star => {
        star.addEventListener('mouseover', () => {
            const val = parseInt(star.getAttribute('data-value'));
            fillStars(val, 'hover');
        });

        star.addEventListener('mouseout', () => {
            fillStars(currentRating, 'active');
        });

        star.addEventListener('click', () => {
            currentRating = parseInt(star.getAttribute('data-value'));
            ratingInput.value = currentRating;
            fillStars(currentRating, 'active');
        });
    });

    function fillStars(value, className) {
        stars.forEach(s => {
            const sVal = parseInt(s.getAttribute('data-value'));
            s.classList.remove('hover', 'active');
            if (sVal <= value) {
                s.classList.add(className);
            }
        });

        if (className === 'hover') {
        } else {
            stars.forEach(s => {
                if (parseInt(s.getAttribute('data-value')) <= currentRating) s.classList.add('active');
            });
        }
    }
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
            valA = parseInt(a.getAttribute('data-date'));
            valB = parseInt(b.getAttribute('data-date'));
        } else {
            valA = parseInt(a.getAttribute('data-rating'));
            valB = parseInt(b.getAttribute('data-rating'));
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