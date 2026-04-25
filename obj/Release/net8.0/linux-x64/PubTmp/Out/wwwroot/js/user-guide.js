document.addEventListener('DOMContentLoaded', function () {
    const sections = document.querySelectorAll('section[id]');
    const navLinks = document.querySelectorAll('.nav-link[href^="#"]');

    window.addEventListener('scroll', function () {
        let current = '';
        sections.forEach(function (s) {
            if (window.scrollY >= s.offsetTop - 100) current = s.id;
        });
        navLinks.forEach(function (a) {
            a.classList.toggle('active', a.getAttribute('href') === '#' + current);
        });
    });
});
