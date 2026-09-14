// Anasayfadaki haftalik lider tablosunun "Tüm kullanıcıları göster" butonu.
// İlk 5 satır sayfa ile birlikte gelir (sunucu tarafında render edilir); bu dosya
// yalnizca butona basilinca tam listeyi AJAX ile çeker ve bir kez cache'ler.
(function () {
    'use strict';

    const showAllBtn = document.getElementById('leaderboardShowAll');
    const fullTable = document.getElementById('leaderboardFull');
    const fullBody = document.getElementById('leaderboardFullBody');

    if (!showAllBtn || !fullTable || !fullBody) {
        return;
    }

    const DEFAULT_LABEL = 'Tüm kullanıcıları göster';
    let loaded = false;

    showAllBtn.addEventListener('click', async function () {
        if (fullTable.hidden) {
            if (!loaded) {
                showAllBtn.disabled = true;
                showAllBtn.textContent = 'Yükleniyor...';

                try {
                    const response = await fetch('/api/leaderboard/full', {
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    });

                    if (response.status === 401) {
                        window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                        return;
                    }

                    if (!response.ok) {
                        throw new Error('Liste yüklenemedi.');
                    }

                    const data = await response.json();
                    renderRows(data.entries || []);
                    loaded = true;
                } catch (error) {
                    fullBody.innerHTML = '<tr><td class="empty-state" colspan="4">' + escapeHtml(error.message) + '</td></tr>';
                    loaded = true;
                } finally {
                    showAllBtn.disabled = false;
                }
            }

            fullTable.hidden = false;
            showAllBtn.textContent = 'Gizle';
        } else {
            fullTable.hidden = true;
            showAllBtn.textContent = DEFAULT_LABEL;
        }
    });

    function rankBadge(rank) {
        if (rank === 1) return '🥇';
        if (rank === 2) return '🥈';
        if (rank === 3) return '🥉';
        return '#' + rank;
    }

    function renderRows(entries) {
        if (entries.length === 0) {
            fullBody.innerHTML = '<tr><td class="empty-state" colspan="4">Bu hafta henüz kimse kart çözmedi.</td></tr>';
            return;
        }

        const rowsHtml = entries.map(function (entry) {
            const rowClass = entry.isCurrentUser ? ' class="is-me"' : '';
            return '<tr' + rowClass + '>' +
                '<td class="leaderboard-rank">' + rankBadge(entry.rank) + '</td>' +
                '<td class="leaderboard-name">' + escapeHtml(entry.displayName) + '</td>' +
                '<td class="leaderboard-score">' + entry.weeklyCardCount + ' kart</td>' +
                '<td class="leaderboard-lastseen">' + escapeHtml(entry.lastSeen) + '</td>' +
                '</tr>';
        }).join('');

        fullBody.innerHTML = rowsHtml;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();
