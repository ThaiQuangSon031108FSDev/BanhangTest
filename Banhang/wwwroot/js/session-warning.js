// Cảnh báo khi session sắp hết hạn
(function() {
    const SESSION_TIMEOUT = 30 * 60 * 1000; // 30 phút (khớp với Program.cs)
    const WARNING_TIME = 25 * 60 * 1000;    // Cảnh báo trước 5 phút
    
    let warningTimer, logoutTimer;
    
    function resetTimers() {
        clearTimeout(warningTimer);
        clearTimeout(logoutTimer);
        
        // Cảnh báo trước 5 phút
        warningTimer = setTimeout(() => {
            if (confirm('⚠️ Phiên làm việc của bạn sắp hết hạn!\n\nNhấn OK để tiếp tục làm việc.')) {
                // Gọi API để làm mới session
                fetch('/Account/KeepAlive', { method: 'POST' })
                    .then(() => resetTimers());
            }
        }, WARNING_TIME);
        
        // Tự động logout sau khi hết timeout
        logoutTimer = setTimeout(() => {
            alert('Phiên làm việc đã hết hạn. Bạn sẽ được đăng xuất.');
            window.location.href = '/Account/Logout';
        }, SESSION_TIMEOUT);
    }
    
    // Reset timer khi có hoạt động
    ['click', 'keypress', 'scroll', 'mousemove'].forEach(event => {
        document.addEventListener(event, () => {
            resetTimers();
        }, { once: false, passive: true });
    });
    
    // Khởi động
    resetTimers();
})();
