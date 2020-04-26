$(function () {
    //Proxy created on the fly
    var chat = $.connection.mwHub;

    $.connection.hub.logging = true;

    chat.client.AddMessage = function (message) {
        if (message.includes("Done#!")) {
            $('#loady').hide();
            message = message.replace('Done#!', 'Done');
        }

        if (message.match("Error") || message.match("Failed") || message.match("Unsupported")) {
            $("#messages").prepend("<li class=\"list-group-item list-group-item-danger\"><small>" + (new Date().toISOString().toString()) + " " + message + "</small></li>");

            document.getElementById('notificationCount').className = "badge badge-danger";
            document.getElementById('liveMessages').className = "hide show";
        } else if (message.includes("Success")) {
            $("#messages").prepend("<li class=\"list-group-item list-group-item-success\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
        } else if (message.match("Discarding") || message.match("Truncating") || message.match("Warning")) {
            $("#messages").prepend("<li class=\"list-group-item list-group-item-warning\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
            document.getElementById('notificationCount').className = "badge badge-warning";
            document.getElementById('liveMessages').className = "hide show";
        } else if (message.match("Warning")) {
            $("#messages").prepend("<li class=\"list-group-item list-group-item-warning\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
            document.getElementById('notificationCount').className = "badge badge-warning";
            document.getElementById('liveMessages').className = "hide show";
        } else {
            $("#messages").prepend("<li class=\"list-group-item\"><small>" + (new Date().toLocaleTimeString()) + " " + message + "</small></li>");
        }

        document.getElementById('notificationCount').innerHTML = document.getElementById('messages').childNodes.length - 1;
    };

    $.connection.hub.start().done(function () {
        chat.server.sendMessage("SignalR connection established, connection ID: " + $.connection.hub.id);
        var input = document.getElementById('clientId');
        input.value = $.connection.hub.id;
        $('#signalRLiveMessages').show();
    });
});