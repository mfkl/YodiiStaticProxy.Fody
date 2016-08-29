#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\LogEventEntry.cs) is part of CiviKey. 
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CK.Core;
using YodiiStaticProxy.Fody.Log;

namespace YodiiStaticProxy.Fody.Service
{
    internal class LogEventEntry : LogHostEventArgs, ILogEventEntry, ICKReadOnlyCollection<ILogEventError>
    {
        int _depth;
        int _errorCount;
        LogEventEntryError _firstError;
        LogEventEntryError _lastError;
        internal object[] _parameters;

        internal bool IsErrorHead
        {
            get { return _errorCount < 0; }
        }

        int IReadOnlyCollection<ILogEventError>.Count
        {
            get { return Math.Abs(_errorCount); }
        }

        bool ICKReadOnlyCollection<ILogEventError>.Contains(object o)
        {
            var e = o as LogEventEntryError;
            return e != null && e.OtherErrors == this;
        }

        IEnumerator<ILogEventError> IEnumerable<ILogEventError>.GetEnumerator()
        {
            var l = _firstError;
            while (l != null)
            {
                yield return l;
                l = l._nextError;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ILogEventError>) this).GetEnumerator();
        }

        public override LogEntryType EntryType
        {
            get { return LogEntryType.Event; }
        }

        public override int Depth
        {
            get { return _depth; }
        }

        public EventInfo Event { get; set; }

        public object[] Parameters
        {
            get { return _parameters; }
        }

        public override MemberInfo Member
        {
            get { return Event; }
        }

        public ICKReadOnlyCollection<ILogEventError> Errors
        {
            get { return this; }
        }

        internal void InitOpen(int lsn, int depth, EventInfo e)
        {
            LSN = -lsn;
            _depth = depth;
            Event = e;
        }

        internal void InitClose(int lsn, int depth, EventInfo e)
        {
            LSN = lsn;
            _depth = depth;
            Event = e;
        }

        /// <summary>
        ///     Initializes the entry as an hidden error head (the first time an error occcurs and no event entry is created
        ///     for the event). This entry is not visible (it is not emitted as a log event), it is here to handle the
        ///     potential list of errors that the event will raise.
        /// </summary>
        /// <param name="lsn"></param>
        /// <param name="depth"></param>
        /// <param name="e"></param>
        /// <param name="firstOne"></param>
        internal void InitError(int lsn, int depth, EventInfo e, LogEventEntryError firstOne)
        {
            LSN = lsn;
            _depth = depth;
            Event = e;
            _errorCount = -1;
            _firstError = _lastError = firstOne;
        }

        internal void AddError(LogEventEntryError l)
        {
            if(_errorCount > 0) ++_errorCount;
            else --_errorCount;
            if(_lastError != null) _lastError._nextError = l;
            else _firstError = l;
            _lastError = l;
        }

        internal void Close()
        {
            Debug.Assert(IsCreating);
            LSN = -LSN;
        }
    }
}