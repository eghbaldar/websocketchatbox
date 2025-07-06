let socket;
let authenticated = false;
let currentFullname = "";
let currentHeadshot = "";

window.addEventListener("DOMContentLoaded", () => {
    connect(); // Automatically try connecting once page loads
});

function connect() {
    socket = new WebSocket((location.protocol === "https:" ? "wss" : "ws") + "://" + location.host + "/ws");

    socket.onopen = () => {
        // Nothing to send — we expect the server to authenticate using cookie
    };

    socket.onmessage = (event) => {
        const data = event.data;
        // ✅ 1. Authenticated
        if (data.startsWith("Authenticated")) {

            const parts = data.split(":");
            if (parts.length >= 3) {
                currentUsername = parts[1].trim();
                currentFullname = parts[2].trim();
                currentHeadshot = parts[3].trim();
                $("#whoami").html(`<b>${currentFullname}</b><br/><small>@${currentUsername}</small>`);
            }

            const Toast = Swal.mixin({
                toast: true,
                position: "top-end",
                showConfirmButton: false,
                timer: 3000,
                timerProgressBar: true,
                didOpen: (toast) => {
                    toast.onmouseenter = Swal.stopTimer;
                    toast.onmouseleave = Swal.resumeTimer;
                }
            });
            Toast.fire({
                icon: "success",
                title: "Authentication succeeded!"
            });

            authenticated = true;

            loadFriends(currentUsername);
        }

        // ✅ 2. Typing event (must come before general 'authenticated' check)
        else if (data.includes("typing")) {

            try {
                const jsonPart = data.split(/:(.+)/)[1].trim();
                const username = data.split(/:(.+)/)[0].trim();
                const typingData = JSON.parse(jsonPart);

                if (!typingData.status) {
                    typing("");
                    document.title = `Benal Messanger!`;
                } else {
                    typing(`<b style='color:red;'>@${username}</b> is typing...`);
                    document.title = `${username} is typing...`;
                }
            } catch (err) {
                console.error("Invalid typing format", err);
            }
        }

        // ❌ 3. Authentication failure
        else if (data.includes("Authentication failed") || data.includes("Invalid")) {
            socket.close();
            leaving();
        }

        // ✅ 4. Any other messages
        else if (authenticated) {
            const lastColonIndex = data.lastIndexOf(":");
            const firstColonIndex = data.indexOf(":");

            const sender = data.substring(0, firstColonIndex).trim();
            const headshot = data.substring(lastColonIndex + 1).trim();
            const message = data.substring(firstColonIndex + 1, lastColonIndex).trim();

            const target = sender === currentUsername ? selectedFriend : sender;

            addMessageToStore(target, `${sender}: ${message}`, headshot);

            if (selectedFriend === target) {
                renderChat(target);
            }
        }
    };

    socket.onclose = () => {
        leaving();
    };

    socket.onerror = () => {
        Swal.fire("⚠️ WebSocket error");
    };
}
function leaving() {
    let timerInterval;
    Swal.fire({
        title: "Disconnected!",
        html: "you are leaving the room! <b></b> milliseconds.",
        timer: 2000,
        timerProgressBar: true,
        didOpen: () => {
            Swal.showLoading();
            const timer = Swal.getPopup().querySelector("b");
            timerInterval = setInterval(() => {
                timer.textContent = `${Swal.getTimerLeft()}`;
            }, 100);
        },
        willClose: () => {
            clearInterval(timerInterval);
        }
    }).then((result) => {
        /* Read more about handling dismissals below */
        if (result.dismiss === Swal.DismissReason.timer) {
            window.location = "/auth";
        }
    });
}
function sendMessage() {
    if (!authenticated || !selectedFriend) {
        Swal.fire("Select a friend to chat with.");
        return;
    }

    const message = document.getElementById("chatMessage").value;
    if (!message) return;

    const payload = {
        type: "chat",
        to: selectedFriend,
        message: message
    };
    socket.send(JSON.stringify(payload));

    (async () => {
        const result = await InsertMessage(currentUsername, selectedFriend, message);
        if (result) {
            document.getElementById("chatMessage").value = "";
        } else {
            Swal.fire("Something went wrong!")
        }
    })();
}

