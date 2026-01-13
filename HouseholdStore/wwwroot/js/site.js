document.addEventListener("DOMContentLoaded", function () {
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }
    window.scrollTo(0, 0);

    //ФУНКЦИЯ ДЛЯ АНИМАЦИЙ ===
    function initScrollAnimations() {
        console.log("Инициализация анимаций...");

        const blocksToAnimate = [
            '.banner-container',
            '.categories-container',
            '.delivery-banner-container',
            '.cards-section',
            '.products-section',
            '.hit-products-section',
            '.main-footer'
        ];

        blocksToAnimate.forEach(selector => {
            const element = document.querySelector(selector);
            if (element) {
                element.classList.add('scroll-animate');
            }
        });

        const observerOptions = {
            threshold: 0.15,
            rootMargin: "0px 0px -50px 0px" 
        };

        const observer = new IntersectionObserver((entries, obs) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    console.log("Показываем элемент:", entry.target.className);
                    entry.target.classList.add('visible');
                    obs.unobserve(entry.target); 
                }
            });
        }, observerOptions);

        blocksToAnimate.forEach(selector => {
            const element = document.querySelector(selector);
            if (element) {
                observer.observe(element);
            }
        });

        window.addEventListener('scroll', () => {
            if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 100) {
                document.querySelectorAll('.scroll-animate:not(.visible)').forEach(el => {
                    el.classList.add('visible');
                });
            }
        });
        initCategoriesAnimation();
    }

    //АНИМАЦИЯ ДЛЯ КАТЕГОРИЙ
    function initCategoriesAnimation() {
        const categoriesContainer = document.querySelector('.categories-container');
        if (!categoriesContainer) return;

        const items = categoriesContainer.querySelectorAll('.category-item');
        if (!items.length) return;
        categoriesContainer.classList.add('scroll-animate');

        items.forEach((item, index) => {
            item.style.transitionDelay = `${(index + 1) * 0.1}s`;
        });
    }

    // запуск аним
    initScrollAnimations();

    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                window.scrollTo({
                    top: targetElement.offsetTop - 100,
                    behavior: 'smooth'
                });
            }
        });
    });
});


