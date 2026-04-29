(function () {
    const launcher = document.getElementById("chatLauncher");
    const bubbleBtn = document.getElementById("chatLauncherBubble");
    const mascotBtn = document.getElementById("chatToggleBtn");
    const panel = document.getElementById("chatPanel");
    const closeBtn = document.getElementById("chatCloseBtn");
    const messagesBox = document.getElementById("chatMessages");
    const sendBtn = document.getElementById("chatSendBtn");
    const input = document.getElementById("chatMessageInput");
    const guestEmailWrap = document.getElementById("chatGuestEmailWrap");
    const guestEmailInput = document.getElementById("chatGuestEmailInput");

    if (!launcher || !panel || !messagesBox || !sendBtn || !input) return;

    let loadGeneration = 0;

    const esc = (str) => (str || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");

    function formatTime(value) {
        const dt = new Date(value);
        return dt.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
    }

    function formatDay(value) {
        const dt = new Date(value);
        return dt.toLocaleDateString("ru-RU", { day: "numeric", month: "long" });
    }

    function dayKey(value) {
        const dt = new Date(value);
        return `${dt.getFullYear()}-${dt.getMonth()}-${dt.getDate()}`;
    }

    function withLinks(escapedText) {
        const urlRegex = /(https?:\/\/[^\s<]+)/gi;
        return escapedText.replace(urlRegex, (url) =>
            `<a href="${url}" target="_blank" rel="noopener noreferrer">${url}</a>`
        );
    }

    function renderMessages(messages) {
        if (!messages || messages.length === 0) {
            messagesBox.innerHTML = '<div class="chat-empty">Напишите ваш вопрос, и администратор ответит в этом чате.</div>';
            return;
        }

        let prevDay = null;
        messagesBox.innerHTML = messages.map(m => {
            const mine = String(m.senderType || "").toLowerCase() === "user";
            const cls = mine ? "mine" : "admin";
            const at = m.createdAt ?? m.CreatedAt;
            const currentDay = dayKey(at);
            const dateDivider = currentDay !== prevDay
                ? `<div class="chat-day-divider"><span>${formatDay(at)}</span></div>`
                : "";
            prevDay = currentDay;

            const safeText = withLinks(esc((m.messageText || m.MessageText || "").trim()));
            const timeStr = formatTime(at);
            return `${dateDivider}<div class="chat-msg-row ${cls}">
                        <div class="chat-msg ${cls}">
                            <div class="chat-msg-text">${safeText}</div>
                            <div class="chat-msg-time">${timeStr}</div>
                        </div>
                    </div>`;
        }).join("");

        messagesBox.scrollTop = messagesBox.scrollHeight;
    }

    async function loadChat() {
        const gen = ++loadGeneration;
        try {
            const resp = await fetch("/Chat/WidgetData", { cache: "no-store" });
            if (gen !== loadGeneration) return;
            if (!resp.ok) return;
            const data = await resp.json();
            if (gen !== loadGeneration) return;
            const list = data.messages || data.Messages || [];
            renderMessages(Array.isArray(list) ? list : []);
            if (!data.isAuthenticated && guestEmailWrap) {
                guestEmailWrap.classList.remove("d-none");
            }
        } catch {
            if (gen === loadGeneration) {
            }
        }
    }

    async function sendMessage() {
        const text = input.value.trim();
        if (!text) return;

        sendBtn.disabled = true;
        try {
            const body = {
                messageText: text,
                guestEmail: guestEmailInput ? guestEmailInput.value.trim() : null
            };

            const resp = await fetch("/Chat/Send", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(body)
            });

            if (!resp.ok) return;
            input.value = "";
            await loadChat();
        } finally {
            sendBtn.disabled = false;
        }
    }

    async function openChatPanel() {
        panel.classList.remove("d-none");
        launcher.classList.add("d-none");
        await loadChat();
    }

    function closeChatPanel() {
        panel.classList.add("d-none");
        launcher.classList.remove("d-none");
    }

    bubbleBtn?.addEventListener("click", openChatPanel);
    mascotBtn?.addEventListener("click", openChatPanel);

    closeBtn?.addEventListener("click", closeChatPanel);
    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });
})();
