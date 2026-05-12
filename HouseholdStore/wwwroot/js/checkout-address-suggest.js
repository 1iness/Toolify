(function () {
    function debounce(fn, delay) {
        let timer = null;
        return function () {
            const args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(null, args); }, delay);
        };
    }

    function createDropdown(input) {
        const parent = input.parentElement;
        if (!parent) return null;
        parent.classList.add('address-suggest-wrap');

        const list = document.createElement('div');
        list.className = 'address-suggest-list';
        list.setAttribute('role', 'listbox');
        list.hidden = true;
        parent.appendChild(list);
        return list;
    }

    function hide(list) {
        if (!list) return;
        list.hidden = true;
        list.innerHTML = '';
    }

    function render(list, input, items, onPick) {
        if (!list) return;
        list.innerHTML = '';
        if (!items || items.length === 0) {
            hide(list);
            return;
        }

        items.forEach(function (text) {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'address-suggest-item';
            btn.textContent = text;
            btn.setAttribute('role', 'option');
            btn.addEventListener('mousedown', function (event) {
                event.preventDefault();
                input.dataset.suggestPicked = '1';
                input.value = text;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                hide(list);
                if (onPick) onPick(text);
            });
            list.appendChild(btn);
        });

        list.hidden = false;
    }

    async function fetchJson(url) {
        const response = await fetch(url, { credentials: 'same-origin' });
        if (!response.ok) return [];
        return await response.json();
    }

    function setupAddressSuggest() {
        const cityInput = document.querySelector('[data-geo-suggest="city"]');
        const streetInput = document.querySelector('[data-geo-suggest="street"]');
        if (!cityInput || !streetInput) return;

        const cityList = createDropdown(cityInput);
        const streetList = createDropdown(streetInput);

        const loadCities = debounce(async function () {
            if (cityInput.dataset.suggestPicked === '1') {
                delete cityInput.dataset.suggestPicked;
                hide(cityList);
                return;
            }

            const term = cityInput.value.trim();
            if (term.length < 1) {
                hide(cityList);
                return;
            }

            const items = await fetchJson('/api/belarus-geo/cities?limit=30&term=' + encodeURIComponent(term));
            render(cityList, cityInput, items, function () {
                cityInput.dataset.suggestPicked = '1';
                streetInput.value = '';
                hide(cityList);
                streetInput.focus();
            });
        }, 220);

        const loadStreets = debounce(async function () {
            if (streetInput.dataset.suggestPicked === '1') {
                delete streetInput.dataset.suggestPicked;
                hide(streetList);
                return;
            }

            const city = cityInput.value.trim();
            const term = streetInput.value.trim();
            if (city.length < 2 || term.length < 1) {
                hide(streetList);
                return;
            }

            const url = '/api/belarus-geo/streets?limit=30'
                + '&city=' + encodeURIComponent(city)
                + '&term=' + encodeURIComponent(term);
            const items = await fetchJson(url);
            render(streetList, streetInput, items, function () {
                streetInput.dataset.suggestPicked = '1';
                hide(streetList);
            });
        }, 260);

        cityInput.addEventListener('input', loadCities);
        streetInput.addEventListener('input', loadStreets);

        cityInput.addEventListener('focus', function () {
            if (cityInput.value.trim().length >= 1) loadCities();
        });

        streetInput.addEventListener('focus', function () {
            if (streetInput.value.trim().length >= 1) loadStreets();
        });

        cityInput.addEventListener('blur', function () {
            setTimeout(function () { hide(cityList); }, 160);
        });

        streetInput.addEventListener('blur', function () {
            setTimeout(function () { hide(streetList); }, 160);
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hide(cityList);
                hide(streetList);
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupAddressSuggest);
    } else {
        setupAddressSuggest();
    }
})();
