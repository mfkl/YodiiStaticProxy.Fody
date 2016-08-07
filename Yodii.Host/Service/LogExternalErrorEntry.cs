#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\LogExternalErrorEntry.cs) is part of CiviKey. 
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
using System.Reflection;
using System.Diagnostics;
using Yodii.Model;

namespace Yodii.Host
{
    class LogExternalErrorEntry : LogExternalEntry, ILogExternalErrorEntry
    {
        Exception _error;
        MemberInfo _culprit;

        internal LogExternalErrorEntry( int lsn, int depth, Exception error, MemberInfo optionalExplicitCulprit, string message, object extraData )
            : base( lsn, depth, message, extraData )
        {
            _error = error;
            _culprit = optionalExplicitCulprit ?? _error.TargetSite;
        }

        public override LogEntryType EntryType
        {
            get { return LogEntryType.ExternalError; }
        }

        public MemberInfo Culprit
        {
            get { return _culprit; }
        }

        public Exception Error
        {
            get { return _error; }
        }

    }
}