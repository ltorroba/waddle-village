/***

This file is part of Waddle Village Game System.

Waddle Village Game System is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Waddle Village Game System is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Waddle Village Game System.  If not, see <http://www.gnu.org/licenses/>.

***/

class core.waddle.village.api.Server extends XMLSocket {
	public static var CLASS_REF = core.waddle.village.api.Server;

	public var ip:String;
	public var port:Number;

	public var socket:XMLSocket;

	public var currDepth:Number;
	public var rooms:Array = [["Street", false], ["Town", false]];// [[name:String, loaded:Boolean]]
	public var roomHeightIndex:Number = 125;
	public var roomWidthIndex:Number = 90;

	public var movementLock:Boolean = false;
	public var setupTriggered:Boolean = false;

	public var hitTests:Number = 31;
	
	public var playerCardOpen = false;
	
	public function Server(arg_IP:String, arg_PORT:Number) {
		ip = arg_IP;
		port = arg_PORT;

		socket = new XMLSocket();
	}

	// CLIENT-DEFINED EVENTS //
	public function onMessageValidation(success:Boolean) {
	}

	public function onNewPlayer(name:String, x:Number, y:Number, privilege:Number) {
	}

	public function onNewMyPlayer(name:String, x:Number, y:Number, privilege:Number) {
	}

	public function onRemovePlayer(name:String) {
	}

	public function onRemoveMyPlayer() {
	}

	public function onPlayerMove(name:String, x:Number, y:Number, pos:Number) {
	}

	public function onMyPlayerMove(x:Number, y:Number) {
	}

	public function onPublicMessage(sender:String, msg:String, privilege:String) {
	}

	public function onConnectionLost() {
	}

	public function onError(name:String, type:String, code:String, description:String) {
	}

	public function onGameStarted() {
	}

	public function onSetup() {
	}

	public function onRoomChange(id:Number) {
	}

	public function onReleaseGame() {
	}

	public function onPlayerCommand(msg:String) {
	}
	
	public function onPlayerCard(username:String){
	}
	
	public function onMyPlayerCard(username:String){
	}
	
	public function onPlayerCardClose(){
	}
	
	public function onColourChange(username:String, colour:Number){		
	}
	
	public function onMyColourChange(colour:Number){		
	}
	
	public function onShirtChange (username:String, id:Number){
	}
	
	public function onMyShirtChange (id:Number){
	}


	// CLIENT-DEFINED FUNCTIONS //
	public function createPlayer(name:String, x:Number, y:Number, privilege:Number, myPlayer:Boolean) {
	}

	public function removePlayer(name:String) {
	}


	// SERVER COMMANDS //
	public function connect() {
		socket.connect(ip,port);
		trace("[SERVER]: Attempting connection... (IP: "+ip+", PORT: "+port+")");
	}

	public function sendPublicMessage(msg:String) {
		socket.send("<publicChat|"+msg+">");
		trace("[SERVER]: Sent public message (MSG: "+msg+")");
	}

	public function sendPosition(x:Number, y:Number, pos:Number) {
		socket.send("<move|"+x+"|"+y+"|"+pos+">");
		trace("[SERVER]: Sent position change (X: "+x+", Y: "+y+", POS: "+pos+")");
	}

	public function sendRoomChange(id:Number) {
		var tempId = id-1;

		socket.send("<changeRoom|"+tempId+">");
		onRoomChange(tempId);

		trace("[SERVER]: Sent room change (ID: "+id+", TEMP_ID: "+tempId+")");
	}

	public function sendStartGame() {
		socket.send("<startPlay>");
	}

	public function sendLoginCheck(username:String, password:String) {
		trace("[SERVER]: Sending login check (ID: "+username+")");
		socket.send("<loginCheck|"+username+"|"+password+">");
	}

	public function trySend(msg:String) {
		trace("[SERVER]: Trying to send public chat message (MSG: "+msg+")");

		// MUST ADD FILTERING IN FUTURE!
		if (msg != "") {
			if (msg.indexOf("/") != 0) {
				sendPublicMessage(msg);
			} else {
				onPlayerCommand(msg);
			}
		}
	}

	public function logout() {
		trace("[SERVER]: Sending logout request...");
		socket.send("<logout>");
	}

