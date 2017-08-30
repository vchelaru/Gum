// FRB "app" provides page interactivity
var frb = (function(jQuery) {

    // create an app container
    var a = {};

    // init function binds events to the DOM
    a.init = function () {

        // show parent category
        $("li.active").parent().show();


        $("p.list_title").click(function () {
            // get the parent "li" object
            $(this).parent()

            // get all ULs belonging to parent li
            .children("ul")
            
            // toggle uls to the opposite state
            .slideToggle("slow");
        });

        $("p.secondary_list_title").click(function () {
            // get the parent "li" object
            $(this).parent()

            // get all ULs belonging to parent li
            .children("ul")
            
            // toggle uls to the opposite state
            .slideToggle("slow");
        });




    }

    // return the container
    return a;
}($));



// Bootstrap the FRB app once page has loaded
$(function () {
    frb.init();
});