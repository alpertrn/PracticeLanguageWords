(function () {
    'use strict';

    const SOURCE_CATEGORY = 0;
    const SOURCE_UNKNOWN_ONLY = 1;
    const AUTO_ADVANCE_DELAY_MS = 1500;
    const MAX_PRONUNCIATION_TRIES = 3;

    const shell = document.querySelector('.practice-shell');
    if (!shell) {
        return;
    }

    const source = parseInt(shell.dataset.source, 10);
    const categoryId = shell.dataset.categoryId ? parseInt(shell.dataset.categoryId, 10) : null;
    const languageId = shell.dataset.languageId ? parseInt(shell.dataset.languageId, 10) : null;
    const homeUrl = shell.dataset.homeUrl || '/';
    const isQuizMode = source === SOURCE_UNKNOWN_ONLY;
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const speechSupported = window.plwSpeech.isRecognitionSupported;

    const el = {
        loading: document.getElementById('cardLoading'),
        body: document.getElementById('cardBody'),
        finished: document.getElementById('cardFinished'),
        finishedText: document.getElementById('finishedText'),
        badge: document.getElementById('categoryBadge'),
        term: document.getElementById('termText'),
        read: document.getElementById('termRead'),
        listen: document.getElementById('listenButton'),
        rateButtons: document.getElementById('rateButtons'),
        form: document.getElementById('answerForm'),
        input: document.getElementById('answerInput'),
        check: document.getElementById('checkButton'),
        feedback: document.getElementById('feedback'),
        back: document.getElementById('cardBack'),
        meaning: document.getElementById('meaning'),
        hintBox: document.getElementById('hintBox'),
        hintText: document.getElementById('hintText'),
        exampleList: document.getElementById('exampleList'),
        speakBox: document.getElementById('speakBox'),
        speakPrompt: document.getElementById('speakPrompt'),
        micButton: document.getElementById('micButton'),
        micLabel: document.getElementById('micLabel'),
        speakResult: document.getElementById('speakResult'),
        masteryMeter: document.getElementById('masteryMeter'),
        masteryDots: document.getElementById('masteryDots'),
        masteryLabel: document.getElementById('masteryLabel'),
        next: document.getElementById('nextButton'),
        streakValue: document.getElementById('streakValue'),
        masteryModal: document.getElementById('masteryModal'),
        masteryModalText: document.getElementById('masteryModalText'),
        masteryYes: document.getElementById('masteryYes'),
        masteryNo: document.getElementById('masteryNo'),
        milestoneModal: document.getElementById('milestoneModal'),
        milestoneText: document.getElementById('milestoneText'),
        milestoneClose: document.getElementById('milestoneClose')
    };

    let currentWordId = null;
    let currentSpeechCode = 'en-US';
    let pronunciationTries = 0;
    let listening = false;
    let busy = false;
    let advanceTimer = null;

    // --- Yardimcilar -------------------------------------------------------

    function setBusy(value) {
        busy = value;

        if (el.rateButtons) {
            el.rateButtons.querySelectorAll('button').forEach(function (b) { b.disabled = value; });
        }
        if (el.check) { el.check.disabled = value; }
        if (el.input) { el.input.disabled = value; }
        if (el.next) { el.next.disabled = value; }
        if (el.micButton) { el.micButton.disabled = value || listening; }
    }

    async function api(url, options) {
        const config = Object.assign({ headers: {} }, options);
        config.headers['X-Requested-With'] = 'XMLHttpRequest';

        if (config.method === 'POST') {
            config.headers['Content-Type'] = 'application/json';
            if (tokenInput) {
                config.headers['X-CSRF-TOKEN'] = tokenInput.value;
            }
        }

        const response = await fetch(url, config);

        if (response.status === 401) {
            window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
            throw new Error('unauthorized');
        }

        if (!response.ok) {
            const problem = await response.json().catch(function () { return {}; });

            // Sunucu iki farkli sekilde hata dondurebiliyor: kendi middleware'imiz
            // { error: "..." }, ASP.NET'in model dogrulamasi ise ProblemDetails
            // ({ title, errors: {...} }). Ikincisinde "error" alani olmadigi icin
            // eskiden herkese "İstek başarısız oldu." gosteriliyor ve gercek sebep
            // kayboluyordu. Artik ne gelirse okunabilir hale getiriliyor.
            const validation = problem.errors
                ? Object.keys(problem.errors).map(function (k) { return problem.errors[k].join(' '); }).join(' ')
                : '';

            throw new Error(problem.error || validation || problem.title
                || ('İstek başarısız oldu (HTTP ' + response.status + ').'));
        }

        return response.json();
    }

    function showFeedback(text, kind) {
        el.feedback.textContent = text;
        el.feedback.className = 'feedback feedback-' + kind;
        el.feedback.hidden = false;
    }

    function showFinished(message) {
        el.body.hidden = true;
        el.loading.hidden = true;
        el.finishedText.textContent = message;
        el.finished.hidden = false;
    }

    function renderAnswer(answer) {
        el.meaning.textContent = answer.meaningText;

        if (answer.memoryConnection) {
            el.hintText.textContent = 'Hafıza İpucu: ' + answer.memoryConnection;
            el.hintBox.hidden = false;
        } else {
            el.hintBox.hidden = true;
        }

        renderExamples(answer.examples);

        el.back.hidden = false;
    }

    /**
     * Kelimenin 0, 1 ya da 2 ornek cumlesi olabilir (bkz. ExampleSentenceDto).
     * Her biri kendi 🔊 dinle butonuyla ve kendi "Türkçe çeviriyi göster" acip-kapama
     * duzeniyle ayri bir madde olarak listelenir.
     */
    function renderExamples(examples) {
        el.exampleList.innerHTML = '';

        if (!examples || examples.length === 0) {
            el.exampleList.hidden = true;
            return;
        }

        examples.forEach(function (example) {
            const item = document.createElement('li');
            item.className = 'example-item';

            const row = document.createElement('div');
            row.className = 'example-row';

            const sentence = document.createElement('p');
            sentence.className = 'example-en';
            sentence.textContent = '"' + example.sentence + '"';
            row.appendChild(sentence);

            if (window.plwSpeech.isSupported) {
                const listenBtn = document.createElement('button');
                listenBtn.type = 'button';
                listenBtn.className = 'btn-icon example-listen';
                listenBtn.setAttribute('aria-label', 'Cümleyi dinle');
                listenBtn.textContent = '🔊';
                listenBtn.addEventListener('click', function () {
                    window.plwSpeech.speak(example.sentence, currentSpeechCode);
                });
                row.appendChild(listenBtn);
            }

            item.appendChild(row);

            if (example.translation) {
                const toggle = document.createElement('button');
                toggle.type = 'button';
                toggle.className = 'link-button';
                toggle.textContent = '🇹🇷 Türkçe çeviriyi göster (tıkla)';

                const translation = document.createElement('p');
                translation.className = 'example-tr';
                translation.textContent = example.translation;
                translation.hidden = true;

                toggle.addEventListener('click', function () {
                    translation.hidden = !translation.hidden;
                    toggle.textContent = translation.hidden
                        ? '🇹🇷 Türkçe çeviriyi göster (tıkla)'
                        : '🇹🇷 Türkçe çeviriyi gizle';
                });

                item.appendChild(toggle);
                item.appendChild(translation);
            }

            el.exampleList.appendChild(item);
        });

        el.exampleList.hidden = false;
    }

    function renderMastery(streak, target) {
        if (!isQuizMode || !target) {
            el.masteryMeter.hidden = true;
            return;
        }

        let dots = '';
        for (let i = 0; i < target; i++) {
            dots += i < streak ? '●' : '○';
        }

        el.masteryDots.textContent = dots;
        el.masteryLabel.textContent = streak + '/' + target + ' gün doğru bildin (yaz + söyle)';
        el.masteryMeter.hidden = false;
    }

    // --- Kart akisi --------------------------------------------------------

    async function loadNextCard() {
        if (advanceTimer) {
            window.clearTimeout(advanceTimer);
            advanceTimer = null;
        }

        window.plwSpeech.stopListening();
        listening = false;
        pronunciationTries = 0;

        setBusy(true);
        el.loading.hidden = false;
        el.body.hidden = true;

        try {
            const query = new URLSearchParams({ source: String(source) });
            if (categoryId) { query.append('categoryId', String(categoryId)); }
            if (languageId) { query.append('languageId', String(languageId)); }

            const data = await api('/api/practice/next?' + query.toString(), { method: 'GET' });

            if (data.finished) {
                showFinished(data.message || (isQuizMode
                    ? 'Bilmediğin kelime kalmadı.'
                    : 'Bu kategoride çalışılacak kelime bulunamadı.'));
                return;
            }

            currentWordId = data.wordId;
            currentSpeechCode = data.speechCode || 'en-US';

            el.badge.textContent = data.categoryBadge;
            el.term.textContent = data.termText;
            el.read.textContent = data.termRead ? '/ ' + data.termRead + ' /' : '';

            el.feedback.hidden = true;
            el.speakResult.hidden = true;
            el.speakBox.hidden = true;
            el.micLabel.textContent = 'Söyle';

            if (isQuizMode) {
                // Adim 1: anlam gizli, once yaziyla cevap
                el.back.hidden = true;
                el.form.hidden = false;
                el.input.value = '';
                el.input.classList.remove('is-correct', 'is-wrong');
                el.next.hidden = true;
                renderMastery(data.masteryStreak, data.masteryTarget);
            } else {
                // Kategori pratigi: her sey gorunur, altta zorluk butonlari
                renderAnswer(data.answer);
                el.rateButtons.hidden = false;
                el.next.hidden = true;

                // Telaffuz yalnizca "bilinmeyen" isaretli kelimelerde, alistirma amacli.
                if (data.pronunciationEnabled && speechSupported) {
                    el.speakPrompt.textContent = 'Bu kelime listende — istersen telaffuzunu dene';
                    el.masteryMeter.hidden = true;
                    el.speakBox.hidden = false;
                }
            }

            el.loading.hidden = true;
            el.body.hidden = false;
            setBusy(false);

            if (isQuizMode) {
                el.input.focus();
            }
        } catch (error) {
            if (error.message !== 'unauthorized') {
                el.loading.textContent = 'Kart yüklenemedi: ' + error.message;
                el.loading.hidden = false;
            }
            setBusy(false);
        }
    }

    // --- Kategori pratigi: Kolay / Orta / Hiç Bilmiyorum --------------------

    async function rate(difficulty) {
        if (busy || currentWordId === null) {
            return;
        }

        setBusy(true);

        try {
            const result = await api('/api/practice/rate', {
                method: 'POST',
                body: JSON.stringify({
                    wordId: currentWordId,
                    difficulty: difficulty,
                    categoryId: categoryId
                })
            });

            if (result.streak) {
                el.streakValue.textContent = result.streak.currentStreak;
            }

            if (result.addedToUnknownList) {
                showFeedback('Bu kelime "Bilmediğim Kelimeler" listene eklendi.', 'info');
            }

            if (result.showMilestoneCelebration) {
                showMilestone(result.streak.currentStreak);
            }

            advanceTimer = window.setTimeout(loadNextCard, result.addedToUnknownList ? 700 : 250);
        } catch (error) {
            if (error.message !== 'unauthorized') {
                showFeedback(error.message, 'wrong');
            }
            setBusy(false);
        }
    }

    // --- Quiz adim 1: yazarak cevaplama ------------------------------------

    async function submitAnswer() {
        if (busy || currentWordId === null) {
            return;
        }

        const answerText = el.input.value.trim();
        if (answerText.length === 0) {
            el.input.focus();
            return;
        }

        setBusy(true);

        try {
            const result = await api('/api/practice/answer', {
                method: 'POST',
                body: JSON.stringify({
                    wordId: currentWordId,
                    answerText: answerText,
                    languageId: languageId,
                    speechSupported: speechSupported
                })
            });

            renderAnswer(result.answer);
            el.form.hidden = true;

            if (result.streak) {
                el.streakValue.textContent = result.streak.currentStreak;
            }

            renderMastery(result.masteryStreak, result.masteryTarget);

            if (result.showMilestoneCelebration) {
                showMilestone(result.streak.currentStreak);
            }

            setBusy(false);

            if (!result.isCorrect) {
                el.input.classList.add('is-wrong');
                showFeedback('Bu sefer olmadı. Doğru anlamı aşağıda inceleyebilirsin.', 'wrong');
                finishRound(result.noMoreUnknownWords);
                return;
            }

            el.input.classList.add('is-correct');

            if (result.requiresPronunciation) {
                // Adim 2: telaffuz
                showFeedback('Anlamı doğru! Şimdi kelimeyi sesli söyle.', 'correct');
                pronunciationTries = 0;
                el.speakPrompt.textContent = 'Kelimeyi mikrofona söyle: "' + el.term.textContent + '"';
                el.speakBox.hidden = false;
                el.micButton.focus();
                return;
            }

            // Mikrofon yok: sadece metinle ilerliyoruz
            showFeedback('Doğru! 🎉', 'correct');
            window.plwConfetti.burst();

            if (result.askMasteryPrompt) {
                openMasteryModal(result.masteryStreak, result.masteryTarget, result.noMoreUnknownWords);
                return;
            }

            advanceTimer = window.setTimeout(loadNextCard, AUTO_ADVANCE_DELAY_MS);
        } catch (error) {
            if (error.message !== 'unauthorized') {
                showFeedback(error.message, 'wrong');
            }
            setBusy(false);
        }
    }

    // --- Telaffuz ----------------------------------------------------------

    function startListening() {
        if (listening || busy || currentWordId === null) {
            return;
        }

        listening = true;
        el.micButton.disabled = true;
        el.micButton.classList.add('is-listening');
        el.micLabel.textContent = 'Dinleniyor...';
        el.speakResult.hidden = true;

        window.plwSpeech.listen(currentSpeechCode, {
            onResult: function (alternatives) { sendPronunciation(alternatives); },
            onError: function (code) {
                listening = false;
                el.micButton.classList.remove('is-listening');
                el.micButton.disabled = false;

                const messages = {
                    'no-speech': 'Ses algılanamadı, tekrar dener misin?',
                    'not-allowed': 'Mikrofon izni verilmedi. Tarayıcı ayarlarından izin verebilirsin.',
                    'audio-capture': 'Mikrofon bulunamadı.',
                    'unsupported': 'Tarayıcın konuşma tanımayı desteklemiyor.'
                };

                el.speakResult.textContent = messages[code] || 'Ses alınamadı, tekrar dene.';
                el.speakResult.className = 'speak-result speak-result-wrong';
                el.speakResult.hidden = false;

                // Izin yok / mikrofon yok / destek yok: tekrar denemek bunu duzeltmez,
                // kullaniciyi bekletmeden telaffuz adimini kapat.
                if (code === 'not-allowed' || code === 'audio-capture' || code === 'unsupported') {
                    endPronunciation('Telaffuz adımı bu cihazda kullanılamıyor.');
                    return;
                }

                // Sessizlik gibi gecici hatalar da deneme hakkindan sayilir,
                // boylece kullanici sonsuz donguye girmez.
                pronunciationTries++;

                if (pronunciationTries >= MAX_PRONUNCIATION_TRIES) {
                    endPronunciation('Telaffuzu bir sonraki turda tekrar deneyebilirsin.');
                    return;
                }

                el.micLabel.textContent = 'Tekrar dene (' + pronunciationTries + '/' + MAX_PRONUNCIATION_TRIES + ')';
            }
        });
    }

    async function sendPronunciation(alternatives) {
        // Bu denemenin sirasini ONCEDEN belirliyoruz: sunucunun "sayaci kesinlestirsin mi
        // yoksa sessizce tekrar mi denesin" kararini verebilmesi icin son hak olup olmadigini
        // bilmesi gerekiyor. Sadece quiz modunda 3 hakla sinirliyiz; kategori pratiginde
        // sinirsiz alistirma serbest (isFinalAttempt hep false gider).
        pronunciationTries++;
        const isFinalAttempt = isQuizMode && pronunciationTries >= MAX_PRONUNCIATION_TRIES;

        try {
            const result = await api('/api/practice/pronunciation', {
                method: 'POST',
                body: JSON.stringify({
                    wordId: currentWordId,
                    transcripts: alternatives,
                    source: source,
                    languageId: languageId,
                    isFinalAttempt: isFinalAttempt
                })
            });

            listening = false;
            el.micButton.classList.remove('is-listening');
            el.micButton.disabled = false;

            renderMastery(result.masteryStreak, result.masteryTarget);

            if (result.isCorrect) {
                el.micLabel.textContent = 'Tekrar söyle';
                el.speakResult.textContent = '✓ Doğru telaffuz: "' + result.bestTranscript + '"';
                el.speakResult.className = 'speak-result speak-result-correct';
                el.speakResult.hidden = false;
                window.plwConfetti.burst();

                if (!isQuizMode) {
                    return; // kategori pratiginde sadece geri bildirim; liste etkilenmez
                }

                if (result.askMasteryPrompt) {
                    openMasteryModal(result.masteryStreak, result.masteryTarget, result.noMoreUnknownWords);
                    return;
                }

                // Dogru telaffuz her zaman aninda kesinlesir: bugunku deneme basariyla bitti.
                showFeedback('Bugünlük bu kadar! Bu kelime birkaç gün içinde tekrar karşına çıkacak.', 'info');
                finishRound(result.noMoreUnknownWords);
                return;
            }

            // Yanlis telaffuz
            el.speakResult.textContent = '✗ "' + (result.bestTranscript || '...') + '" duyuldu, beklenen: "' + result.expectedTerm + '"';
            el.speakResult.className = 'speak-result speak-result-wrong';
            el.speakResult.hidden = false;

            if (isFinalAttempt) {
                // 3. hakta da tutmadi: bu gunku deneme "bilmedi" olarak kesinlesti (sayaç sıfırlandı).
                el.micLabel.textContent = 'Söyle';
                showFeedback('Bu sefer olmadı, "bilmiyorum" sayıldı. Birkaç gün içinde tekrar karşına çıkacak.', 'info');
                finishRound(result.noMoreUnknownWords);
                return;
            }

            // Kategori pratiginde deneme sinirsizdir (isFinalAttempt hep false) - "x/3" gibi
            // bir oran burada anlamsizlasip tasabilir (ör. "4/3"), o yuzden sadece quiz
            // modunda gosteriliyor.
            el.micLabel.textContent = isQuizMode
                ? 'Tekrar dene (' + pronunciationTries + '/' + MAX_PRONUNCIATION_TRIES + ')'
                : 'Tekrar dene';
        } catch (error) {
            listening = false;
            el.micButton.classList.remove('is-listening');
            el.micButton.disabled = false;
            el.micLabel.textContent = 'Tekrar dene';

            if (error.message !== 'unauthorized') {
                el.speakResult.textContent = error.message;
                el.speakResult.className = 'speak-result speak-result-wrong';
                el.speakResult.hidden = false;
            }
        }
    }

    /**
     * Telaffuz adimi TEKNIK bir sorun yuzunden (izin yok, cihaz yok, destek yok, tekrarli
     * sessizlik) kapanir - kullanicinin kelimeyi bilip bilmedigiyle ilgisi yok, o yuzden
     * basari sayacina DOKUNULMAZ. Quiz modunda yine de kelime bugun icin "gorundu"
     * isaretlenir (sunucuya postpone cagrisi) ki ayni gun tekrar cikmasin; tur biter.
     * Kategori pratiginde sadece mikrofon bolumu gizlenir, zorluk butonlari yerinde kalir.
     */
    function endPronunciation(message) {
        el.speakBox.hidden = true;

        if (message) {
            showFeedback(message, 'info');
        }

        if (isQuizMode) {
            api('/api/practice/postpone', {
                method: 'POST',
                body: JSON.stringify({ wordId: currentWordId })
            }).catch(function () { /* kritik degil, sessizce yok say */ });

            finishRound(false);
        }
    }

    /// Tur bitti: kullanici kendi ilerlesin (ya da liste bittiyse kapat).
    function finishRound(noMoreUnknownWords) {
        el.speakBox.hidden = true;
        el.next.hidden = false;
        el.next.disabled = false;

        if (noMoreUnknownWords) {
            el.next.textContent = 'Bitir';
            el.next.onclick = function () { window.location.href = homeUrl; };
        } else {
            el.next.textContent = 'Sonraki Kelime →';
            el.next.onclick = loadNextCard;
        }

        el.next.focus();
    }

    // --- "Artık kolay mı?" penceresi ---------------------------------------

    let pendingNoMoreUnknown = false;

    function openMasteryModal(streak, target, noMoreUnknownWords) {
        pendingNoMoreUnknown = noMoreUnknownWords;
        el.masteryModalText.textContent =
            '"' + el.term.textContent + '" kelimesini ' + target +
            ' farklı günde doğru yazıp doğru telaffuz ettin. Bilmediğin kelimeler listesinden çıkaralım mı?';
        el.masteryModal.hidden = false;
        window.plwConfetti.celebrate();
    }

    async function sendMasteryDecision(markAsEasy) {
        // Savunma: kart yuklenmeden bu pencere hic acilmamali. Yine de acik kalirsa
        // (ornegin bir stil hatasi yuzunden) wordId null gider ve sunucu anlamsiz bir
        // dogrulama hatasi doner. Boyle bir durumda sessizce kapat.
        if (currentWordId === null) {
            el.masteryModal.hidden = true;
            return;
        }

        el.masteryYes.disabled = true;
        el.masteryNo.disabled = true;

        try {
            const result = await api('/api/practice/mastery-decision', {
                method: 'POST',
                body: JSON.stringify({
                    wordId: currentWordId,
                    markAsEasy: markAsEasy,
                    languageId: languageId
                })
            });

            el.masteryModal.hidden = true;

            if (result.noMoreUnknownWords) {
                showFinished('Bilmediğin kelime kalmadı.');
                return;
            }

            loadNextCard();
        } catch (error) {
            if (error.message !== 'unauthorized') {
                el.masteryModalText.textContent = error.message;
            }
        } finally {
            el.masteryYes.disabled = false;
            el.masteryNo.disabled = false;
        }
    }

    function showMilestone(streakDays) {
        el.milestoneText.textContent = streakDays + ' gündür aralıksız çalışıyorsun. Böyle devam!';
        el.milestoneModal.hidden = false;
        window.plwConfetti.celebrate();
    }

    // --- Olay baglama ------------------------------------------------------

    if (el.listen) {
        if (!window.plwSpeech.isSupported) {
            el.listen.hidden = true;
        } else {
            el.listen.addEventListener('click', function () {
                window.plwSpeech.speak(el.term.textContent, currentSpeechCode);
            });
        }
    }

    if (el.rateButtons) {
        el.rateButtons.addEventListener('click', function (event) {
            const button = event.target.closest('button[data-difficulty]');
            if (button) {
                // Mobilde dokunma sonrasi buton uzerinde odak/aktif durumu
                // yapisik kalmasin diye ek onlem (asil duzeltme CSS'teki
                // (hover: hover) sarmalayicisi).
                button.blur();
                rate(parseInt(button.dataset.difficulty, 10));
            }
        });
    }

    if (el.form) {
        el.form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitAnswer();
        });
    }

    el.micButton.addEventListener('click', startListening);

    el.next.addEventListener('click', loadNextCard);

    // Not: örnek cümlelerin çeviri aç/kapa ve 🔊 dinle butonları artik dinamik olarak
    // renderExamples() icinde her madde icin ayri ayri baglaniyor (bkz. yukarisi).

    el.masteryYes.addEventListener('click', function () { sendMasteryDecision(true); });
    el.masteryNo.addEventListener('click', function () { sendMasteryDecision(false); });

    el.milestoneClose.addEventListener('click', function () {
        el.milestoneModal.hidden = true;
    });

    loadNextCard();
})();
