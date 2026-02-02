// Site JavaScript

// Auto-hide alerts after 5 seconds
$(document).ready(function () {
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Convert all UTC dates to local timezone
    convertToLocalTime();

    // Update current time display every second
    updateCurrentTime();
    setInterval(updateCurrentTime, 1000);
});

// Confirm delete
function confirmDelete(message) {
    return confirm(message || 'Are you sure you want to delete this item?');
}

// Image preview
function previewImage(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            $('#imagePreview').attr('src', e.target.result).show();
        }
        reader.readAsDataURL(input.files[0]);
    }
}

// ==================== TIMEZONE HANDLING ====================

// Get user's timezone name
function getUserTimezone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

// Convert UTC date string to local date
function utcToLocal(utcDateString) {
    if (!utcDateString) return null;
    // Ensure the date is treated as UTC
    var date = new Date(utcDateString);
    if (isNaN(date.getTime())) {
        // Try adding 'Z' if not present to treat as UTC
        date = new Date(utcDateString + 'Z');
    }
    return date;
}

// Format date to local string
function formatLocalDate(date, format) {
    if (!date || isNaN(date.getTime())) return '';

    var options = {};

    switch (format) {
        case 'date':
            options = { year: 'numeric', month: '2-digit', day: '2-digit' };
            break;
        case 'datetime':
            options = { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
            break;
        case 'time':
            options = { hour: '2-digit', minute: '2-digit' };
            break;
        case 'full':
            options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
            break;
        case 'fulltime':
            options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' };
            break;
        default:
            options = { year: 'numeric', month: '2-digit', day: '2-digit' };
    }

    return date.toLocaleString(undefined, options);
}

// Convert all elements with data-utc-date attribute to local time
function convertToLocalTime() {
    $('[data-utc-date]').each(function () {
        var utcDate = $(this).attr('data-utc-date');
        var format = $(this).attr('data-date-format') || 'datetime';

        if (utcDate) {
            var localDate = utcToLocal(utcDate);
            if (localDate && !isNaN(localDate.getTime())) {
                $(this).text(formatLocalDate(localDate, format));
            }
        }
    });
}

// Update current time display elements
function updateCurrentTime() {
    var now = new Date();

    // Update elements with class 'current-time'
    $('.current-time').text(now.toLocaleTimeString());

    // Update elements with class 'current-date'
    $('.current-date').text(now.toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }));

    // Update elements with class 'current-datetime'
    $('.current-datetime').text(now.toLocaleString());

    // Update timezone display
    $('.user-timezone').text(getUserTimezone());
}

// Get local date/time as ISO string for form inputs
function getLocalISOString() {
    var now = new Date();
    var offset = now.getTimezoneOffset() * 60000;
    return new Date(now - offset).toISOString().slice(0, 16);
}
