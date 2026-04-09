(function () {
    const toggleBtn = document.getElementById("chatToggleBtn");
    const panel = document.getElementById("chatPanel");
    const closeBtn = document.getElementById("chatCloseBtn");
    const messagesBox = document.getElementById("chatMessages");
    const sendBtn = document.getElementById("chatSendBtn");
    const input = document.getElementById("chatMessageInput");
    const guestEmailWrap = document.getElementById("chatGuestEmailWrap");
    const guestEmailInput = document.getElementById("chatGuestEmailInput");

    if (!toggleBtn || !panel || !messagesBox || !sendBtn || !input) return;

    let loaded = false;

    const esc = (str) => (str || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");

    function renderMessages(messages) {
        if (!messages || messages.length === 0) {
            messagesBox.innerHTML = '<div class="chat-empty">Напишите ваш вопрос, и администратор ответит в этом чате.</div>';
            return;
        }

        messagesBox.innerHTML = messages.map(m => {
            const mine = m.senderType === "user";
            const cls = mine ? "mine" : "admin";
            const dt = new Date(m.createdAt).toLocaleString();
            return `<div class="chat-msg ${cls}">
                        <div class="chat-msg-text">${esc(m.messageText)}</div>
                        <div class="chat-msg-time">${dt}</div>
                    </div>`;
        }).join("");

        messagesBox.scrollTop = messagesBox.scrollHeight;
    }

    async function loadChat() {
        const resp = await fetch("/Chat/WidgetData");
        if (!resp.ok) return;
        const data = await resp.json();
        renderMessages(data.messages || []);
        if (!data.isAuthenticated && guestEmailWrap) {
            guestEmailWrap.classList.remove("d-none");
        }
        loaded = true;
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

    toggleBtn.addEventListener("click", async () => {
        panel.classList.toggle("d-none");
        if (!panel.classList.contains("d-none") && !loaded) {
            await loadChat();
        }
    });

    closeBtn?.addEventListener("click", () => panel.classList.add("d-none"));
    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });
})();
