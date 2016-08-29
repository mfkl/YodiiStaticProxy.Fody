#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\LogMethodEntry.cs) is part of CiviKey. 
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
    internal class LogMethodEntry : LogHostEventArgs, ILogMethodEntry
    {
        int _depth;
        LogMethodEntryError _error;
        internal object[] _parameters;
        internal object _returnValue;

        public override LogEntryType EntryType
        {
            get { return LogEntryType.Method; }
        }

        public override int Depth
        {
            get { return _depth; }
        }

        public MethodInfo Method { get; set; }

        public object[] Parameters
        {
            get { return _parameters; }
        }

        public object ReturnValue
        {
            get { return _returnValue; }
        }

        public override MemberInfo Member
        {
            get { return Method; }
        }

        public ILogMethodError Error
        {
            get { return _error; }
        }

        internal void InitOpen(int lsn, int depth, MethodInfo m)
        {
            LSN = -lsn;
            _depth = depth;
            Method = m;
        }

        internal void InitClose(int lsn, int depth, MethodInfo m)
        {
            LSN = lsn;
            _depth = depth;
            Method = m;
        }

        /// <summary>
        ///     Setting the error closes the entry if it was opened.
        ///     True is returned if the entry has been closed.
        /// </summary>
        internal bool SetError(LogMethodEntryError e)
        {
            _error = e;
            if(LSN < 0)
            {
                LSN = -LSN;
                return true;
            }
            return false;
        }

        internal void Close()
        {
            Debug.Assert(IsCreating);
            LSN = -LSN;
        }
    }
}