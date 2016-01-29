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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaddleVillage_Server
{
    class Item
    {
        /// <summary>
        /// The Item's Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// There are 5 types of items, them being: head, torso, legs, feet and accessory. The count starts at 0, and goes up to 4.
        /// </summary>
        public int type { get; set; }        
    }
}
