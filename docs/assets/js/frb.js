var frb = (function(jQuery) {

    var a = {};

    a.init = function () {
        console.log("Loading frb js app...");
    }

    return a;
}($));

$(function () {
    frb.init();
});