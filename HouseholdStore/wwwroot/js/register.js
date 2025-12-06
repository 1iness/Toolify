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

    const phoneInput = document.getElementById('phone');
    if (phoneInput) {
        if (!phoneInput.value.trim()) {
            phoneInput.value = '+375 (';
        }

        phoneInput.addEventListener('input', function () {
            let value = this.value;

            const cursorPos = this.selectionStart;
            const digits = value.replace(/\D/g, '');

            let formatted = '+375 (';
            if (digits.length > 3) {
                formatted += digits.substring(3, 5);

                if (digits.length >= 5) {
                    formatted += ') ' + digits.substring(5, 8);

                    if (digits.length >= 8) {
                        formatted += '-' + digits.substring(8, 10);

                        if (digits.length >= 10) {
                            formatted += '-' + digits.substring(10, 12);
                        }
                    }
                }
            } else if (digits.length > 3) {
                formatted += digits.substring(3);
            }

            if (formatted.length > 19) {
                formatted = formatted.substring(0, 19);
            }

            this.value = formatted;

            setTimeout(() => {
                this.setSelectionRange(formatted.length, formatted.length);
            }, 0);
        });
        phoneInput.addEventListener('keydown', function (e) {
            if (this.selectionStart <= 6 && (e.key === 'Backspace' || e.key === 'Delete')) {
                e.preventDefault();
                setTimeout(() => {
                    this.setSelectionRange(this.value.length, this.value.length);
                }, 0);
            }
        });
    }
});