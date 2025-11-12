// Confirm delete với style đẹp hơn
document.addEventListener('DOMContentLoaded', function() {
    // Tất cả các button/form có class 'confirm-delete'
    document.querySelectorAll('.confirm-delete').forEach(function(form) {
        form.addEventListener('submit', function(e) {
            if (!confirm('⚠️ Bạn có chắc chắn muốn xóa? Hành động này không thể hoàn tác!')) {
                e.preventDefault();
            }
        });
    });
    
    // Toast notification
    window.showToast = function(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        toast.style.zIndex = '9999';
        toast.textContent = message;
        document.body.appendChild(toast);
        
        setTimeout(() => toast.remove(), 3000);
    };
});
