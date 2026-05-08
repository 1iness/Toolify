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

document.addEventListener('DOMContentLoaded', function () {
    if (typeof window.jQuery === 'undefined' || typeof jQuery.validator === 'undefined') {
        return;
    }

    const $form = $('.auth-form');
    if ($form.length === 0) {
        return;
    }

    let validator = $form.data('validator');
    if (!validator && $.validator.unobtrusive) {
        $.validator.unobtrusive.parse($form);
        validator = $form.data('validator');
    }

    if (!validator) {
        return;
    }

    validator.settings.onkeyup = function (element) {
        this.element(element);
    };
    validator.settings.onfocusout = function (element) {
        this.element(element);
    };

    ['FirstName', 'LastName', 'Password', 'ConfirmPassword'].forEach(function (fieldName) {
        const $input = $form.find('[name="' + fieldName + '"]');
        $input.on('input blur', function () {
            validator.element(this);

            if (fieldName === 'Password') {
                const confirm = $form.find('[name="ConfirmPassword"]');
                if (confirm.length > 0 && confirm.val()) {
                    validator.element(confirm[0]);
                }
            }
        });
    });
});