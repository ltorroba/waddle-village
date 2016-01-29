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

class core.waddle.village.api.Assambler {
	
	public static var CLASS_REF = core.waddle.village.api.Assambler;
	
	public var loadlist:Array = [["login.swf", "login_mc", false], ["play.swf", "play_mc", false]]; // [[swfName, mcToAllocate, loaded?]];
	
	public function Assambler() {};
	
	// CLIENT-DEFINED EVENTS //
	public function onLoadComplete () { };
	public function onReset () { };
	
	// API-DEFINED FUNCTIONS //
	public function allMoviesLoaded(){
		for(var i = 0; i < loadlist.length; i++){
			if(!loadlist[i][2]){
				return false;
			}
		}
		
		trace("[ASSAMBLER]: All movies have been successfully loaded");		
		return true;
	}
	
	public function resetGame () {
		onReset();
	}
}