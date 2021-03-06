﻿#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\LogEventNotRunningError.cs) is part of CiviKey. 
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

using System.Diagnostics;
using System.Reflection;
using YodiiStaticProxy.Fody.Log;

namespace YodiiStaticProxy.Fody.Service
{
    internal class LogEventNotRunningError : LogHostEventArgs, ILogEventNotRunningError
    {
        internal LogEventNotRunningError(int lsn, int depth, EventInfo e, bool serviceIsDisabled)
        {
            Debug.Assert(e != null);
            LSN = lsn;
            Event = e;
            Depth = depth;
            ServiceIsDisabled = serviceIsDisabled;
        }

        public override LogEntryType EntryType
        {
            get { return LogEntryType.EventNotRunningError; }
        }

        public bool ServiceIsDisabled { get; }

        public override int Depth { get; }

        public MemberInfo Culprit
        {
            get { return Event; }
        }

        public EventInfo Event { get; }

        public override MemberInfo Member
        {
            get { return Event; }
        }
    }
}