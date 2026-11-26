(function (window, document) {
    'use strict';

    // Default true: format Marathi dates, times, numbers & tables with Devanagari digits (०१२३४५६७८९)
    const USE_DEVANAGARI_DIGITS = true;
    const LANG_STORAGE_KEY = 'rms_lang';
    const CONFIG_STORAGE_KEY = 'rms_restaurant_config';
    const DEV_HOST = typeof window !== 'undefined' && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

    const state = {
        lang: 'en',
        dictionaries: {
            en: {},
            mr: {}
        },
        brandConfig: null,
        listeners: []
    };

    function normalizeLang(lang) {
        return lang === 'mr' ? 'mr' : 'en';
    }

    function getStoredLanguage() {
        try {
            const saved = localStorage.getItem(LANG_STORAGE_KEY);
            if (saved === 'en' || saved === 'mr') return saved;
        } catch (e) { /* ignore */ }
        const docLang = document.documentElement.getAttribute('lang');
        return normalizeLang(docLang);
    }

    function saveLanguage(lang, syncServer) {
        const normalized = normalizeLang(lang);
        state.lang = normalized;
        try {
            localStorage.setItem(LANG_STORAGE_KEY, normalized);
        } catch (e) { /* ignore */ }
        document.cookie = `rms_lang=${normalized}; path=/; max-age=31536000; SameSite=Lax`;
        document.documentElement.setAttribute('lang', normalized);

        if (syncServer) {
            fetch('/Account/SetLanguage', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ language: normalized })
            }).catch(function () { /* non-blocking */ });
        }
    }

    function getLocaleCode() {
        if (state.lang === 'mr') {
            return USE_DEVANAGARI_DIGITS ? 'mr-IN-u-nu-deva' : 'mr-IN-u-nu-latn';
        }
        return 'en-IN';
    }

    function t(key, fallback) {
        if (!key) return fallback || '';
        const dict = state.dictionaries[state.lang] || state.dictionaries.en || {};
        if (Object.prototype.hasOwnProperty.call(dict, key)) {
            return dict[key];
        }
        if (DEV_HOST && Object.keys(dict).length > 0) {
            console.warn(`[i18n] Missing key "${key}" for language "${state.lang}"`);
        }
        const enDict = state.dictionaries.en || {};
        if (Object.prototype.hasOwnProperty.call(enDict, key)) {
            return enDict[key];
        }
        return fallback !== undefined ? fallback : key;
    }

    function formatDigits(value) {
        const str = String(value ?? '');
        if (!USE_DEVANAGARI_DIGITS || state.lang !== 'mr') {
            return str;
        }
        const devanagari = ['०', '१', '२', '३', '४', '५', '६', '७', '८', '९'];
        return str.replace(/\d/g, d => devanagari[Number(d)]);
    }

    function formatNumber(num) {
        const n = Number(num || 0);
        try {
            return new Intl.NumberFormat(getLocaleCode(), { maximumFractionDigits: 2 }).format(n);
        } catch (e) {
            return formatDigits(n.toLocaleString('en-IN'));
        }
    }

    function formatCurrency(amount) {
        return '₹' + formatNumber(amount);
    }

    function formatDateTime(dateInput, options) {
        const d = dateInput instanceof Date ? dateInput : new Date(dateInput);
        if (isNaN(d.getTime())) return '';
        const opts = options || {
            weekday: 'short',
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        };
        try {
            return new Intl.DateTimeFormat(getLocaleCode(), opts).format(d);
        } catch (e) {
            return d.toLocaleString('en-IN');
        }
    }

    function formatTime(dateInput) {
        const d = dateInput instanceof Date ? dateInput : new Date(dateInput);
        if (isNaN(d.getTime())) return '';
        try {
            return new Intl.DateTimeFormat(getLocaleCode(), {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            }).format(d);
        } catch (e) {
            return d.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
        }
    }

    function formatDate(dateInput, options) {
        const d = dateInput instanceof Date ? dateInput : new Date(dateInput);
        if (isNaN(d.getTime())) return '';
        const opts = options || { day: '2-digit', month: 'short', year: 'numeric' };
        try {
            return new Intl.DateTimeFormat(getLocaleCode(), opts).format(d);
        } catch (e) {
            return d.toLocaleDateString('en-IN');
        }
    }

    function getGreetingKey(date) {
        const hr = (date || new Date()).getHours();
        if (hr < 12) return 'greeting.morning';
        if (hr < 17) return 'greeting.afternoon';
        return 'greeting.evening';
    }

    function resolveLocalizedHotelName(config) {
        if (!config) return '';
        if (state.lang === 'mr' && config.restaurantNameMr && config.restaurantNameMr.trim()) {
            return config.restaurantNameMr.trim();
        }
        return config.restaurantName || '';
    }

    function applyBrandTexts(config) {
        if (!config) return;
        const name = resolveLocalizedHotelName(config) || 'Hotel Kumbharwada';
        const tagline = (state.lang === 'mr' && config.taglineMr && config.taglineMr.trim())
            ? config.taglineMr.trim()
            : (config.tagline || '');
        const address = (state.lang === 'mr' && config.addressMr && config.addressMr.trim())
            ? config.addressMr.trim()
            : (config.fullAddress || '');

        document.querySelectorAll('[data-brand-name]').forEach(function (el) {
            el.textContent = name;
            if (el.hasAttribute('title') || el.classList.contains('luxury-brand-title')) {
                el.setAttribute('title', name);
            }
        });

        document.querySelectorAll('[data-brand-tagline]').forEach(function (el) {
            el.textContent = tagline ? ` · ${tagline}` : '';
        });

        document.querySelectorAll('[data-brand-address]').forEach(function (el) {
            if (address) el.textContent = address;
        });

        const titleEl = document.querySelector('title[data-page-title-key]');
        if (titleEl) {
            const pageKey = titleEl.getAttribute('data-page-title-key');
            const pageTitle = pageKey ? t(pageKey) : '';
            document.title = pageTitle ? `${pageTitle} - ${name}` : name;
        }
    }

    function applyTranslations(root) {
        const container = root || document;

        container.querySelectorAll('[data-i18n]').forEach(function (el) {
            const key = el.getAttribute('data-i18n');
            if (!key) return;
            el.textContent = t(key, el.textContent);
        });

        container.querySelectorAll('[data-i18n-placeholder]').forEach(function (el) {
            const key = el.getAttribute('data-i18n-placeholder');
            if (key) el.setAttribute('placeholder', t(key, el.getAttribute('placeholder') || ''));
        });

        container.querySelectorAll('[data-i18n-title]').forEach(function (el) {
            const key = el.getAttribute('data-i18n-title');
            if (key) {
                const val = t(key, el.getAttribute('title') || '');
                el.setAttribute('title', val);
                el.setAttribute('data-tooltip', val);
                if (el.hasAttribute('data-bs-original-title')) {
                    el.setAttribute('data-bs-original-title', val);
                }
            }
        });

        container.querySelectorAll('[data-i18n-aria]').forEach(function (el) {
            const key = el.getAttribute('data-i18n-aria');
            if (key) el.setAttribute('aria-label', t(key, el.getAttribute('aria-label') || ''));
        });

        container.querySelectorAll('[data-i18n-dynamic]').forEach(function (el) {
            const enVal = el.getAttribute('data-name-en') || '';
            const mrVal = el.getAttribute('data-name-mr') || '';
            el.textContent = (state.lang === 'mr' && mrVal.trim().length > 0) ? mrVal : enVal;
        });

        container.querySelectorAll('[data-i18n-table]').forEach(function (el) {
            const num = el.getAttribute('data-table-num') || '';
            const mrName = el.getAttribute('data-name-mr') || '';
            if (state.lang === 'mr') {
                el.textContent = mrName.trim().length > 0
                    ? mrName
                    : `${t('table.prefix')} ${formatDigits(num)}`;
            } else {
                el.textContent = `${t('table.prefix')} ${num}`.trim();
            }
        });

        container.querySelectorAll('[data-i18n-greeting]').forEach(function (el) {
            const userEn = el.getAttribute('data-user-en') || el.getAttribute('data-user') || '';
            const userMr = el.getAttribute('data-user-mr') || '';
            const user = (state.lang === 'mr' && userMr.trim()) ? userMr : userEn;
            const greet = t(getGreetingKey(new Date()));
            el.textContent = user ? `${greet}, ${user}` : greet;
        });

        container.querySelectorAll('[data-i18n-role]').forEach(function (el) {
            const rawRole = (el.getAttribute('data-i18n-role') || '').toLowerCase().replace(/\s+/g, '');
            const key = 'role.' + rawRole;
            const enDict = state.dictionaries.en || {};
            if (Object.prototype.hasOwnProperty.call(enDict, key)) {
                el.textContent = t(key);
            }
        });

        container.querySelectorAll('[data-locale-number]').forEach(function (el) {
            el.textContent = formatNumber(el.getAttribute('data-locale-number'));
        });

        container.querySelectorAll('[data-locale-currency]').forEach(function (el) {
            el.textContent = formatCurrency(el.getAttribute('data-locale-currency'));
        });

        container.querySelectorAll('[data-locale-digits]').forEach(function (el) {
            el.textContent = formatDigits(el.getAttribute('data-locale-digits'));
        });

        container.querySelectorAll('[data-locale-time]').forEach(function (el) {
            const iso = el.getAttribute('data-locale-time');
            if (iso) el.textContent = formatTime(iso);
        });

        container.querySelectorAll('[data-locale-datetime]').forEach(function (el) {
            const iso = el.getAttribute('data-locale-datetime');
            if (iso) el.textContent = formatDateTime(iso);
        });

        container.querySelectorAll('[data-locale-date]').forEach(function (el) {
            const iso = el.getAttribute('data-locale-date');
            if (iso) el.textContent = formatDate(iso);
        });

        container.querySelectorAll('.lang-switch-btn').forEach(function (btn) {
            const btnLang = btn.getAttribute('data-lang');
            const isActive = btnLang === state.lang;
            btn.classList.toggle('active', isActive);
            btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
        });

        if (state.brandConfig) {
            applyBrandTexts(state.brandConfig);
        }
    }

    function setLanguage(lang, syncServer = true) {
        saveLanguage(lang, syncServer);
        applyTranslations(document);
        state.listeners.forEach(function (cb) {
            try { cb(state.lang); } catch (e) { /* isolate */ }
        });
        document.dispatchEvent(new CustomEvent('rms:languageChanged', { detail: { lang: state.lang } }));
    }

    function onLanguageChange(cb) {
        if (typeof cb === 'function') {
            state.listeners.push(cb);
        }
    }

    function init(enDict, mrDict, serverPreferredLang) {
        if (enDict) state.dictionaries.en = enDict;
        if (mrDict) state.dictionaries.mr = mrDict;

        if (DEV_HOST) {
            const enKeys = Object.keys(state.dictionaries.en || {});
            const mrDictObj = state.dictionaries.mr || {};
            const missing = enKeys.filter(k => !mrDictObj[k]);
            if (missing.length > 0) {
                console.warn('[i18n] Missing keys in mr.json:', missing);
            }
        }

        let initialLang = getStoredLanguage();
        if (!localStorage.getItem(LANG_STORAGE_KEY) && (serverPreferredLang === 'en' || serverPreferredLang === 'mr')) {
            initialLang = serverPreferredLang;
        }
        saveLanguage(initialLang, false);
        applyTranslations(document);
    }

    function showToast(message, type) {
        const container = document.getElementById('rmsToastContainer');
        if (!container) return;

        const isError = type === 'error' || type === 'danger';
        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center border-0 shadow-lg mb-2 ${isError ? 'text-bg-danger' : 'rms-toast-success'}`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        const inner = document.createElement('div');
        inner.className = 'd-flex align-items-center';

        const body = document.createElement('div');
        body.className = 'toast-body fw-semibold d-flex align-items-center gap-2';
        body.innerHTML = `<i class="fa-solid ${isError ? 'fa-circle-exclamation' : 'fa-circle-check'}"></i><span></span>`;
        body.querySelector('span').textContent = message;

        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn-close btn-close-white me-2 m-auto';
        closeBtn.setAttribute('data-bs-dismiss', 'toast');
        closeBtn.setAttribute('aria-label', 'Close');

        inner.appendChild(body);
        inner.appendChild(closeBtn);
        toastEl.appendChild(inner);
        container.appendChild(toastEl);

        if (window.bootstrap && window.bootstrap.Toast) {
            const toast = new window.bootstrap.Toast(toastEl, { delay: 3500 });
            toastEl.addEventListener('hidden.bs.toast', function () {
                toastEl.remove();
            });
            toast.show();
        } else {
            setTimeout(function () { toastEl.remove(); }, 3500);
        }
    }

    function applyRestaurantBranding(config) {
        if (!config || !config.restaurantName) return;
        state.brandConfig = config;
        try {
            localStorage.setItem(CONFIG_STORAGE_KEY, JSON.stringify(config));
        } catch (e) { /* ignore */ }

        const name = resolveLocalizedHotelName(config) || config.restaurantName;
        const logoUrl = config.logoUrl || '';
        const initial = name.trim().charAt(0).toUpperCase() || 'H';

        applyBrandTexts(config);

        document.querySelectorAll('[data-brand-logo-wrap]').forEach(function (wrap) {
            const img = wrap.querySelector('[data-brand-logo-img]');
            const fallback = wrap.querySelector('[data-brand-logo-fallback]');
            if (fallback) fallback.textContent = initial;
            if (img && fallback) {
                if (logoUrl) {
                    img.src = logoUrl;
                    img.classList.remove('d-none');
                    fallback.classList.add('d-none');
                } else {
                    img.removeAttribute('src');
                    img.classList.add('d-none');
                    fallback.classList.remove('d-none');
                }
            }
        });

        const favicon = document.getElementById('appFavicon');
        if (favicon) {
            favicon.href = logoUrl || '/assets/hotel-logo.jpg';
        }
    }

    window.I18n = {
        USE_DEVANAGARI_DIGITS: USE_DEVANAGARI_DIGITS,
        get lang() { return state.lang; },
        init: init,
        t: t,
        setLanguage: setLanguage,
        applyTranslations: applyTranslations,
        onLanguageChange: onLanguageChange,
        getLocaleCode: getLocaleCode,
        formatDigits: formatDigits,
        formatNumber: formatNumber,
        formatCurrency: formatCurrency,
        formatDateTime: formatDateTime,
        formatTime: formatTime,
        formatDate: formatDate,
        getGreetingKey: getGreetingKey
    };

    window.RmsUI = {
        showToast: showToast,
        applyRestaurantBranding: applyRestaurantBranding,
        CONFIG_STORAGE_KEY: CONFIG_STORAGE_KEY
    };
})(window, document);
