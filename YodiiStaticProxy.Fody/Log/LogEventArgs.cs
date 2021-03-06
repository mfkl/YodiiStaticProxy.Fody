﻿#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Log\LogEventArgs.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/

#endregion

using System;

namespace YodiiStaticProxy.Fody.Log
{
    /// <summary>
    ///     Base class for event logs.
    /// </summary>
    public abstract class LogEventArgs : EventArgs, ILogEntry
    {
        /// <summary>
        ///     True if this event is beeing created (its <see cref="LSN" /> is negative).
        /// </summary>
        public bool IsCreating
        {
            get { return LSN < 0; }
        }

        /// <summary>
        ///     Describes the type of this entry.
        /// </summary>
        public abstract LogEntryType EntryType { get; }

        /// <summary>
        ///     Log Serial Number: incremental number that enables the ordering of the events.
        ///     When negative, this event is under creation (<see cref="IsCreating" /> is true).
        /// </summary>
        public int LSN { get; protected set; }

        /// <summary>
        ///     Depth in the call stack (at the proxy level).
        /// </summary>
        public abstract int Depth { get; }
    }
}