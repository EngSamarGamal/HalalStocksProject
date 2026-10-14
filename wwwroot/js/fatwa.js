// "قراءة الإجابة كاملة" toggle for .fatwa-card answers (used on /fatawa and the home page).
// Safe to call repeatedly: cards already set up are skipped, so it also works for cards added later
// ("عرض المزيد") or revealed from display:none (overflow can't be measured while hidden).
(function () {
    window.HalalStocks = window.HalalStocks || {};

    function init(root) {
        (root || document).querySelectorAll('.fatwa-card').forEach(function (card) {
            if (card.dataset.fatwaInit) return;
            var answer = card.querySelector('.fatwa-answer');
            var btn = card.querySelector('.fatwa-toggle');
            if (!answer || !btn) return;
            if (answer.clientHeight === 0) return; // still hidden — retried when revealed

            card.dataset.fatwaInit = '1';
            // Only offer the toggle when the answer is taller than the clamped preview.
            if (answer.scrollHeight > answer.clientHeight + 4) { btn.classList.remove('d-none'); card.classList.add('is-clamped'); }

            btn.addEventListener('click', function () {
                var open = card.classList.toggle('is-open');
                btn.setAttribute('aria-expanded', open ? 'true' : 'false');
            });
        });
    }
    window.HalalStocks.initFatwaCards = init;

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { init(); });
    else init();
})();
