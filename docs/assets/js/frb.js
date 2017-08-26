var frb = (function(jQuery) {

    var a = {};

    a.init = function () {
        $(this).children("ul").hide("fast");

        $("li.top_nav").click(function () {
            $(this).children("ul").slideToggle("slow");
        });
    }

    return a;
}($));

$(function () {
    frb.init();
});