window.conatradecBrowser = (() => {
    "use strict";

    const rememberedLoginKey =
        "conatradec.portal.remembered-login.v2";

    const previousRememberedLoginKey =
        "conatradec.portal.remembered-login.v1";

    const installationKey =
        "conatradec.portal.web-installation-id.v1";

    /*
     * La sesión se guarda en localStorage para que todas las pestañas del
     * mismo navegador compartan el mismo identificador. Antes se utilizaba
     * sessionStorage y cada pestaña podía aumentar el contador de sesiones.
     */
    const authenticatedSessionKey =
        "conatradec.portal.web-auth-session-id.v2";

    const previousSessionKey =
        "conatradec.portal.web-session-id.v1";

    const heartbeatState = {
        timerId: null,
        dotNetReference: null,
        running: false,
        intervalMs: 45000,
        visibilityHandler: null,
        onlineHandler: null,
        pageShowHandler: null
    };

    const locationState = {
        latitude: null,
        longitude: null,
        accuracy: null,
        timestampUtc: null,
        permissionState: "NO_SOLICITADA",
        origin: "NAVEGADOR",
        lastAttemptAt: 0
    };

    function safeGet(storage, key) {
        try {
            return storage.getItem(key);
        } catch {
            return null;
        }
    }

    function safeSet(storage, key, value) {
        try {
            storage.setItem(key, value);
            return true;
        } catch {
            return false;
        }
    }

    function safeRemove(storage, key) {
        try {
            storage.removeItem(key);
        } catch {
        }
    }

    function createUuid() {
        if (globalThis.crypto?.randomUUID) {
            return globalThis.crypto.randomUUID();
        }

        const values = new Uint8Array(16);

        if (globalThis.crypto?.getRandomValues) {
            globalThis.crypto.getRandomValues(values);
        } else {
            for (let index = 0; index < values.length; index++) {
                values[index] =
                    Math.floor(Math.random() * 256);
            }
        }

        values[6] = (values[6] & 0x0f) | 0x40;
        values[8] = (values[8] & 0x3f) | 0x80;

        const hexadecimal = Array.from(
            values,
            value => value.toString(16).padStart(2, "0")
        ).join("");

        return [
            hexadecimal.slice(0, 8),
            hexadecimal.slice(8, 12),
            hexadecimal.slice(12, 16),
            hexadecimal.slice(16, 20),
            hexadecimal.slice(20)
        ].join("-");
    }

    function getOrCreateInstallationId() {
        let value = safeGet(
            globalThis.localStorage,
            installationKey);

        if (!value) {
            value = createUuid();

            safeSet(
                globalThis.localStorage,
                installationKey,
                value);
        }

        return value;
    }

    function beginAuthenticatedSession() {
        const value = createUuid();

        safeSet(
            globalThis.localStorage,
            authenticatedSessionKey,
            value);

        /*
         * El identificador anterior vivía por pestaña. Se elimina para que
         * no vuelva a participar en los latidos.
         */
        safeRemove(
            globalThis.sessionStorage,
            previousSessionKey);

        return value;
    }

    function getOrCreateSessionId() {
        let value = safeGet(
            globalThis.localStorage,
            authenticatedSessionKey);

        if (!value) {
            value = createUuid();

            safeSet(
                globalThis.localStorage,
                authenticatedSessionKey,
                value);
        }

        return value;
    }

    function detectBrowser() {
        const userAgent = navigator.userAgent || "";

        const matchers = [
            {
                name: "Microsoft Edge",
                regex: /Edg\/([\d.]+)/
            },
            {
                name: "Google Chrome",
                regex: /Chrome\/([\d.]+)/
            },
            {
                name: "Mozilla Firefox",
                regex: /Firefox\/([\d.]+)/
            },
            {
                name: "Safari",
                regex: /Version\/([\d.]+).*Safari/
            },
            {
                name: "Opera",
                regex: /OPR\/([\d.]+)/
            }
        ];

        for (const matcher of matchers) {
            const result = userAgent.match(matcher.regex);

            if (result) {
                return {
                    name: matcher.name,
                    version: result[1] || ""
                };
            }
        }

        return {
            name: "Navegador web",
            version: ""
        };
    }

    function detectOperatingSystem() {
        const userAgent = navigator.userAgent || "";
        const platform = navigator.platform || "";

        let match = userAgent.match(
            /Windows NT ([\d.]+)/);

        if (match) {
            const versions = {
                "10.0": "10/11",
                "6.3": "8.1",
                "6.2": "8",
                "6.1": "7"
            };

            return {
                name: "Windows",
                version: versions[match[1]] || match[1]
            };
        }

        match = userAgent.match(
            /Android ([\d.]+)/);

        if (match) {
            return {
                name: "Android",
                version: match[1] || ""
            };
        }

        match = userAgent.match(
            /(?:iPhone OS|CPU OS) ([\d_]+)/);

        if (match) {
            return {
                name: "iOS/iPadOS",
                version:
                    (match[1] || "").replaceAll("_", ".")
            };
        }

        match = userAgent.match(
            /Mac OS X ([\d_]+)/);

        if (match) {
            return {
                name: "macOS",
                version:
                    (match[1] || "").replaceAll("_", ".")
            };
        }

        if (/Linux/i.test(platform) ||
            /Linux/i.test(userAgent)) {
            return {
                name: "Linux",
                version: ""
            };
        }

        return {
            name: platform || "Sistema desconocido",
            version: ""
        };
    }

    function detectDeviceType() {
        const userAgent = navigator.userAgent || "";

        const isMobile =
            navigator.userAgentData?.mobile === true ||
            /Mobi|Android|iPhone|Windows Phone/i.test(
                userAgent);

        const isTablet =
            /iPad|Tablet/i.test(userAgent) ||
            (/Android/i.test(userAgent) &&
             !/Mobi/i.test(userAgent));

        if (isTablet) {
            return "Tablet";
        }

        return isMobile
            ? "Móvil"
            : "Escritorio";
    }

    function detectConnectionType() {
        const connection =
            navigator.connection ||
            navigator.mozConnection ||
            navigator.webkitConnection;

        if (!connection) {
            return navigator.onLine
                ? "En línea"
                : "Sin conexión";
        }

        const parts = [];

        if (connection.effectiveType) {
            parts.push(connection.effectiveType);
        }

        if (Number.isFinite(connection.downlink)) {
            parts.push(
                `${connection.downlink} Mbps`);
        }

        if (connection.saveData === true) {
            parts.push("Ahorro de datos");
        }

        return parts.length > 0
            ? parts.join(" · ")
            : navigator.onLine
                ? "En línea"
                : "Sin conexión";
    }

    function getLocalTimeInfo() {
        const now = new Date();
        const hour = now.getHours();

        let greeting = "Buenas noches";

        if (hour >= 5 && hour < 12) {
            greeting = "Buenos días";
        } else if (hour >= 12 && hour < 18) {
            greeting = "Buenas tardes";
        }

        let timeZone = "";

        try {
            timeZone =
                Intl.DateTimeFormat()
                    .resolvedOptions()
                    .timeZone || "";
        } catch {
        }

        return {
            hora: hour,
            minuto: now.getMinutes(),
            zonaHoraria: timeZone,
            saludo: greeting
        };
    }

    function clearLocationCoordinates() {
        locationState.latitude = null;
        locationState.longitude = null;
        locationState.accuracy = null;
        locationState.timestampUtc = null;
    }

    function setLocationError(error) {
        clearLocationCoordinates();

        switch (error?.code) {
            case 1:
                locationState.permissionState =
                    "DENEGADO";
                break;

            case 2:
                locationState.permissionState =
                    "NO_DISPONIBLE";
                break;

            case 3:
                locationState.permissionState =
                    "TIEMPO_AGOTADO";
                break;

            default:
                locationState.permissionState =
                    "NO_DISPONIBLE";
                break;
        }
    }

    function obtainCurrentPosition() {
        return new Promise(resolve => {
            navigator.geolocation.getCurrentPosition(
                position => {
                    locationState.latitude =
                        position.coords.latitude;

                    locationState.longitude =
                        position.coords.longitude;

                    locationState.accuracy =
                        Number.isFinite(
                            position.coords.accuracy)
                                ? position.coords.accuracy
                                : null;

                    locationState.timestampUtc =
                        new Date(
                            position.timestamp ||
                            Date.now())
                            .toISOString();

                    locationState.permissionState =
                        "CONCEDIDO";

                    locationState.lastAttemptAt =
                        Date.now();

                    resolve(true);
                },
                error => {
                    setLocationError(error);

                    locationState.lastAttemptAt =
                        Date.now();

                    resolve(false);
                },
                {
                    enableHighAccuracy: true,
                    timeout: 15000,
                    maximumAge: 300000
                });
        });
    }

    async function updateLocation(requestPermission) {
        if (!globalThis.isSecureContext) {
            clearLocationCoordinates();

            locationState.permissionState =
                "REQUIERE_HTTPS";

            return false;
        }

        if (!navigator.geolocation) {
            clearLocationCoordinates();

            locationState.permissionState =
                "NO_COMPATIBLE";

            return false;
        }

        let permissionState = "prompt";

        if (navigator.permissions?.query) {
            try {
                const permission =
                    await navigator.permissions.query({
                        name: "geolocation"
                    });

                permissionState =
                    permission.state;
            } catch {
            }
        }

        if (permissionState === "denied") {
            clearLocationCoordinates();

            locationState.permissionState =
                "DENEGADO";

            return false;
        }

        if (permissionState === "prompt" &&
            !requestPermission) {
            locationState.permissionState =
                "NO_SOLICITADA";

            return false;
        }

        return await obtainCurrentPosition();
    }

    async function getPresenceContext(
        requestLocation = false) {
        const browser = detectBrowser();
        const os = detectOperatingSystem();

        const locationExpired =
            !locationState.lastAttemptAt ||
            Date.now() -
                locationState.lastAttemptAt >
                5 * 60 * 1000;

        if (requestLocation ||
            (locationExpired &&
             locationState.permissionState ===
                "CONCEDIDO")) {
            await updateLocation(
                requestLocation);
        } else if (
            locationExpired &&
            navigator.permissions?.query) {
            try {
                const permission =
                    await navigator.permissions.query({
                        name: "geolocation"
                    });

                if (permission.state === "granted") {
                    await updateLocation(false);
                } else if (
                    permission.state === "denied") {
                    clearLocationCoordinates();

                    locationState.permissionState =
                        "DENEGADO";
                }
            } catch {
            }
        }

        return {
            instalacionId:
                getOrCreateInstallationId(),

            sesionId:
                getOrCreateSessionId(),

            plataforma:
                "Web",

            tipoDispositivo:
                detectDeviceType(),

            fabricante:
                navigator.vendor ||
                "Navegador web",

            modelo:
                browser.version
                    ? `${browser.name} ${browser.version}`
                    : browser.name,

            nombreDispositivo:
                `${browser.name} en ${os.name}`,

            sistemaOperativo:
                os.name,

            versionSistema:
                os.version,

            versionApp:
                "Portal Web",

            buildApp:
                "1.0",

            idioma:
                navigator.language ||
                document.documentElement.lang ||
                "es",

            tipoConexion:
                detectConnectionType(),

            latitud:
                locationState.latitude,

            longitud:
                locationState.longitude,

            precisionMetros:
                locationState.accuracy,

            fechaUbicacionUtc:
                locationState.timestampUtc,

            origenUbicacion:
                locationState.origin,

            estadoPermisoUbicacion:
                locationState.permissionState,

            ubicacionSimulada:
                null
        };
    }

    function getRememberedLogin() {
        return (
            safeGet(
                globalThis.localStorage,
                rememberedLoginKey) ||
            safeGet(
                globalThis.localStorage,
                previousRememberedLoginKey)
        );
    }

    function setRememberedLogin(value) {
        const normalized =
            typeof value === "string"
                ? value.trim()
                : "";

        if (!normalized) {
            clearRememberedLogin();
            return;
        }

        safeSet(
            globalThis.localStorage,
            rememberedLoginKey,
            normalized);

        safeRemove(
            globalThis.localStorage,
            previousRememberedLoginKey);
    }

    function clearRememberedLogin() {
        safeRemove(
            globalThis.localStorage,
            rememberedLoginKey);

        safeRemove(
            globalThis.localStorage,
            previousRememberedLoginKey);
    }

    async function getSavedCredentials() {
        const rememberedUser =
            getRememberedLogin() || "";

        const supported =
            "credentials" in navigator &&
            typeof globalThis.PasswordCredential ===
                "function";

        if (!supported) {
            return {
                usuario: rememberedUser,
                clave: "",
                disponible: false,
                encontrada:
                    rememberedUser.length > 0
            };
        }

        try {
            const credential =
                await navigator.credentials.get({
                    password: true,
                    mediation: "optional"
                });

            if (credential &&
                credential.type === "password") {
                return {
                    usuario:
                        credential.id || rememberedUser,

                    clave:
                        credential.password || "",

                    disponible:
                        true,

                    encontrada:
                        true
                };
            }
        } catch {
        }

        return {
            usuario: rememberedUser,
            clave: "",
            disponible: true,
            encontrada:
                rememberedUser.length > 0
        };
    }

    async function saveCredentials(
        username,
        password) {
        const normalizedUser =
            typeof username === "string"
                ? username.trim()
                : "";

        const normalizedPassword =
            typeof password === "string"
                ? password
                : "";

        if (!normalizedUser ||
            !normalizedPassword) {
            return false;
        }

        setRememberedLogin(
            normalizedUser);

        if (!("credentials" in navigator) ||
            typeof globalThis.PasswordCredential !==
                "function") {
            return false;
        }

        try {
            const credential =
                new PasswordCredential({
                    id: normalizedUser,
                    name: normalizedUser,
                    password: normalizedPassword
                });

            await navigator.credentials.store(
                credential);

            return true;
        } catch {
            /*
             * Los campos conservan autocomplete=username y
             * autocomplete=current-password, por lo que el administrador
             * nativo todavía puede ofrecer guardar la contraseña.
             */
            return false;
        }
    }

    async function clearSavedCredentials() {
        clearRememberedLogin();

        if (!navigator.credentials?.preventSilentAccess) {
            return;
        }

        try {
            await navigator.credentials
                .preventSilentAccess();
        } catch {
        }
    }

    async function heartbeatTick() {
        if (heartbeatState.running ||
            !heartbeatState.dotNetReference ||
            navigator.onLine === false) {
            return;
        }

        heartbeatState.running = true;

        try {
            await heartbeatState.dotNetReference
                .invokeMethodAsync(
                    "ReportarLatidoDesdeNavegadorAsync");
        } catch {
            /*
             * Una reconexión de Blazor o el cierre de la pestaña puede
             * invalidar temporalmente la referencia.
             */
        } finally {
            heartbeatState.running = false;
        }
    }

    function stopHeartbeat() {
        if (heartbeatState.timerId !== null) {
            globalThis.clearInterval(
                heartbeatState.timerId);

            heartbeatState.timerId = null;
        }

        if (heartbeatState.visibilityHandler) {
            document.removeEventListener(
                "visibilitychange",
                heartbeatState.visibilityHandler);
        }

        if (heartbeatState.onlineHandler) {
            globalThis.removeEventListener(
                "online",
                heartbeatState.onlineHandler);
        }

        if (heartbeatState.pageShowHandler) {
            globalThis.removeEventListener(
                "pageshow",
                heartbeatState.pageShowHandler);
        }

        heartbeatState.dotNetReference = null;
        heartbeatState.running = false;
        heartbeatState.visibilityHandler = null;
        heartbeatState.onlineHandler = null;
        heartbeatState.pageShowHandler = null;
    }

    function startHeartbeat(
        dotNetReference,
        intervalMs) {
        stopHeartbeat();

        heartbeatState.dotNetReference =
            dotNetReference;

        heartbeatState.intervalMs =
            Math.max(
                30000,
                Number(intervalMs) || 45000);

        heartbeatState.visibilityHandler = () => {
            if (document.visibilityState ===
                "visible") {
                void heartbeatTick();
            }
        };

        heartbeatState.onlineHandler = () => {
            void heartbeatTick();
        };

        heartbeatState.pageShowHandler = () => {
            void heartbeatTick();
        };

        document.addEventListener(
            "visibilitychange",
            heartbeatState.visibilityHandler);

        globalThis.addEventListener(
            "online",
            heartbeatState.onlineHandler);

        globalThis.addEventListener(
            "pageshow",
            heartbeatState.pageShowHandler);

        void heartbeatTick();

        heartbeatState.timerId =
            globalThis.setInterval(
                heartbeatTick,
                heartbeatState.intervalMs);
    }

    return {
        beginAuthenticatedSession,
        getLocalTimeInfo,
        getPresenceContext,
        getRememberedLogin,
        setRememberedLogin,
        clearRememberedLogin,
        getSavedCredentials,
        saveCredentials,
        clearSavedCredentials,
        startHeartbeat,
        stopHeartbeat
    };
})();
