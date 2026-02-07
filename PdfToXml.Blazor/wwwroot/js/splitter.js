function initSplitter() {
    const container = document.querySelector('.main-container');
    const leftPane = container.querySelector('.left-pane');
    const rightPane = container.querySelector('.right-pane');
    const splitter = container.querySelector('.splitter');

    let isResizing = false;

    // Restore previous width
    const savedWidth = localStorage.getItem('leftPaneWidth');
    if (savedWidth) {
        leftPane.style.width = savedWidth + 'px';
        rightPane.style.width = (container.clientWidth - savedWidth - splitter.offsetWidth) + 'px';
    }

    splitter.addEventListener('mousedown', function (e) {
        isResizing = true;
        document.body.style.cursor = 'col-resize';
    });

    document.addEventListener('mousemove', function (e) {
        if (!isResizing) return;
        const containerRect = container.getBoundingClientRect();
        let newLeftWidth = e.clientX - containerRect.left;
        if (newLeftWidth < 200) newLeftWidth = 200; // min width
        if (newLeftWidth > containerRect.width - 200) newLeftWidth = containerRect.width - 200;
        leftPane.style.width = newLeftWidth + 'px';
        rightPane.style.width = (containerRect.width - newLeftWidth - splitter.offsetWidth) + 'px';

        // Save width
        localStorage.setItem('leftPaneWidth', newLeftWidth);
    });

    document.addEventListener('mouseup', function (e) {
        if (isResizing) {
            isResizing = false;
            document.body.style.cursor = 'default';
        }
    });
}
