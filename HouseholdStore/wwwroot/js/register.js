document.addEventListener('DOMContentLoaded', function () {
    const toggleButtons = document.querySelectorAll('.toggle-password');

    toggleButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('input');
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);

            const icon = this.querySelector('img');
            icon.src = type === 'password'
                ? '/image/eye-icon.png'
                : '/image/eye-closed-icon.png';
        });
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const phoneInput = document.getElementById('phone');

    if (phoneInput) {
        phoneInput.addEventListener('focus', function () {
            if (!phoneInput.value) {
                phoneInput.value = '+375 ';
            }
        });

        phoneInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');

            if (!e.target.value.startsWith('+375')) {
                e.target.value = '+375 ';
                return;
            }

            let x = value.match(/(\d{0,3})(\d{0,2})(\d{0,3})(\d{0,2})(\d{0,2})/);

            if (!x[2]) {
                e.target.value = '+375 ';
            } else {
                e.target.value = '+375 (' + x[2] +
                    (x[3] ? ') ' + x[3] : '') +
                    (x[4] ? '-' + x[4] : '') +
                    (x[5] ? '-' + x[5] : '');
            }
        });

        phoneInput.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && e.target.value.length <= 5) {
                e.preventDefault();
            }
        });
    }
});