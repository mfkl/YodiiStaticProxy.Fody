﻿#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\LogEventEntryError.cs) is part of CiviKey. 
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
using System.Diagnostics;
using System.Reflection;
using CK.Core;
using YodiiStaticProxy.Fody.Log;

namespace YodiiStaticProxy.Fody.Service
{
    internal class LogEventEntryError : LogHostEventArgs, ILogEventError
    {
        readonly LogEventEntry _entry;
        internal LogEventEntryError _nextError;

        internal LogEventEntryError(int lsn, LogEventEntry e, MethodInfo target, Exception ex)
        {
            Debug.Assert(e != null && target != null && ex != null);
            LSN = lsn;
            _entry = e;
            Target = target;
            Error = ex;
        }

        public override LogEntryType EntryType
        {
            get { return LogEntryType.EventError; }
        }

        public override int Depth
        {
            get { return _entry.Depth; }
        }

        public EventInfo Event
        {
            get { return _entry.Event; }
        }

        public MethodInfo Target { get; }

        public MemberInfo Culprit
        {
            get { return Target; }
        }

        public ILogEventEntry EventEntry
        {
            get { return _entry.IsErrorHead ? null : _entry; }
        }

        public ICKReadOnlyCollection<ILogEventError> OtherErrors
        {
            get { return _entry; }
        }

        public Exception Error { get; }

        public override MemberInfo Member
        {
            get { return _entry.Event; }
        }
    }
}