function typing(msg) {
    const div = document.getElementById("div_typing");
    div.innerHTML = msg;
}

let selectedFriend = null; // global variable
function loadFriends(username) {
    fetch(`/user/GetFriends?username=${username}`)
        .then(res => res.json())
        .then(data => {
            const container = document.getElementById("friendListContainer");
            container.innerHTML = "";
            var friends = data.result;

            friends.forEach(friend => {
                // 🔁 Replace hardcoded name/text with dynamic content
                const box = document.createElement("div");
                box.innerHTML = `
                    <a class="list-group-item media" style="text-decoration: none;cursor:pointer;">
                        <div class="pull-left">
                            <img src="/users_materials/headshots/${friend.friendHeadshot}" alt="" class="img-avatar">
                        </div>
                        <div class="media-body">
                            <small class="list-group-item-heading">${friend.friendName}</small>
                            <br/><b>@${friend.friendUsername}</b>
                        </div>
                    </a>
            `.trim();

                // 👇 Handle friend selection
                box.addEventListener("click", () => {
                    selectedFriend = friend.friendUsername;
                    $("#selectedFriendNameContext").attr("class", "friendbox");
                    $("#selectedFriendName").html(`${friend.friendName}<br/><b>@${friend.friendUsername}</b>`);
                    $('.ms-menu').removeClass('toggled');
                    GetMessages(currentUsername, friend.friendUsername)
                });

                container.appendChild(box);
            });
        });
}

// load messages
function GetMessages(currentUsername, friendUsername) {
    fetch(`/user/GetMessages?currentUsername=${currentUsername}&friendUsername=${friendUsername}`)
        .then(res => res.json())
        .then(data => {
            //alert(JSON.stringify(data));
            const formatted = data.result.map(m => ({
                text: `${m.senderUsername}: ${m.message}`,
                time: new Date(m.insertDate).toLocaleString(), // or just m.timestamp if formatted already
                headshot: m.senderHeadshot,
            }));

            // Optionally store in local memory
            messageStore[friendUsername] = formatted;
            renderChat(friendUsername, formatted);
        });
}
//

let typingTimeout;
const TYPING_TIMER_LENGTH = 3000; // 3 seconds no typing = stop

const chatInput = document.getElementById("chatMessage");
chatInput.addEventListener("input", () => {
    if (!authenticated) return;
    sendTypingStatus(true);
    clearTimeout(typingTimeout);
    typingTimeout = setTimeout(() => {
        sendTypingStatus(false);
    }, TYPING_TIMER_LENGTH);
});

function sendTypingStatus(isTyping) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        const typingPayload = JSON.stringify({ type: "typing", status: isTyping });
        socket.send(typingPayload);
    }
}

//🧠 3. Manage Chat History per Friend
const messageStore = {}; // { friendUsername: [message1, message2, ...] }

function addMessageToStore(friend, message, headshot) {
    if (!messageStore[friend]) {
        messageStore[friend] = [];
    }
    messageStore[friend].push({
        text: message,
        time: new Date().toLocaleString(), // Store message time
        headshot: headshot
    });
}

function leftSide(message, time, headshot) {
    return `<div class="message-feed media">
        <div class="pull-left">
            <img src="/users_materials/headshots/${headshot}" alt="" class="img-avatar">
        </div>
        <div class="media-body">
            <div class="mf-content">${message}</div>
            <small class="mf-date">${time} <svg xmlns="http://www.w3.org/2000/svg" width="8" height="8" fill="currentColor" class="bi bi-clock" viewBox="0 0 16 16">
  <path d="M8 3.5a.5.5 0 0 0-1 0V9a.5.5 0 0 0 .252.434l3.5 2a.5.5 0 0 0 .496-.868L8 8.71z"/>
  <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16m7-8A7 7 0 1 1 1 8a7 7 0 0 1 14 0"/>
</svg></small>
        </div>
    </div>`.trim();
}
//
function rightSide(message, time, headshot) {
    return `<div class="message-feed right">
        <div class="pull-right">
            <img src="/users_materials/headshots/${headshot}" alt="" class="img-avatar">
        </div>
        <div class="media-body">
            <div class="mf-content ">${message}</div>
            <small class="mf-date">${time} <svg xmlns="http://www.w3.org/2000/svg" width="8" height="8" fill="currentColor" class="bi bi-clock" viewBox="0 0 16 16">
  <path d="M8 3.5a.5.5 0 0 0-1 0V9a.5.5 0 0 0 .252.434l3.5 2a.5.5 0 0 0 .496-.868L8 8.71z"/>
  <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16m7-8A7 7 0 1 1 1 8a7 7 0 0 1 14 0"/>
</svg></small>
        </div>
    </div>`.trim();
}


