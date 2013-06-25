// If there is an anchor specified for the frame file, try to apply it to the main page.
function onLoad() {
    if (window.location.hash != null && window.location.hash != "") {
        body.main.document.getElementsByName(window.location.hash.substring(1))[0].scrollIntoView(true);
    }
}