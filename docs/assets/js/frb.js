var frb = (function(jQuery) {

    var a = {};

    a.init = function () {
        $("li.top_nav").click(function () {
            $(this).children("ul").toggle("slow");
        });
    }

    return a;
}($));

$(function () {
    frb.init();
});