#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Model\Discoverer\IPluginCtorInfo.cs) is part of CiviKey. 
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
using System.Threading.Tasks;

namespace Yodii.Model
{
    /// <summary>
    /// Describes the plugin constructor that has been selected.
    /// </summary>
    public interface IPluginCtorInfo
    {
        /// <summary>
        /// Gets the number of parameters of the selected constructor.
        /// </summary>
        int ParameterCount { get; }

        /// <summary>
        /// Gets the subset of known parameters.
        /// This relies on conventions (even services already described in <see cref="IPluginInfo.ServiceReferences"/> may appear here).
        /// Standard <see cref="IDiscoverer"/> currently only detects "IActivityMonitor" and "IYodiiEngine" parameters.
        /// </summary>
        IReadOnlyList<IPluginCtorKnownParameterInfo> KnownParameters { get; }
    }
}
