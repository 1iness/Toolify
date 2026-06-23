(function (global) {
    'use strict';

    const OPERATOR_CODES = ['25', '29', '33', '44'];
    const OPERATOR_FIRST_DIGITS = ['2', '3', '4'];
    const DIGIT_LIMIT = 12;

    function normalizeDigits(rawValue) {
        let digits = (rawValue || '').replace(/\D/g, '');

        if (digits.startsWith('80')) {
            digits = `375${digits.slice(2)}`;
        } else if (digits.startsWith('0')) {
            digits = `375${digits.slice(1)}`;
        } else if (!digits.startsWith('375')) {
            digits = `375${digits}`;
        }

        return digits.slice(0, DIGIT_LIMIT);
    }

    function sanitizeOperatorDigits(digits) {
        if (digits.length <= 3) {
            return digits;
        }

        const first = digits[3];
        if (digits.length === 4 && !OPERATOR_FIRST_DIGITS.includes(first)) {
            return digits.slice(0, 3);
        }

        if (digits.length >= 5) {
            const operator = digits.slice(3, 5);
            if (digits.length === 5) {
                const ok = OPERATOR_CODES.some((code) => code.startsWith(operator));
                if (!ok) {
                    return digits.slice(0, 4);
                }
            } else if (!OPERATOR_CODES.includes(operator)) {
                return digits.slice(0, 4);
            }
        }

        return digits;
    }

    function formatFromDigits(digits) {
        const operatorCode = digits.slice(3, 5);
        const firstPart = digits.slice(5, 8);
        const secondPart = digits.slice(8, 10);
        const thirdPart = digits.slice(10, 12);

        let formatted = '+375';
        if (operatorCode) {
            formatted += ` (${operatorCode}`;
            if (operatorCode.length === 2) {
                formatted += ')';
            }
        }
        if (firstPart) {
            formatted += ` ${firstPart}`;
        }
        if (secondPart) {
            formatted += `-${secondPart}`;
        }
        if (thirdPart) {
            formatted += `-${thirdPart}`;
        }

        return formatted;
    }

    function formatBelarusPhone(rawValue) {
        let digits = sanitizeOperatorDigits(normalizeDigits(rawValue));
        return formatFromDigits(digits);
    }

    function isCompleteBelarusPhone(rawValue) {
        const digits = normalizeDigits(rawValue);
        if (digits.length !== DIGIT_LIMIT) {
            return false;
        }

        return OPERATOR_CODES.includes(digits.slice(3, 5));
    }

    function attachBelarusPhoneInput(input, options) {
        if (!input) {
            return;
        }

        const minPrefixLength = (options && options.minPrefixLength) || 5;

        input.addEventListener('focus', function () {
            if (!this.value.trim()) {
                this.value = '+375 ';
            }
        });

        input.addEventListener('input', function () {
            this.value = formatBelarusPhone(this.value);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && this.value.length <= minPrefixLength) {
                e.preventDefault();
            }
        });

        if (input.value) {
            input.value = formatBelarusPhone(input.value);
        }
    }

    global.BelarusPhone = {
        OPERATOR_CODES,
        formatBelarusPhone,
        isCompleteBelarusPhone,
        attachBelarusPhoneInput
    };
})(window);
