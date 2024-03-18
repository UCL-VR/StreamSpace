import Ubiq from "/bundle.js"

function generateMachineUUID() {
    // Generate a random UUID4
    const uuidv4 = ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    );
  
    // Remove the hyphens and extract the first 16 characters in the desired format
    const formatted = uuidv4.replace(/-/g, '').substring(0, 16);
    return formatted.substring(0, 8) + '-' + formatted.substring(8);
  }

// This creates a typical Browser WebSocket, with a wrapper that can 
// parse Ubiq messages.

// The config is downloaded before the module is dynamically imported
const config = window.ubiq.config;

const connection = new Ubiq.WebSocketConnectionWrapper(new WebSocket(`wss://${config.wss.uri}:${config.wss.port}`));

const scene = new Ubiq.NetworkScene();
scene.addConnection(connection);

// The RoomClient is used to leave and join Rooms. Rooms define which other
// Peers are in the Peer Group.

const roomClient = new Ubiq.RoomClient(scene);

roomClient.addListener("OnJoinedRoom", room => {
    console.log("Joined Room with Join Code " + room.joincode);
    document.getElementById("roomuuid").textContent = room.uuid;
    document.getElementById("roomjoincode").textContent = room.joincode;
});

// This section binds the Ubiq PeerConnection Component to the actual 
// RTCPeerConnection instance created by the browser. This is done by connecting
// the complementary APIs of the two types. Once this is done, the browser code 
// can interact with the RTCPeerConnection - adding outgoing tracks, listening
// for incoming ones - as if were any other.

const peerConnectionManager = new Ubiq.PeerConnectionManager(scene);

const nameInput = document.getElementById("name-input");
let name = nameInput.value;

// If the name is saved in browser storage, load it
const savedName = localStorage.getItem("name");
if (savedName) {
    name = savedName;
    nameInput.value = name;
}

nameInput.addEventListener("change", () => {
    name = nameInput.value;

    // Save in browser storage
    localStorage.setItem("name", name);
});

peerConnectionManager.addListener("OnPeerConnectionRemoved", component =>{
    for(let element of component.elements){
        element.remove();
    }
});

class ScreenManager {
    constructor(){ 
        this.networkId = new Ubiq.NetworkId("e12e9c92-30a45567");
        this.context = scene.register(this);
        this.streams = new Map(); // Store streams with their track IDs
        this.streamsPrivacy = new Map(); // Store stream visibility with their track IDs
        this.mid = generateMachineUUID();
    }

    sendScreenToAllPeers = async () => {
        try {
            const stream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            const track = stream.getVideoTracks()[0];
            this.streams.set(track.id, stream);
            if (!this.streamsPrivacy.has(track.id)) {
                this.streamsPrivacy.set(track.id, false);
            }
            this.context.send({ action: "add", id: track.id, mid: this.mid, name: name, isprivate: this.streamsPrivacy.get(track.id) });
            console.log("change...");
            console.log(this.streamsPrivacy);
    
            this.addVideoElement(stream, track.id);

            // Send the screen to all peers
            peerConnections.forEach(pc => {
                pc.addTrack(track);
            });
        } catch (error) {
            console.error('Error capturing screen:', error);
        }
    };

    sendPrivacyUpdate = (trackId, isPrivate) => {
        this.streamsPrivacy.set(trackId, isPrivate);
        this.context.send({ action: "privacy", id: trackId, mid: this.mid, name: "", isprivate: this.streamsPrivacy.get(trackId)});
        console.log("change...");
        console.log(this.streamsPrivacy[trackId]);
        console.log(trackId);
        console.log(this.streamsPrivacy);
        console.log("...end change");
    }

    // Loop over all streams and send them to the specified peer
    sendStreamsToPeer = async (pc) => {
        this.streams.forEach((stream, trackId) => {
            const track = stream.getVideoTracks()[0];
            this.context.send({ action: "add", id: track.id, mid: this.mid, name: name, isprivate:this.streamsPrivacy.get(trackId) });
            console.log("send...after");
            console.log(this.streamsPrivacy);

            console.log(`Sending stream ${trackId} to new peer ${pc.uuid}`);
            pc.addTrack(track);
        });
    }

    addVideoElement = (stream, trackId) => {
        const container = document.getElementById("video-container");

        const video = document.createElement("video");
        video.srcObject = stream;
        video.autoplay = true;
        video.controls = true;
        video.id = `video-${trackId}`;

        const deleteButton = document.createElement("button");
        deleteButton.textContent = "Stop Sharing";
        deleteButton.onclick = () => this.stopSharing(trackId);
        
        const privacyDropdown = document.createElement("select");
        const privateOption = document.createElement("option");
        privateOption.value = "private";
        privateOption.text = "Private";
        const publicOption = document.createElement("option");
        publicOption.value = "public";
        publicOption.text = "Public";
        privacyDropdown.appendChild(privateOption);
        privacyDropdown.appendChild(publicOption);
        privacyDropdown.value = "public";
        privacyDropdown.onchange = () => {
            const isPrivate = privacyDropdown.value === "private";
            this.sendPrivacyUpdate(trackId, isPrivate);
        };

        const videoContainer = document.createElement("div");
        videoContainer.appendChild(video);
        videoContainer.appendChild(deleteButton);
        container.appendChild(videoContainer);
        videoContainer.appendChild(privacyDropdown);
    };

