function switchLanguage(lang) {
    document.cookie = `language=${lang}; path=/; max-age=31536000`;
    location.reload();
}

function getCurrentLanguage() {
    return document.cookie.replace(/(?:(?:^|.*;\s*)language\s*\=\s*([^;]*).*$)|^.*$/, "$1") || 'en';
}
