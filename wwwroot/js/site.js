// Site JavaScript

// Auto-hide alerts after 5 seconds
$(document).ready(function() {
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
});

// Confirm delete
function confirmDelete(message) {
    return confirm(message || 'Are you sure you want to delete this item?');
}

// Image preview
function previewImage(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function(e) {
            $('#imagePreview').attr('src', e.target.result).show();
        }
        reader.readAsDataURL(input.files[0]);
    }
}
