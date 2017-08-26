var frb = (function(jQuery) {

    var a = {};

    a.init = function () {
        $("li.top_nav").children("ul").slideToggle("fast");

        $("li.top_nav").click(function () {
            $(this).children("ul").slideToggle("slow");
        });
    }

    return a;
}($));

$(function () {
    frb.init();
});