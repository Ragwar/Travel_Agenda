document.addEventListener('DOMContentLoaded', function () {
    // Function to determine page type and set appropriate classes
    function setPageBackground() {
        const body = document.body;
        const currentPath = window.location.pathname.toLowerCase();

        // Remove any existing background classes
        body.classList.remove('index-page', 'other-page');

        // Remove existing right background if it exists
        const existingRightBg = document.querySelector('.right-background');
        if (existingRightBg) {
            existingRightBg.remove();
        }

        // Check if this is the index page
        if (currentPath === '/' || currentPath === '/home' || currentPath === '/home/index') {
            body.classList.add('index-page');
            console.log('Applied index-page background');
        } else {
            // All other pages get the three-zone layout
            body.classList.add('other-page');
            setupOtherPageBackground();
            console.log('Applied other-page background');
        }
    }

    // Function to set up the three-zone background for non-index pages
    function setupOtherPageBackground() {
        // Create right background element
        const rightBackground = document.createElement('div');
        rightBackground.className = 'right-background';
        document.body.appendChild(rightBackground);

        // Ensure proper z-index stacking
        const navbar = document.querySelector('.navbar');
        if (navbar) {
            navbar.style.position = 'relative';
            navbar.style.zIndex = '1000';
        }

        const main = document.querySelector('main');
        if (main) {
            main.style.position = 'relative';
            main.style.zIndex = '1';
        }

        // Ensure container has white background
        const container = document.querySelector('.container');
        if (container) {
            container.style.backgroundColor = 'white';
            container.style.position = 'relative';
            container.style.zIndex = '1';
        }
    }

    // Function to handle dynamic page changes (for SPA-like behavior)
    function handlePageChange() {
        setTimeout(() => {
            setPageBackground();
        }, 100); // Small delay to ensure DOM is updated
    }

    // Initialize background on page load
    setPageBackground();

    // Listen for navigation changes
    window.addEventListener('popstate', handlePageChange);

    // Listen for pushState/replaceState if using history API
    const originalPushState = history.pushState;
    const originalReplaceState = history.replaceState;

    history.pushState = function () {
        originalPushState.apply(history, arguments);
        handlePageChange();
    };

    history.replaceState = function () {
        originalReplaceState.apply(history, arguments);
        handlePageChange();
    };

    // Listen for link clicks to handle immediate changes
    document.addEventListener('click', function (e) {
        const link = e.target.closest('a');
        if (link && link.href && !link.href.includes('#') && !link.target) {
            // This is a navigation link, prepare for page change
            setTimeout(handlePageChange, 50);
        }
    });
});

// Export function for manual triggering if needed
window.setTravelAgendaBackground = function () {
    const event = new Event('DOMContentLoaded');
    document.dispatchEvent(event);
};