function renderChat(friend, externalMessages = null) {
    const chatWindow = document.getElementById("chatWindow");
    chatWindow.innerHTML = "";

    const messages = externalMessages || messageStore[friend] || [];

    messages.forEach(msgObj => {
        const msg = msgObj.text;
        const time = msgObj.time;
        const headshot = msgObj.headshot;

        const firstColonIndex = msg.indexOf(":");
        const sender = msg.substring(0, firstColonIndex).trim();
        const content = msg.substring(firstColonIndex + 1).trim();

        const isMine = sender === currentUsername;
        const html = isMine ? rightSide(content, time, headshot) : leftSide(content, time, headshot);

        const wrapper = document.createElement("div");
        wrapper.innerHTML = html;
        chatWindow.appendChild(wrapper.firstElementChild);
    });

    chatWindow.scrollTop = chatWindow.scrollHeight;
}


async function InsertMessage(currentUsername, friendUsername, message) {
    try {
        const response = await $.ajax({
            url: "/user/PostMessage",
            type: "POST",
            data: {
                SenderUsername: currentUsername,
                RecieverUsername: friendUsername,
                Message: message,
            }
        });
        return response.isSuccess;
    } catch (error) {
        return false;
    }
}
async function addFriend() {
    Swal.fire({
        title: "Enter your friend USERNAME",
        input: "text",
        inputAttributes: {
            autocapitalize: "off"
        },
        showCancelButton: true,
        confirmButtonText: "Look up",
        showLoaderOnConfirm: true,
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/user/PostFriend",
                type: "POST",
                data: {
                    'currentUsername': currentUsername,
                    'friendUsername': result.value,
                },
                success: function (e) {
                    if (e.isSuccess) {
                        loadFriends(currentUsername);
                        const Toast = Swal.mixin({
                            toast: true,
                            position: "top-end",
                            showConfirmButton: false,
                            timer: 3000,
                            timerProgressBar: true,
                            didOpen: (toast) => {
                                toast.onmouseenter = Swal.stopTimer;
                                toast.onmouseleave = Swal.resumeTimer;
                            }
                        });
                        Toast.fire({
                            icon: "success",
                            title: "Your friend is added successfully!"
                        });
                    }
                    else {
                        const Toast = Swal.mixin({
                            toast: true,
                            position: "top-end",
                            showConfirmButton: false,
                            timer: 3000,
                            timerProgressBar: true,
                            didOpen: (toast) => {
                                toast.onmouseenter = Swal.stopTimer;
                                toast.onmouseleave = Swal.resumeTimer;
                            }
                        });
                        Toast.fire({
                            icon: "error",
                            title: "This friend does not exist!"
                        });
                    }
                }
            });
        }
    });
}
function logOut() {
    window.location.href = "/auth/logout"; // calls the logout endpoint and redirects
}
//// Run on load
function handleLoadLayout() {
    $("#selectedFriendNameContext").attr("class", "friendbox");
    $("#selectedFriendName").html(`<h4>Benal Messanger!</h4>`);
    $("#meHeadshot").prop("src",`/users_materials/headshots/${currentHeadshot}`);
}
window.addEventListener('load', handleLoadLayout);
//// End Run on load
document.querySelector(".chat-container")?.addEventListener("click", () => {
    $('.ms-menu').removeClass('toggled');
});
