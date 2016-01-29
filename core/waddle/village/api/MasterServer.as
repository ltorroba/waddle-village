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

class core.waddle.village.api.MasterServer extends XMLSocket {
	public static var CLASS_REF = core.waddle.village.api.MasterServer;

	public var socket:XMLSocket;

	private var ip:String;
	private var port:Number;
	
	public var wrongLoginCount:Number = 0;

	private var serverList:Array = [["Feathers", null]];
	private	var serverListAccomplished = true;
	
	private var tempLogin = [null, null];

	public function MasterServer(arg_IP:String, arg_PORT:Number) {
		ip = arg_IP;
		port = arg_PORT;		
		
		socket = new XMLSocket();
	}
	
	// CLIENT-DEFINED METHODS //
	public function onError(name:String, type:String, code:String, description:String) { };
	public function onRecieveServerList (success:Boolean) { };
	public function onLostConnection () { };
	public function onKick (reason:String) { };
	public function onBan (reason:String) { };
	public function onLoginGranted () { };
	
	// API-DEFINED EVENTS //
	public function onConnectionFailed () {
		onError("Connectivity defect", "MS-Connect", "0-0002", 
				"There was a problem when trying to connect to the master server!");
	}

	// INNER METHODS //
	public function setServerLoad(name:String, load:Number) {
		for (var i = 0; i < serverList.length; i++) {
			if (serverList[i][0].toUpperCase() == name.toUpperCase()) {
				serverList[i][1] = load;
				break;
			}
		}
	}
	
	public function getServerLoad(name:String):String {	
		for (var i = 0; i < serverList.length; i++) {
			if (serverList[i][0].toUpperCase() == name.toUpperCase()) {
				return serverList[i][1];
				break;
			}
		}
	}

	// SOCKET METHODS //
	public function sendLogin(username:String, password:String) {
		socket.send("<loginCheck|"+username+"|"+password+">");
		tempLogin = [username, password];
	}

	public function retrieveServerlist() {
		socket.send("<retrieveServerList>");
	}
	
	public function connect(){
		socket.connect(ip,port);
	}

	// EXECUTING METHODS //
	public function read(msg:String) {
		trace("[MASTER SERVER]: Received message " + msg);
		
		var msgCache = msg.substring(1, msg.length-1);
		msgCache = msgCache.split("|");

		switch (msgCache[0]) {
			case "loginCheck" :
				if (msgCache[2] == "true") {
					retrieveServerlist();
				} else if (msgCache[2] == "false") {
					if(wrongLoginCount <= 3){
						sendLogin(tempLogin[0], tempLogin[1]);
						wrongLoginCount++;
					}else{
						wrongLoginCount = 0;
						onError("Incorrect details!", "MS-Login", "0-0001", 
							"The username and password combination you have provided is incorrect.");
					}
				} else if (msgCache[2] == "multi"){
					onError("Multiple logins detected!", "MS-Login", "0-0003", 
							"The account you are trying to access is already logged in.");
				}
				break;
			case "serverList" :
				setServerLoad(msgCache[2], int(msgCache[1]));				
				
				for (var i = 0; i < serverList.length; i++) {
					if(serverList[i][1] == null){
						serverListAccomplished = false;
						break;
					}
				}
				
				if(serverListAccomplished){
					onRecieveServerList(true);
				}
				
				break;
		}
	}
	
	// DEBUGGING METHODS //
	public function traceSocket(){
		trace("[MASTER SERVER]: Socket: " + socket);
	}
	
	public function traceServerProperties (){
		trace("[MASTER SERVER]: Master Server IP: " + ip);
		trace("[MASTER SERVER]: Master Server Port: " + port);
	}
	
	// FILE-SPECIFIC METHODS //
	public function tryLogin(username:String, password:String){
		if(username != "" && password != ""){
			onLoginGranted();
		}else{
			onError("Incorrect details!", "MS-Login", "0-0003", 
					"Both username and password must be filled in.");
		}
	}
}