$(function () {
    //Proxy created on the fly
    var chat = $.connection.mwHub;

    $.connection.hub.logging = true;

    $.connection.hub.start().done(function () {
        chat.server.sendMessage("SignalR connection established, connection ID: " + $.connection.hub.id);
        var input = document.getElementById('clientId');
        input.value = $.connection.hub.id;
    });

    chat.client.AddMessage = function (message) {

        if (message.includes("Done#!")) {
            $('#loady').hide();
            message = message.replace('Done#!', 'Done');
        }

        if (message.includes("Error") || message.includes("Failed") || message.includes("Unsupported")) {

            $("#messages").prepend("<li class=\"list-group-item list-group-item-danger\"><small>" + (new Date().toISOString().toString()) + " " + message + "</small></li>");

        } else if (message.includes("Success")) {

            $("#messages").prepend("<li class=\"list-group-item list-group-item-success\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
        }
        else {
            $("#messages").prepend("<li class=\"list-group-item\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
        }
    };
});