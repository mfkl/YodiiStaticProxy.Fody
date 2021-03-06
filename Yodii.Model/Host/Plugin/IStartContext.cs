#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Model\Host\Plugin\IStartContext.cs) is part of CiviKey. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yodii.Model
{
    /// <summary>
    /// Transition context for <see cref="IYodiiPlugin.Start"/>.
    /// </summary>
    public interface IStartContext
    {
        /// <summary>
        /// Gets the running status that can be <see cref="T:RunningStatus.Running"/> or <see cref="T:RunningStatus.RunningLocked"/>.
        /// </summary>
        RunningStatus RunningStatus { get; }

        /// <summary>
        /// Gets whether this stop is from a cancelled <see cref="IYodiiPlugin.PreStop"/> rather
        /// than a successful <see cref="IYodiiPlugin.PreStart"/>.
        /// </summary>
        bool CancellingPreStop { get; }

        /// <summary>
        /// Gets a shared storage that is available during the whole transition.
        /// Used to exchange any kind of information during a transition: this typically 
        /// holds data that enables plugin hot swapping.
        /// </summary>
        IDictionary<object, object> SharedMemory { get; }

        /// <summary>
        /// Gets whether the plugin silently replaces the <see cref="IPreStartContext.PreviousPlugin"/> if any.
        /// </summary>
        bool HotSwapping { get; }

        /// <summary>
        /// Posts an action that will be executed once the current phase will be completed.
        /// </summary>
        /// <param name="delayedAction">Any action on the Engine.</param>
        void PostAction( Action<IYodiiEngineExternal> delayedAction );
    }
}
