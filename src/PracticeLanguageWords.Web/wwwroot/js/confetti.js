/*
 * Bagimlilik gerektirmeyen kucuk konfeti efekti (canvas tabanli).
 * burst()     : dogru cevap icin kisa kutlama
 * celebrate() : 5'in katlarindaki seri kilometre taslari icin daha buyuk kutlama
 */
(function () {
    'use strict';

    const COLORS = ['#4f46e5', '#22c55e', '#f59e0b', '#ec4899', '#06b6d4'];

    let canvas = null;
    let context = null;
    let particles = [];
    let animationFrame = null;

    function ensureCanvas() {
        if (canvas) {
            return;
        }

        canvas = document.createElement('canvas');
        canvas.className = 'confetti-canvas';
        document.body.appendChild(canvas);
        context = canvas.getContext('2d');
        resize();
        window.addEventListener('resize', resize);
    }

    function resize() {
        if (!canvas) {
            return;
        }
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }

    function spawn(count, power) {
        ensureCanvas();

        const originX = window.innerWidth / 2;
        const originY = window.innerHeight / 3;

        for (let i = 0; i < count; i++) {
            particles.push({
                x: originX,
                y: originY,
                vx: (Math.random() - 0.5) * power,
                vy: (Math.random() - 1) * power,
                size: 4 + Math.random() * 6,
                color: COLORS[Math.floor(Math.random() * COLORS.length)],
                rotation: Math.random() * Math.PI,
                spin: (Math.random() - 0.5) * 0.3,
                life: 1
            });
        }

        if (!animationFrame) {
            animationFrame = window.requestAnimationFrame(tick);
        }
    }

    function tick() {
        context.clearRect(0, 0, canvas.width, canvas.height);

        particles = particles.filter(function (p) { return p.life > 0; });

        particles.forEach(function (p) {
            p.vy += 0.25;          // yercekimi
            p.x += p.vx;
            p.y += p.vy;
            p.rotation += p.spin;
            p.life -= 0.012;

            context.save();
            context.globalAlpha = Math.max(p.life, 0);
            context.translate(p.x, p.y);
            context.rotate(p.rotation);
            context.fillStyle = p.color;
            context.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
            context.restore();
        });

        if (particles.length > 0) {
            animationFrame = window.requestAnimationFrame(tick);
        } else {
            context.clearRect(0, 0, canvas.width, canvas.height);
            animationFrame = null;
        }
    }

    window.plwConfetti = {
        burst: function () { spawn(60, 12); },
        celebrate: function () { spawn(160, 18); }
    };
})();
