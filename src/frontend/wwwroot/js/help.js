window.helpFunctions = {
    toggleAnswer: function (answerId) {
        const answerElement = document.getElementById(answerId);
        if (answerElement) {
            answerElement.style.display = answerElement.style.display === "none" ? "block" : "none";
        }
    },
    scrollToSection: function (sectionId) {
        const sectionElement = document.getElementById(sectionId);
        if (sectionElement) {
            sectionElement.scrollIntoView({ behavior: 'smooth' });
        }
    }
};
