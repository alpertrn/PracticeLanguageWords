/*
 * Ortak seslendirme modulu (Web Speech API).
 * Her kelime kendi dilinin kodu ile okunur: İngilizce en-US, Almanca de-DE, Fransızca fr-FR...
 * Dil kodu veritabanindaki Languages.SpeechCode alanindan gelir.
 */
(function () {
    'use strict';

    const isSupported = 'speechSynthesis' in window;

    function pickVoice(languageCode) {
        const voices = window.speechSynthesis.getVoices();
        if (!voices || voices.length === 0) {
            return null;
        }

        const lower = languageCode.toLowerCase();

        // Once tam eslesme (de-DE), sonra sadece dil kodu (de) aranir.
        return voices.find(function (v) { return v.lang && v.lang.toLowerCase() === lower; })
            || voices.find(function (v) { return v.lang && v.lang.toLowerCase().split('-')[0] === lower.split('-')[0]; })
            || null;
    }

    function speak(text, languageCode) {
        if (!isSupported || !text) {
            return;
        }

        const lang = languageCode || 'en-US';

        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang;
        utterance.rate = 0.9;

        const voice = pickVoice(lang);
        if (voice) {
            utterance.voice = voice;
        }

        window.speechSynthesis.speak(utterance);
    }

    // Sesler bazi tarayicilarda gec yuklenir; liste hazir olunca onbellege alinir.
    if (isSupported && typeof window.speechSynthesis.getVoices === 'function') {
        window.speechSynthesis.getVoices();
        window.speechSynthesis.onvoiceschanged = function () {
            window.speechSynthesis.getVoices();
        };
    }

    /**
     * Sayfadaki tum [data-speak] butonlarini baglar.
     * Buton ornegi: <button data-speak="Reluctant" data-speech-code="en-US">🔊</button>
     * Destek yoksa butonlar gizlenir.
     */
    function bindAll(root) {
        const scope = root || document;
        const buttons = scope.querySelectorAll('[data-speak]');

        buttons.forEach(function (button) {
            if (!isSupported) {
                button.hidden = true;
                return;
            }

            if (button.dataset.speakBound === '1') {
                return;
            }

            button.dataset.speakBound = '1';
            button.addEventListener('click', function () {
                speak(button.dataset.speak, button.dataset.speechCode);
            });
        });
    }

    // --- Konusma TANIMA (kullanicinin telaffuzunu dinleme) ---------------------
    // Seviye 1: tarayicinin kendi motoru. Ucretsiz, anahtar gerekmez.
    // Chrome sesi tanima icin Google sunucusuna gonderir; Firefox desteklemez.

    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const isRecognitionSupported = Boolean(Recognition);
    let activeRecognition = null;

    /**
     * Mikrofonu acar, tek bir ifade dinler ve tanidigi metin alternatiflerini dondurur.
     * @param {string} languageCode Ornek: "en-US" - kelimenin kendi dili.
     * @param {{onStart, onResult, onError, onEnd}} handlers
     */
    function listen(languageCode, handlers) {
        if (!isRecognitionSupported) {
            handlers.onError && handlers.onError('unsupported');
            return null;
        }

        stopListening();

        const recognition = new Recognition();
        recognition.lang = languageCode || 'en-US';
        recognition.continuous = false;
        recognition.interimResults = false;
        recognition.maxAlternatives = 5; // birden fazla alternatif: tanima hatasina tolerans

        let finished = false;

        recognition.onstart = function () {
            handlers.onStart && handlers.onStart();
        };

        recognition.onresult = function (event) {
            finished = true;

            const alternatives = [];
            const result = event.results[0];

            for (let i = 0; i < result.length; i++) {
                if (result[i].transcript) {
                    alternatives.push(result[i].transcript);
                }
            }

            handlers.onResult && handlers.onResult(alternatives);
        };

        recognition.onerror = function (event) {
            finished = true;
            handlers.onError && handlers.onError(event.error || 'error');
        };

        recognition.onend = function () {
            activeRecognition = null;
            if (!finished) {
                // Ses algilanmadan kapandi (sessizlik / zaman asimi)
                handlers.onError && handlers.onError('no-speech');
            }
            handlers.onEnd && handlers.onEnd();
        };

        activeRecognition = recognition;
        recognition.start();
        return recognition;
    }

    function stopListening() {
        if (activeRecognition) {
            try { activeRecognition.abort(); } catch (e) { /* yok say */ }
            activeRecognition = null;
        }
    }

    window.plwSpeech = {
        isSupported: isSupported,
        isRecognitionSupported: isRecognitionSupported,
        speak: speak,
        bindAll: bindAll,
        listen: listen,
        stopListening: stopListening
    };

    document.addEventListener('DOMContentLoaded', function () { bindAll(document); });
})();