// наклон карточки
function initTiltCards(selector = '.category-item, .info-card') {
    const cards = Array.from(document.querySelectorAll(selector));
    if (!cards.length) return;

    const maxRotate = 3; 
    const scaleOnHover = 1.06;
    const rAFs = new WeakMap();

    function handleMove(e, card) {
        const clientX = e.clientX ?? (e.touches && e.touches[0]?.clientX);
        const clientY = e.clientY ?? (e.touches && e.touches[0]?.clientY);
        if (clientX == null || clientY == null) return;

        const rect = card.getBoundingClientRect();
        const cx = rect.left + rect.width / 2;
        const cy = rect.top + rect.height / 2;

        const dx = (clientX - cx) / (rect.width / 2);
        const dy = (clientY - cy) / (rect.height / 2);

        const clampedX = Math.max(-1, Math.min(1, dx));
        const clampedY = Math.max(-1, Math.min(1, dy));

        const rotateY = clampedX * maxRotate;      
        const rotateX = -clampedY * maxRotate;     

        if (rAFs.get(card)) cancelAnimationFrame(rAFs.get(card));
        const id = requestAnimationFrame(() => {
            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale(${scaleOnHover})`;
            card.style.boxShadow = `0 ${Math.abs(rotateX) + 12}px ${20 + Math.abs(rotateY)}px rgba(0,0,0,0.14)`;
            rAFs.delete(card);
        });
        rAFs.set(card, id);
    }

    function handleLeave(card) {
        if (rAFs.get(card)) cancelAnimationFrame(rAFs.get(card));
        card.style.transform = 'translateZ(0) scale(1)';
        card.style.boxShadow = '';
    }

    cards.forEach(card => {
        if (window.matchMedia && window.matchMedia('(hover: none), (pointer: coarse)').matches) {
            return;
        }

        const onMove = (e) => handleMove(e, card);
        const onEnter = (e) => { card.style.transition = 'transform 180ms cubic-bezier(.2,.8,.2,1), box-shadow 200ms ease'; onMove(e); };
        const onLeave = () => { card.style.transition = 'transform 400ms cubic-bezier(.2,.8,.2,1), box-shadow 300ms ease'; handleLeave(card); };

        card.addEventListener('mouseenter', onEnter);
        card.addEventListener('mousemove', onMove);
        card.addEventListener('mouseleave', onLeave);

    });
}

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => initTiltCards());
    } else {
        initTiltCards();
    }

    //ДЛЯ ЛИСТЬЕВ
window.addEventListener('load', () => {
    const leaf1 = document.querySelector('.leaf1');
    const leaf2 = document.querySelector('.leaf2');

    leaf1.addEventListener('animationend', () => {
        leaf1.classList.add('float1');
    });

    leaf2.addEventListener('animationend', () => {
        leaf2.classList.add('float2');
    });
});


// ДЛЯ ПОИСКА
    document.addEventListener("DOMContentLoaded", function () {
        const searchInput = document.getElementById("searchInput");
        const resultsBox = document.getElementById("searchResults");
        let debounceTimer;

        searchInput.addEventListener("input", function () {
            const query = this.value.trim();

            clearTimeout(debounceTimer);

            if (query.length < 2) {
                resultsBox.style.display = "none";
                resultsBox.innerHTML = "";
                return;
            }

            debounceTimer = setTimeout(() => {
                fetchProducts(query);
            }, 300);
        });

        async function fetchProducts(query) {
            try {
                const response = await fetch(`/Home/SearchJson?query=${encodeURIComponent(query)}`);
                if (!response.ok) return;

                const products = await response.json();
                renderResults(products);
            } catch (error) {
                console.error("Ошибка поиска:", error);
            }
        }

        function renderResults(products) {
            resultsBox.innerHTML = "";

            if (products.length === 0) {
                resultsBox.innerHTML = '<div style="padding:15px; color:#777; text-align:center;">Ничего не найдено</div>';
                resultsBox.style.display = "block";
                return;
            }

            products.forEach(p => {
                let priceHtml = '';
                
                if (p.discount > 0) {
                    let discountedPrice = p.price * (1 - p.discount / 100);
                    
                    priceHtml = `
                        <div class="price-block">
                            <span class="current-price" style="color: #e74c3c;">${formatMoney(discountedPrice)}</span>
                            <span class="old-price">${formatMoney(p.price)}</span>
                        </div>
                    `;
                } else {
                    priceHtml = `
                        <div class="price-block">
                            <span class="current-price">${formatMoney(p.price)}</span>
                        </div>
                    `;
                }

                const apiBaseUrl = "https://localhost:7188";
                const imgPath = p.imagePath ? (apiBaseUrl + p.imagePath) : '/images/no-image.png';

                const itemHtml = `
                    <a href="/Home/Details/${p.id}" class="search-item">
                        <img src="${imgPath}" alt="${p.name}" onerror="this.src='/images/no-image.png'">
        
                        <div class="info">
                            <div class="product-name">${p.name}</div>
                            <div class="product-art">Арт: ${p.articleNumber || '---'}</div>
                        </div>
        
                        ${priceHtml}
                    </a>
                `;
                resultsBox.innerHTML += itemHtml;
            });

            resultsBox.style.display = "block";
        }

        function formatMoney(amount) {
            return amount.toLocaleString('by-BY', { style: 'currency', currency: 'BYN', maximumFractionDigits: 0 });
        }

        document.addEventListener("click", function (e) {
            if (!searchInput.contains(e.target) && !resultsBox.contains(e.target)) {
                resultsBox.style.display = "none";
            }
        });
    });

    //Анимация товара для добавления в корзину
function addToCartAnimated(event, productId, btnElement) {

    event.preventDefault(); 

    const card = btnElement.closest('.product-card');
    const productImg = card.querySelector('img');

    const cartIcon = document.getElementById('cart-target-img');

    if (productImg && cartIcon) {
        const flyImg = productImg.cloneNode();
        flyImg.classList.add('flying-img'); 

        const rect = productImg.getBoundingClientRect();
        flyImg.style.left = rect.left + 'px';
        flyImg.style.top = rect.top + 'px';
        flyImg.style.width = productImg.offsetWidth + 'px';
        flyImg.style.height = productImg.offsetHeight + 'px';

        document.body.appendChild(flyImg);

        const cartRect = cartIcon.getBoundingClientRect();

        setTimeout(() => {
            flyImg.style.left = (cartRect.left + cartRect.width / 2 - 10) + 'px';
            flyImg.style.top = (cartRect.top + cartRect.height / 2 - 10) + 'px';

            flyImg.style.width = '20px';
            flyImg.style.height = '20px';
            flyImg.style.opacity = '0';
        }, 10);

        setTimeout(() => {
            flyImg.remove();

            cartIcon.style.transform = "scale(1.2)";
            cartIcon.style.transition = "transform 0.2s";
            setTimeout(() => cartIcon.style.transform = "scale(1)", 200);

        }, 1500); 
    }


    const originalContent = btnElement.innerHTML;
    btnElement.style.pointerEvents = 'none';
    btnElement.style.opacity = '0.7';

    fetch('/Cart/AddToCartApi?id=' + productId, {
        method: 'POST'
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const badge = document.getElementById('cart-count');
                if (badge) {
                    badge.innerText = data.count;
                    badge.style.display = 'inline-block';

                    badge.style.transform = "scale(1.5)";
                    badge.style.transition = "transform 0.2s";
                    setTimeout(() => badge.style.transform = "scale(1)", 200);
                }
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'Товар добавлен в корзину',
                    showConfirmButton: false,
                    timer: 2500,
                    timerProgressBar: true
                });
            }
        })
        .catch(error => console.error('Ошибка:', error))
        .finally(() => {
            btnElement.style.pointerEvents = 'auto';
            btnElement.style.opacity = '1';
        });
}

//Загрузка при открытии
document.addEventListener("DOMContentLoaded", function () {
    fetch('/Cart/GetCartCount') 
        .then(r => r.json())
        .then(data => {
            const badge = document.getElementById('cart-count');
            if (data.count > 0 && badge) {
                badge.innerText = data.count;
                badge.style.display = 'inline-block';
            }
        });
});