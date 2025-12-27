document.addEventListener("DOMContentLoaded", function () {
    const menuItems = document.querySelectorAll(".menu-item");
    const sections = document.querySelectorAll(".profile-section");

    menuItems.forEach(item => {
        item.addEventListener("click", function () {
            const targetTab = this.getAttribute("data-tab");

            menuItems.forEach(i => i.classList.remove("active"));
            this.classList.add("active");

            sections.forEach(section => {
                if (section.id === `tab-${targetTab}`) {
                    section.style.display = "block";
                } else {
                    section.style.display = "none";
                }
            });
        });
    });
});