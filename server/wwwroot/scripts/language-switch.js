function switchLanguage(lang) {
    document.cookie = `.AspNetCore.Culture=c=${lang}|uic=${lang}; path=/; max-age=31536000`;
    location.reload();
}

function getCurrentLanguage() {
    const match = document.cookie.match(/\.AspNetCore\.Culture=c=([^|]+)\|/);
    return match ? match[1] : 'en-US';
}