	// FILERING/SECURITY //
	public function checkValidity(msg:String):Boolean {
		if (msg != "") {
			return true;
		} else {
			return false;
		}
	}

	public function allRoomsLoaded():Boolean {
		for (var i = 0; i<rooms.length; i++) {
			if (!rooms[i][1]) {
				return false;
			}
		}

		trace("[SERVER]: All rooms have been loaded successfully.");
		return true;
	}

	public function roomLoadComplete(name:String) {
		for (var i = 0; i<rooms.length; i++) {
			if (rooms[i][0].toUpperCase() == name.toUpperCase()) {
				rooms[i][1] = true;
				trace("[SERVER]: Room load complete ("+name+")");
				break;
			}
		}
	}

	public function lockMovement() {
		movementLock = true;
	}

	public function releaseMovement() {
		movementLock = false;
	}

	// MATHEMATICAL CRAP //
	public function timeToLive(speed:Number, startX:Number, startY:Number, finalX:Number, finalY:Number):Number {
		var difX = finalX-startX;
		var difY = finalY-startY;

		var hypSq = Math.pow(difX, 2)+Math.pow(difY, 2);
		var hyp = Math.sqrt(hypSq);

		var seconds = hyp/speed;

		return seconds;
	}

	public function checkAngle(xmouse:Number, ymouse:Number, xsprite:Number, ysprite:Number, hstage:Number):Number {
		var startX = xmouse;
		var startY = hstage-ymouse;

		var finalX = startX-xsprite;
		var finalY = startY-(hstage-ysprite);

		var tan = Math.atan(finalY/finalX)*(180/Math.PI);

		// top; top-left; left; bottom-left; bottom; bottom-right; right; top-right; IN ASCENDING ORDER
		var position = -1;

		if (xmouse>=xsprite) {
			if (ymouse>=ysprite) {
				tan = tan*-1;
				if (tan>=22.5 && tan<=67.5) {
					position = 5;// BOTTOM-RIGHT
				} else if (tan>67.5) {
					position = 4;// BOTTOM
				} else if (tan<22.5) {
					position = 6;// RIGHT
				}
			} else {
				if (tan>67.5) {
					position = 0;// TOP
				} else if (tan>=22.5 && tan<=67.5) {
					position = 7;// TOP-RIGHT
				} else if (tan<22.5) {
					position = 6;// RIGHT
				}
			}
		} else {
			if (ymouse>=ysprite) {
				if (tan>67.5) {
					position = 4;// BOTTOM
				} else if (tan>=22.5 && tan<=67.5) {
					position = 3;// BOTTOM-LEFT
				} else if (tan<22.5) {
					position = 2;// LEFT
				}
			} else {
				tan = tan*-1;
				if (tan>67.5) {
					position = 0;// TOP
				} else if (tan>=22.5 && tan<=67.5) {
					position = 1;// TOP-LEFT
				} else if (tan<22.5) {
					position = 2;// LEFT
				}
			}
		}

		return position;
	}

	// GAME COMMANDS //
	public function setup() {
		if (!setupTriggered) {
			onSetup();
		}
	}

	public function startGame() {
		onGameStarted();
	}

	public function releaseGame() {
		onReleaseGame();
	}

	public function newMyPlayer(name:String, x:Number, y:Number, privilege:Number) {
		onNewMyPlayer(name,x,y,privilege);
	}

	public function newPlayer(name:String, x:Number, y:Number, privilege:Number) {
		onNewPlayer(name,x,y,privilege);
	}
	
	public function openPlayerCard(username:String){
		onPlayerCard(username);
	}
	
	public function openMyPlayerCard(){
		onMyPlayerCard();
	}
	
	public function closePlayerCard(){
		onPlayerCardClose();
	}
	
	public function changeColour(colour:Number){
		socket.send("<changeColour|" + colour + ">");
	}
	
	public function changeShirt (id:Number){
		socket.send("<changeShirt|" + id + ">");
	}
	
	public function kick (name:String){
		socket.send("<kick|" + name + ">");
	}
	
	public function servPrint(param:String){
		socket.send("<serv_print|" + param + ">");
	}
}




