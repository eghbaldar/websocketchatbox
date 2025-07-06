$(function () {
    if ($('#ms-menu-trigger')[0]) {
        $('body').on('click', '#ms-menu-trigger', function () {
            $('.ms-menu').toggleClass('toggled');
        });
    }
});
/* When the user clicks on the button,
toggle between hiding and showing the dropdown content */
function myFunction() {
    document.getElementById("myDropdown").classList.toggle("show");
}

// Close the dropdown menu if the user clicks outside of it
window.onclick = function (event) {
    if (!event.target.matches('.dropbtn')) {
        var dropdowns = document.getElementsByClassName("dropdown-content");
        var i;
        for (i = 0; i < dropdowns.length; i++) {
            var openDropdown = dropdowns[i];
            if (openDropdown.classList.contains('show')) {
                openDropdown.classList.remove('show');
            }
        }
    }
}
// change textbox layoyt based on language
const input = document.getElementById("chatMessage");

input.addEventListener("input", function () {
    const val = this.value.trim();

    if (val.length === 0) {
        this.style.direction = "ltr"; // default direction
        return;
    }

    const firstChar = val[0];
    const isFarsi = /[\u0600-\u06FF]/.test(firstChar); // Persian/Arabic range

    this.style.direction = isFarsi ? "rtl" : "ltr";
});

document.getElementById('chatMessage').addEventListener('keydown', function (e) {
    // Check if Ctrl key is pressed and the key is Enter (keyCode 13 or key 'Enter')
    if (e.shiftKey && (e.keyCode === 13 || e.key === 'Enter')) {
        // Prevent the default Enter key behavior (e.g., submitting a form)
        e.preventDefault();

        // Get the current cursor position
        const start = this.selectionStart;
        const end = this.selectionEnd;

        // Insert the newline character at the cursor position
        this.value = this.value.substring(0, start) + '\n' + this.value.substring(end);

        // Reposition the cursor after the inserted newline
        this.selectionStart = this.selectionEnd = start + 1;
        return;
    }
    if (e.keyCode == 13) sendMessage();
});