    stopSharing = (trackId) => {
        const stream = this.streams.get(trackId);
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            this.streams.delete(trackId);
            this.context.send({ action: "remove", id: trackId, mid: this.mid, name: "", isprivate: false });
        }

        const videoElement = document.getElementById(`video-${trackId}`);
        videoElement?.parentNode.remove();
    };

    stopAllShares = () => {
        this.streams.forEach((stream, trackId) => {
            this.context.send({ action: "remove", id: trackId });
            stream.getTracks().forEach(track => track.stop());
        });
        this.streams.clear();
    };

    processMessage(m){}
}

const screenManager = new ScreenManager();
const peerConnections = [];

peerConnectionManager.addListener("OnPeerConnection", async component =>{
    let pc = new RTCPeerConnection({
        sdpSemantics: 'unified-plan',
    });

    peerConnections.push(pc);

    // Send previously created streams to the new peer
    screenManager.sendStreamsToPeer(pc);
    component.elements = [];

    component.makingOffer = false;
    component.ignoreOffer = false;
    component.isSettingRemoteAnswerPending = false;

    // Special handling for dotnet peers
    component.otherPeerId = undefined;

    pc.onicecandidate = ({candidate}) => component.sendIceCandidate(candidate);

    pc.onnegotiationneeded = async () => {
        try {
            component.makingOffer = true;
            await pc.setLocalDescription();
            component.sendSdp(pc.localDescription);
        } catch (err) {
            console.error(err);
        } finally {
            component.makingOffer = false;
        }
    };

    component.addListener("OnSignallingMessage", async m => {

        // Special handling for dotnet peers
        if (component.otherPeerId === undefined) {
            component.otherPeerId = m.implementation ? m.implementation : null;
            if (component.otherPeerId == "dotnet") {
                // If just one of the two peers is dotnet, the
                // non-dotnet peer always takes on the role of polite
                // peer as the dotnet implementaton isn't smart enough
                // to handle rollback
                component.polite = true;
            }
        }

        let description = m.type ? {
            type: m.type,
            sdp: m.sdp
        } : undefined;

        let candidate = m.candidate ? {
            candidate: m.candidate,
            sdpMid: m.sdpMid,
            sdpMLineIndex: m.sdpMLineIndex,
            usernameFragment: m.usernameFragment
        } : undefined;

        try {
            if (description) {
              // An offer may come in while we are busy processing SRD(answer).
              // In this case, we will be in "stable" by the time the offer is processed
              // so it is safe to chain it on our Operations Chain now.
                const readyForOffer =
                    !component.makingOffer &&
                    (pc.signalingState == "stable" || component.isSettingRemoteAnswerPending);
                const offerCollision = description.type == "offer" && !readyForOffer;

                component.ignoreOffer = !component.polite && offerCollision;
                if (component.ignoreOffer) {
                    return;
                }
                component.isSettingRemoteAnswerPending = description.type == "answer";
                await pc.setRemoteDescription(description); // SRD rolls back as needed
                component.isSettingRemoteAnswerPending = false;
                if (description.type == "offer") {
                    await pc.setLocalDescription();
                    component.sendSdp(pc.localDescription);
                }
            } else if (candidate) {
                try {
                    await pc.addIceCandidate(candidate);
                } catch (err) {
                    if (!component.ignoreOffer) throw err; // Suppress ignored offer's candidates
                }
            }
        } catch (err) {
            console.error(err);
        }
    });

    // pc.ontrack = ({track, streams}) => {
    //     switch(track.kind){
    //         case 'audio':
    //             const audioplayer = document.getElementById("audioplayer");
    //             audioplayer.srcObject = new MediaStream([track]);
    //             break;
    //         case 'video':
    //             const videoplayer = document.getElementById("videoplayer");
    //             videoplayer.srcObject = new MediaStream([track]);
    //             break;
    //     }
    // }

    pc.onconnectionstatechange = e => {
        if(e.target.connectionState === "disconnected"){
            const index = peerConnections.indexOf(pc);
            if (index > -1) {
                peerConnections.splice(index, 1);
            }
            for(let element of component.elements){
                element.remove();
            }
        }
    };
});

document.getElementById('capture-btn').addEventListener('click', async () => {
    if (!name || name === "") {
        alert("Please enter a name before capturing a screen.");
        return;
    }

    await screenManager.sendScreenToAllPeers();
});

// Before the page is unloaded, stop all screen shares so a message is sent to Unity to remove the screens
window.addEventListener('beforeunload', () => {
    screenManager.stopAllShares();
});

roomClient.join(config.room);