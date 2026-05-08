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

    function isPhoneField(element) {
        return element && element.name === 'Phone';
    }

    function isPhoneComplete(element) {
        if (!isPhoneField(element)) {
            return true;
        }

        const digits = (element.value || '').replace(/\D/g, '');
        return digits.length === 12; 
    }

    function clearFieldError(element) {
        const $element = $(element);
        const fieldName = $element.attr('name');
        if (!fieldName) {
            return;
        }

        $element.removeClass('input-validation-error');
        $element.removeAttr('aria-invalid');

        const $message = $form.find('[data-valmsg-for="' + fieldName + '"]');
        $message
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .text('');
    }

    validator.settings.onkeyup = function (element) {
        if (isPhoneField(element) && !isPhoneComplete(element)) {
            clearFieldError(element);
            return;
        }
        this.element(element);
    };

    validator.settings.onfocusout = function (element) {
        if (isPhoneField(element) && !isPhoneComplete(element)) {
            clearFieldError(element);
            return;
        }
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