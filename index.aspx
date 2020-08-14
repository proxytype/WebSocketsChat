<%@ Page Language="C#" AutoEventWireup="true" CodeFile="index.aspx.cs" Inherits="index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta name="viewport" content="initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0,  user-scalable=no,width=device-width" />
    <title></title>
    <link href="https://fonts.googleapis.com/css?family=Open+Sans:700" rel="stylesheet" />
    <script src="jquery.js"></script>

    <script>

        var connection = undefined;
        var ownerID = undefined;
        var sessionID = undefined;
        var name = undefined;

        $(document).ready(function () {

            center();
            openConnection();
        });

        function openConnection() {

            connection = new WebSocket("ws://127.0.0.1:777");

            connection.onopen = function () {
                console.log("Connection Success!");
            }

            connection.onerror = errorConnection;

            connection.onmessage = callbackConnection;
        }

        function errorConnection(error) {
            console.log("Websocket Error: " + error);
        }

        function callbackConnection(e) {
            var response = JSON.parse(e.data);

            if (response.isValid) {
                if (response.method == 'create' || response.method == "join") {

                    sessionID = response.payload.sessionID;
                    ownerID = response.payload.ownerID;
                    name = response.payload.name;
                    $('#p_chat_title').html(sessionID);
                    $('#p_intro').fadeOut();
                    $('#p_chat').fadeIn();
                }

                if (response.method == 'message') {
                  

                    var template = undefined;
                    if (response.payload.ownerID == ownerID) {
                        template = "<div style='width:90%; margin-left:5%; margin-top:10px; margin-bottom:5px; display:inline-block; background-color:#252569'>"
                            + "<div style='margin-left:10px;padding-top:5px'>You</div>"
                            + "<div style='width:100%;'><div style='float:right;text-align: right;; padding-right:10px; padding-left:10px; padding-bottom:10px'>" + response.payload.message + "</div></div></div>";
                    } else {
                        template = "<div style='width:90%; margin-left:5%; margin-top:10px; margin-bottom:5px; display:inline-block; background-color:#323273'>"
                            + "<div style='margin-left:10px;padding-top:5px'>" + response.payload.name + "</div>"
                            + "<div style='width:100%;'><div style='float:left; text-align: left; padding-left:10px; padding-right:10px; padding-bottom:10px '>" + response.payload.message + "</div></div></div>";
                    }

                    $('#p_chat_main').html($('#p_chat_main').html() + template);

                    $('#p_chat_main').animate({
                        scrollTop: $('#p_chat_main')[0].scrollHeight - $('#p_chat_main')[0].clientHeight
                    }, 500);
                }

                console.log(response.payload);
            }
        }

        function center() {
            $('.page').css('width', window.innerWidth).css('height', window.innerHeight).attr('width', window.innerWidth).attr('height', window.innerHeight);
            $('#p_intro_wrapper').css('margin-top', (parseInt($('#p_intro').css('height')) - parseInt($('#p_intro_wrapper').css('height'))) / 2);
            $('#p_chat_main').css('height', parseInt($('#p_chat').css('height')) - parseInt($('#p_chat_header').css('height')) - parseInt($('#p_chat_bottom').css('height')));
        }

        function createSession() {

            hideErrors();
            isValid = true;

            var nickname = $('#txb_create_nickname').val();

            if (nickname == '') {
                showError('#txb_create_nickname_error', "Nickname missing");
                isValid = false;
            }

            if (isValid) {
                if (connection != undefined && connection.readyState == 1) {
                    var request = { method: "create", payload: nickname }
                    connection.send(JSON.stringify(request));

                } else {
                    showError('#btn_create_session_error', "Connection lost...");
                }
            }
        }

        function joinSession() {

            hideErrors();
            var isValid = true;

            var session = $('#txb_join_session').val();
            var nickname = $('#txb_join_nickname').val();

            if (session == '') {
                showError('#txb_join_session_error', "Session ID missing");
                isValid = false;
            }

            if (nickname == '') {
                showError('#txb_join_nickname_error', "Nickname missing");
                isValid = false;
            }

            if (isValid) {
                var request = {
                    method: "join", payload: { name: nickname, sessionID: session }
                }
                connection.send(JSON.stringify(request));
            } else {
                showError('#btn_join_session_error', "Session not exists...");
            }

        }

        function sendChat() {

            var message = $('#txb_chat').val();
            $('#txb_chat').val('')
            if (message != '') {
                var request = {
                    method: "chat",
                    payload: { sessionID: sessionID, ownerID: ownerID, message: message, name: name }
                };

                connection.send(JSON.stringify(request));
                
            }
        }

        function showError(id, message) {
            $(id).html(message);
            $(id).css('display', 'block');
        }

        function hideErrors() {
            $('.validator').css('display', 'none');
        }


    </script>

    <style>
        .validator {
            color: red;
            font-size: 14px;
            margin-left: 5%;
        }

        .input_intro {
            margin-left: 5%;
            width: 90%;
            font-size: 24px;
            outline: 0;
            background: center bottom no-repeat;
            background-image: linear-gradient(to top,transparent 1px,#afafaf 1px);
            background-position-x: center;
            background-position-y: bottom;
            background-size: 100% 2px;
            background-repeat-x: no-repeat;
            background-repeat-y: no-repeat;
            background-attachment: initial;
            background-origin: initial;
            background-clip: initial;
            background-color: initial;
            outline: 0;
            line-height: 1;
            font-size: 24px;
            background-image: linear-gradient(to top,transparent 1px,#afafaf 1px);
            background-size: 100% 2px;
            font-size: 22px;
            font-weight: 400;
            border: none;
            border-radius: 0;
            height: 28px;
            vertical-align: middle;
            color: #0066ff;
        }

        .panel_intro {
            width: 90%;
            padding-top: 2%;
            margin-left: 5%;
            margin-bottom: 3%;
            padding-bottom: 0.5%;
            background-color: #202035;
            border-radius: 12px;
        }

        .panel_intro_btn {
            margin-left: 5%;
            width: 90%;
            height: 38px;
            font-size: 18px;
        }

        .page {
            position: absolute
        }
    </style>

</head>
<body style="background-color: #151521; font-family: 'Open Sans'; color: white; font-size: 28px; padding: 0; margin: 0">
    <form id="form1" runat="server">



        <div id="p_intro" class="page">
            <div id="p_intro_wrapper">

                <div class="panel_intro">
                    <div style="margin-left: 4%; margin-bottom: 2%">Join Session</div>
                    <div id="pnl_join">
                        <div style="margin-bottom: 5%">
                            <div>
                                <input id="txb_join_session" type="text" class="input_intro" placeholder="Session ID" tabindex="1" />
                            </div>
                            <div id="txb_join_session_error" class="validator" style="display: none"></div>
                        </div>
                        <div style="margin-bottom: 5%">
                            <div>
                                <input id="txb_join_nickname" type="text" class="input_intro" placeholder="Nickname" tabindex="2" />
                            </div>
                            <div id="txb_join_nickname_error" class="validator" style="display: none"></div>
                        </div>
                        <div style="margin-bottom: 5%">
                            <div>
                                <input id="btn_join_session" type="button" value="Join" class="panel_intro_btn" tabindex="3" onclick="joinSession(this)" />
                            </div>
                            <div id="btn_join_session_error" class="validator" style="display: none"></div>
                        </div>
                    </div>
                </div>

                <div class="panel_intro">
                    <div style="margin-left: 4%; margin-bottom: 2%">Create Session</div>
                    <div id="pnl_create">
                        <div style="margin-bottom: 5%">
                            <div>
                                <input id="txb_create_nickname" type="text" class="input_intro" placeholder="Nickname" tabindex="4" />
                            </div>
                            <div id="txb_create_nickname_error" class="validator" style="display: none"></div>
                        </div>
                        <div style="margin-bottom: 5%">
                            <div>
                                <input id="btn_create_session" type="button" value="Create" class="panel_intro_btn" tabindex="5" onclick="createSession(this)" />
                            </div>
                            <div id="btn_create_session_error" class="validator" style="display: none"></div>
                        </div>
                    </div>

                </div>

            </div>

        </div>

        <div id="p_chat" class="page" style="display: none">
            <div id="p_chat_header" style="background-color: #202035; height: 45px">
                <div style="margin-left: 4%; margin-bottom: 2%" id="p_chat_title">Join Session</div>
            </div>
            <div id="p_chat_main" style="font-size:16px; padding-top:1%; overflow:scroll">
            </div>
            <div id="p_chat_bottom" style="background-color: #202035; height: 45px">
                <div style="float: left; width:78%">
                    <input id="txb_chat" type="text" class="input_intro" style="width: 100%" placeholder="" tabindex="4" />
                </div>
                <div style="float: right; margin-right:1%">
                    <input id="btn_chat" type="button" value="Send" class="panel_intro_btn" tabindex="5" style="width:100%" onclick="sendChat(this)" /></div>
            </div>
        </div>

    </form>
</body>
</html>
