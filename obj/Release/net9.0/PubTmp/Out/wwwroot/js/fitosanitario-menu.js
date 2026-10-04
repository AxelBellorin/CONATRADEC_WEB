window.conatradecFitosanitarioMenu = (() => {
    const linkClass = "fitosanitaria-dynamic-nav";
    const sectionClass = "fitosanitaria-dynamic-section";

    function normalizePath(path) {
        return (path || "").split(/[?#]/)[0].replace(/^\/+|\/+$/g, "").toLowerCase();
    }

    function createLink(href, icon, text, currentPath) {
        const link = document.createElement("a");
        const normalizedHref = normalizePath(href);
        const normalizedCurrent = normalizePath(currentPath);

        link.href = href;
        link.className = linkClass;
        if (normalizedCurrent === normalizedHref || normalizedCurrent.startsWith(`${normalizedHref}/`)) {
            link.classList.add("active");
            link.setAttribute("aria-current", "page");
        }

        const iconElement = document.createElement("i");
        iconElement.className = icon;
        const label = document.createElement("span");
        label.textContent = text;
        link.append(iconElement, label);
        return link;
    }

    function findSupervisionSection(nav) {
        return Array.from(nav.querySelectorAll(".nav-section"))
            .find(item => (item.textContent || "").trim().toLowerCase() === "supervisión agrícola");
    }

    function findInsertionAnchor(nav) {
        const labels = ["Propietarios y cobertura", "Parámetros agronómicos", "Catálogos generales", "Contenido", "Administración"];
        return Array.from(nav.querySelectorAll(".nav-section"))
            .find(item => labels.includes((item.textContent || "").trim()));
    }

    function clear(nav) {
        nav.querySelectorAll(`.${linkClass}`).forEach(item => item.remove());
        nav.querySelectorAll(`.${sectionClass}`).forEach(item => item.remove());
    }

    function apply(canInspection, canControl, canConfiguration, currentPath) {
        const nav = document.querySelector(".nav-menu");
        if (!nav) {
            return false;
        }

        clear(nav);
        if (!canInspection && !canControl && !canConfiguration) {
            return true;
        }

        let section = findSupervisionSection(nav);
        let createdSection = false;

        if (!section) {
            section = document.createElement("div");
            section.className = `nav-section ${sectionClass}`;
            section.textContent = "Supervisión agrícola";
            createdSection = true;

            const anchor = findInsertionAnchor(nav);
            if (anchor) {
                nav.insertBefore(section, anchor);
            } else {
                nav.appendChild(section);
            }
        }

        let cursor = section;
        const insert = (element) => {
            cursor.insertAdjacentElement("afterend", element);
            cursor = element;
        };

        if (canInspection) {
            insert(createLink(
                "/inspeccion-fitosanitaria",
                "fa-solid fa-leaf",
                "Inspección fitosanitaria",
                currentPath));
        }

        if (canControl) {
            insert(createLink(
                "/control-fitosanitario",
                "fa-solid fa-shield-virus",
                "Control fitosanitario",
                currentPath));
        }

        if (canConfiguration) {
            insert(createLink(
                "/configuracion/fitosanitaria",
                "fa-solid fa-sliders",
                "Configuración fitosanitaria",
                currentPath));
        }

        if (createdSection) {
            section.dataset.fitosanitaria = "true";
        }

        return true;
    }

    function sync(canInspection, canControl, canConfiguration, currentPath) {
        let attempts = 0;
        const tryApply = () => {
            attempts += 1;
            if (apply(canInspection, canControl, canConfiguration, currentPath) || attempts >= 20) {
                return;
            }
            window.setTimeout(tryApply, 50);
        };
        tryApply();
    }

    return { sync };
})();
